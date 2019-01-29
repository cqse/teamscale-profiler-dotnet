using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UploadDaemon
{
    /// <summary>
    /// Scans the trace directory for trace files that are ready to upload or archive.
    /// </summary>
    public class TraceFileScanner
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string traceDirectory;
        private readonly IFileSystem fileSystem;

        public TraceFileScanner(string traceDirectory, IFileSystem fileSystem)
        {
            this.traceDirectory = traceDirectory;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Returns all trace files that can be uploaded or archived.
        /// </summary>
        public IEnumerable<TraceFile> ListTraceFilesReadyForUpload()
        {
            List<string> files;
            try
            {
                files = fileSystem.Directory.EnumerateFiles(traceDirectory).ToList();
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to list files in {traceDirectory}. Will retry later", traceDirectory);
                yield break;
            }

            logger.Debug("Scanning {fileCount} files", files.Count);
            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                if (!TraceFile.IsTraceFile(fileName))
                {
                    logger.Debug("Skipping file that does not look like a trace file: {unknownFilePath}", filePath);
                    continue;
                }

                TraceFile scannedFile = ScanFile(filePath);
                if (scannedFile != null)
                {
                    yield return scannedFile;
                }
            }
        }

        /// <summary>
        /// Scans the given file path and returns the resulting ScannedFile or null in case the file should be ignored.
        ///
        /// Please note that we deliberately chose to only check the locking status of a file, not whether it contains the
        /// Stopped= line in order to decide if the file should be processed or not. We did this to also be able to process
        /// files when the profiler was hard-killed while writing the coverage info (e.g. in eager mode with certain unit
        /// test frameworks).
        /// </summary>
        private TraceFile ScanFile(string filePath)
        {
            if (IsLocked(filePath))
            {
                logger.Debug("Ignoring locked trace {trace}", filePath);
                return null;
            }

            string[] lines;
            try
            {
                lines = fileSystem.File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to read from file {trace}. Ignoring this file", filePath);
                return null;
            }

            return new TraceFile(filePath, lines);
        }

        private bool IsLocked(string tracePath)
        {
            try
            {
                using (fileSystem.File.Open(tracePath, FileMode.Open))
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                logger.Debug(e, "Failed to open {trace}. Assuming it's locked", tracePath);
                // this is slightly inaccurate as the error might stem from permission problems etc.
                // but we log it
                return true;
            }
        }
    }
}
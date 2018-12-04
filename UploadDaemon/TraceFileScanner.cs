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
        private static readonly Regex TraceFileRegex = new Regex(@"^coverage_\d*_\d*.txt$");

        private readonly string traceDirectory;
        private readonly string versionAssembly;
        private readonly Regex versionAssemblyRegex;
        private readonly IFileSystem fileSystem;

        public TraceFileScanner(string traceDirectory, string versionAssembly, IFileSystem fileSystem)
        {
            this.traceDirectory = traceDirectory;
            this.versionAssembly = versionAssembly;
            this.versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*Version:([^ ]*).*", RegexOptions.IgnoreCase);
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Returns all trace files that can be uploaded or archived.
        /// </summary>
        public IEnumerable<ScannedFile> ListTraceFilesReadyForUpload()
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
                if (!IsTraceFile(fileName))
                {
                    logger.Debug("Skipping file that does not look like a trace file: {unknownFilePath}", filePath);
                    continue;
                }

                ScannedFile scannedFile = ScanFile(filePath);
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
        private ScannedFile ScanFile(string filePath)
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

            string version = FindVersion(lines, filePath);
            return new ScannedFile
            {
                FilePath = filePath,
                Version = version,
                IsEmpty = IsEmpty(lines),
            };
        }

        private bool IsEmpty(string[] lines)
        {
            return !lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
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

        private string FindVersion(string[] lines, string tracePath)
        {
            Match matchingLine = lines.Select(line => versionAssemblyRegex.Match(line)).Where(match => match.Success).FirstOrDefault();
            if (matchingLine == null)
            {
                logger.Debug("Did not find the version assembly {versionAssembly} in {trace}", versionAssembly, tracePath);
                return null;
            }

            return matchingLine.Groups[1].Value;
        }

        private bool IsTraceFile(string fileName)
        {
            return TraceFileRegex.IsMatch(fileName);
        }

        /// <summary>
        /// A single file that can either be uploaded or archived.
        /// </summary>
        public class ScannedFile
        {
            /// <summary>
            /// The path to the file.
            /// </summary>
            public string FilePath { get; set; }

            /// <summary>
            /// The parsed version of the version assembly or null in case the version assembly was not in the file.
            /// </summary>
            public string Version { get; set; }

            /// <summary>
            /// If this is true then the trace file contains no coverage information (may happen when the profiler
            /// is killed before it can write the information to disk).
            /// </summary>
            public bool IsEmpty { get; set; }

            public override bool Equals(object obj)
            {
                return obj is ScannedFile file &&
                       FilePath == file.FilePath &&
                       Version == file.Version &&
                       IsEmpty == file.IsEmpty;
            }

            public override int GetHashCode()
            {
                int hashCode = -1491167301;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
                hashCode = hashCode * -1521134295 + EqualityComparer<bool>.Default.GetHashCode(IsEmpty);
                return hashCode;
            }

            public override string ToString()
            {
                return $"ScannedFile[{FilePath} Version={Version} IsEmpty={IsEmpty}]";
            }
        }
    }
}
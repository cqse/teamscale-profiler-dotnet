using NLog;
using System;
using System.IO;
using System.IO.Abstractions;

namespace UploadDaemon
{
    /// <summary>
    /// Archives processed traces to different archive directories.
    /// </summary>
    public class Archiver
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem fileSystem;
        private readonly string uploadedDirectory;
        private readonly string missingVersionDirectory;
        private readonly string emptyFileDirectory;
        private readonly string missingProcessDirectory;
        private readonly string noLineCoverageDirectory;

        public Archiver(string traceDirectory, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.uploadedDirectory = Path.Combine(traceDirectory, "uploaded");
            this.missingVersionDirectory = Path.Combine(traceDirectory, "missing-version");
            this.emptyFileDirectory = Path.Combine(traceDirectory, "empty-traces");
            this.missingProcessDirectory = Path.Combine(traceDirectory, "missing-process");
            this.noLineCoverageDirectory = Path.Combine(traceDirectory, "no-line-coverage");
        }

        /// <summary>
        /// Archives a file that was successfully uploaded.
        /// </summary>
        public void ArchiveUploadedFile(string tracePath)
        {
            Archive(tracePath, uploadedDirectory);
        }

        /// <summary>
        /// Archives a file that has no version assembly.
        /// </summary>
        public void ArchiveFileWithoutVersionAssembly(string tracePath)
        {
            Archive(tracePath, missingVersionDirectory);
        }

        /// <summary>
        /// Archives a file that has no profiled process path.
        /// </summary>
        public void ArchiveFileWithoutProcess(string tracePath)
        {
            Archive(tracePath, missingProcessDirectory);
        }

        /// <summary>
        /// Archives a file that has no coverage data (Jitted=, Inlined= lines).
        /// </summary>
        public void ArchiveEmptyFile(string tracePath)
        {
            Archive(tracePath, emptyFileDirectory);
        }

        /// <summary>
        /// Archives a file that, after being converted to line coverage, did not produce any coverage.
        /// </summary>
        public void ArchiveFileWithoutLineCoverage(string tracePath)
        {
            Archive(tracePath, noLineCoverageDirectory);
        }

        private void Archive(string tracePath, string targetDirectory)
        {
            if (!fileSystem.Directory.Exists(targetDirectory))
            {
                try
                {
                    fileSystem.Directory.CreateDirectory(targetDirectory);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Unable to create archive directory {archivePath}. Trace file {trace} cannot be archived and will be processed again later", targetDirectory, tracePath);
                    return;
                }
            }

            string targetPath = Path.Combine(targetDirectory, Path.GetFileName(tracePath));
            try
            {
                fileSystem.File.Move(tracePath, targetPath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to archive {trace} to {archivePath}. The file will remain there" +
                    " which may lead to it being uploaded multiple times", tracePath, targetDirectory);
            }
        }
    }
}
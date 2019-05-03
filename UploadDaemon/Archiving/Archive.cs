using NLog;
using System;
using System.IO;
using System.IO.Abstractions;

namespace UploadDaemon.Archiving
{
    /// <summary>
    /// Archives processed traces to different archive directories.
    /// </summary>
    public class Archive : IArchive
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem fileSystem;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly string uploadedDirectory;
        private readonly string missingVersionDirectory;
        private readonly string emptyFileDirectory;
        private readonly string missingProcessDirectory;
        private readonly string noLineCoverageDirectory;

        public Archive(string traceDirectory, IFileSystem fileSystem, IDateTimeProvider dateTimeProvider)
        {
            this.fileSystem = fileSystem;
            this.dateTimeProvider = dateTimeProvider;
            this.uploadedDirectory = Path.Combine(traceDirectory, "uploaded");
            this.missingVersionDirectory = Path.Combine(traceDirectory, "missing-version");
            this.emptyFileDirectory = Path.Combine(traceDirectory, "empty-traces");
            this.missingProcessDirectory = Path.Combine(traceDirectory, "missing-process");
            this.noLineCoverageDirectory = Path.Combine(traceDirectory, "no-line-coverage");
        }

        /// <inheritDoc/>
        public void ArchiveUploadedFile(string tracePath)
        {
            MoveFileToArchive(tracePath, uploadedDirectory);
        }

        /// <inheritDoc/>
        public void PurgeUploadedFiles(TimeSpan maximumAge)
        {
            PurgeFiles(uploadedDirectory, maximumAge);
        }

        /// <inheritDoc/>
        public void ArchiveFileWithoutVersionAssembly(string tracePath)
        {
            MoveFileToArchive(tracePath, missingVersionDirectory);
        }

        /// <inheritDoc/>
        public void PurgeFilesWithoutVersionAssembly(TimeSpan maximumAge)
        {
            PurgeFiles(missingVersionDirectory, maximumAge);
        }

        /// <inheritDoc/>
        public void ArchiveFileWithoutProcess(string tracePath)
        {
            MoveFileToArchive(tracePath, missingProcessDirectory);
        }

        /// <inheritDoc/>
        public void PurgeFilesWithoutProcess(TimeSpan maximumAge)
        {
            PurgeFiles(missingProcessDirectory, maximumAge);
        }

        /// <inheritDoc/>
        public void ArchiveEmptyFile(string tracePath)
        {
            MoveFileToArchive(tracePath, emptyFileDirectory);
        }

        /// <inheritDoc/>
        public void PurgeEmptyFiles(TimeSpan maximumAge)
        {
            PurgeFiles(emptyFileDirectory, maximumAge);
        }

        /// <inheritDoc/>
        public void ArchiveFileWithoutLineCoverage(string tracePath)
        {
            MoveFileToArchive(tracePath, noLineCoverageDirectory);
        }

        /// <inheritDoc/>
        public void PurgeFilesWithoutLineCoverage(TimeSpan maximumAge)
        {
            PurgeFiles(noLineCoverageDirectory, maximumAge);
        }

        private void MoveFileToArchive(string tracePath, string targetDirectory)
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

        private void PurgeFiles(string archiveDirectory, TimeSpan maximumAge)
        {
            if (!fileSystem.Directory.Exists(archiveDirectory))
            {
                return;
            }

            foreach (string file in fileSystem.Directory.GetFiles(archiveDirectory))
            {
                DateTime creationTime = fileSystem.File.GetCreationTime(file);
                if (dateTimeProvider.Now > (creationTime + maximumAge))
                {
                    fileSystem.File.Delete(file);
                }
            }
        }
    }
}
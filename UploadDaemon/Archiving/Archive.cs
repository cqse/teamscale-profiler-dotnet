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
        // TODO just for testing this
        public static bool ShouldArchiveLineCoverage = false;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFileSystem fileSystem;
        private readonly IDateTimeProvider dateTimeProvider;
        private readonly string uploadedDirectory;
        private readonly string missingVersionDirectory;
        private readonly string emptyFileDirectory;
        private readonly string missingProcessDirectory;
        private readonly string noLineCoverageDirectory;
        private readonly string lineCoverageDirectory;

        public Archive(string traceDirectory, IFileSystem fileSystem, IDateTimeProvider dateTimeProvider)
        {
            this.fileSystem = fileSystem;
            this.dateTimeProvider = dateTimeProvider;
            this.uploadedDirectory = Path.Combine(traceDirectory, "uploaded");
            this.missingVersionDirectory = Path.Combine(traceDirectory, "missing-version");
            this.emptyFileDirectory = Path.Combine(traceDirectory, "empty-traces");
            this.missingProcessDirectory = Path.Combine(traceDirectory, "missing-process");
            this.noLineCoverageDirectory = Path.Combine(traceDirectory, "no-line-coverage");
            this.lineCoverageDirectory = Path.Combine(traceDirectory, "converted-line-coverage");
        }

        /// <inheritdoc/>
        public void ArchiveLineCoverage(string fileName, string lineCoverageReport)
        {
            if (!ShouldArchiveLineCoverage)
            {
                return;
            }

            // TODO refactor
            if (!fileSystem.Directory.Exists(lineCoverageDirectory))
            {
                try
                {
                    fileSystem.Directory.CreateDirectory(lineCoverageDirectory);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Unable to create archive directory {archivePath}. Line coverage will not be archived",
                        lineCoverageDirectory);
                    return;
                }
            }

            // make sure there's no .. or other path components in the file name
            string sanitizedFileName = Path.GetFileName(fileName);
            string targetPath = Path.Combine(lineCoverageDirectory, sanitizedFileName);
            try
            {
                fileSystem.File.WriteAllText(targetPath, lineCoverageReport);
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to archive line coverage to {archivePath}.", targetPath);
            }
        }

        /// <inheritdoc/>
        public void ArchiveUploadedFile(string tracePath)
        {
            MoveFileToArchive(tracePath, uploadedDirectory);
        }

        /// <inheritdoc/>
        public void PurgeUploadedFiles(TimeSpan maximumAge)
        {
            PurgeFiles(uploadedDirectory, maximumAge);
        }

        /// <inheritdoc/>
        public void ArchiveFileWithoutVersionAssembly(string tracePath)
        {
            MoveFileToArchive(tracePath, missingVersionDirectory);
        }

        /// <inheritdoc/>
        public void PurgeFilesWithoutVersionAssembly(TimeSpan maximumAge)
        {
            PurgeFiles(missingVersionDirectory, maximumAge);
        }

        /// <inheritdoc/>
        public void ArchiveFileWithoutProcess(string tracePath)
        {
            MoveFileToArchive(tracePath, missingProcessDirectory);
        }

        /// <inheritdoc/>
        public void PurgeFilesWithoutProcess(TimeSpan maximumAge)
        {
            PurgeFiles(missingProcessDirectory, maximumAge);
        }

        /// <inheritdoc/>
        public void ArchiveEmptyFile(string tracePath)
        {
            MoveFileToArchive(tracePath, emptyFileDirectory);
        }

        /// <inheritdoc/>
        public void PurgeEmptyFiles(TimeSpan maximumAge)
        {
            PurgeFiles(emptyFileDirectory, maximumAge);
        }

        /// <inheritdoc/>
        public void ArchiveFileWithoutLineCoverage(string tracePath)
        {
            MoveFileToArchive(tracePath, noLineCoverageDirectory);
        }

        /// <inheritdoc/>
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
                    logger.Error(e, "Unable to create archive directory {archivePath}. Trace file {trace}" +
                        " cannot be archived and will be processed again later", targetDirectory, tracePath);
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

            try
            {
                foreach (string file in fileSystem.Directory.GetFiles(archiveDirectory))
                {
                    DeleteFileIfOlderThan(file, maximumAge);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while purging {archive}. Some purgeable files might not have been" +
                    " deleted.", archiveDirectory);
            }
        }

        private void DeleteFileIfOlderThan(string file, TimeSpan maximumAge)
        {
            try
            {
                DateTime creationTime = fileSystem.File.GetCreationTime(file);
                if (dateTimeProvider.Now > (creationTime + maximumAge))
                {
                    fileSystem.File.Delete(file);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Unable to purge {trace}. The file will remain in the archive.", file);
            }
        }
    }
}

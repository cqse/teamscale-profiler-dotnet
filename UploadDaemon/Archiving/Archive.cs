using NLog;
using System;
using System.IO;
using System.IO.Abstractions;

namespace UploadDaemon.Archiving
{
    public interface IArchive
    {
        /// <summary>
        /// Archives a file that was successfully uploaded.
        /// </summary>
        void ArchiveUploadedFile(string tracePath);

        /// <summary>
        /// Deletes all uploaded files that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeUploadedFiles(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no version assembly.
        /// </summary>
        void ArchiveFileWithoutVersionAssembly(string tracePath);

        /// <summary>
        /// Deletes all files without version assembly that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutVersionAssembly(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no profiled process path.
        /// </summary>
        void ArchiveFileWithoutProcess(string tracePath);

        /// <summary>
        /// Deletes all files without a profiled process that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutProcess(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that has no coverage data (Jitted=, Inlined= lines).
        /// </summary>
        void ArchiveEmptyFile(string tracePath);

        /// <summary>
        /// Deletes all files without coverage that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeEmptyFiles(TimeSpan maximumAge);

        /// <summary>
        /// Archives a file that, after being converted to line coverage, did not produce any coverage.
        /// </summary>
        void ArchiveFileWithoutLineCoverage(string tracePath);

        /// <summary>
        /// Deletes all files without line coverage that are older than the given maximum age from the archive.
        /// </summary>
        void PurgeFilesWithoutLineCoverage(TimeSpan maximumAge);
    }


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

        /// <summary>
        /// Archives a file that was successfully uploaded.
        /// </summary>
        public void ArchiveUploadedFile(string tracePath)
        {
            MoveFileToArchive(tracePath, uploadedDirectory);
        }

        /// <summary>
        /// Deletes all uploaded files that are older than the given maximum age from the archive.
        /// </summary>
        public void PurgeUploadedFiles(TimeSpan maximumAge)
        {
            PurgeFiles(uploadedDirectory, maximumAge);
        }

        /// <summary>
        /// Archives a file that has no version assembly.
        /// </summary>
        public void ArchiveFileWithoutVersionAssembly(string tracePath)
        {
            MoveFileToArchive(tracePath, missingVersionDirectory);
        }

        /// <summary>
        /// Deletes all files without version assembly that are older than the given maximum age from the archive.
        /// </summary>
        public void PurgeFilesWithoutVersionAssembly(TimeSpan maximumAge)
        {
            PurgeFiles(missingVersionDirectory, maximumAge);
        }

        /// <summary>
        /// Archives a file that has no profiled process path.
        /// </summary>
        public void ArchiveFileWithoutProcess(string tracePath)
        {
            MoveFileToArchive(tracePath, missingProcessDirectory);
        }

        /// <summary>
        /// Deletes all files without a profiled process that are older than the given maximum age from the archive.
        /// </summary>
        public void PurgeFilesWithoutProcess(TimeSpan maximumAge)
        {
            PurgeFiles(missingProcessDirectory, maximumAge);
        }

        /// <summary>
        /// Archives a file that has no coverage data (Jitted=, Inlined= lines).
        /// </summary>
        public void ArchiveEmptyFile(string tracePath)
        {
            MoveFileToArchive(tracePath, emptyFileDirectory);
        }

        /// <summary>
        /// Deletes all files without coverage that are older than the given maximum age from the archive.
        /// </summary>
        public void PurgeEmptyFiles(TimeSpan maximumAge)
        {
            PurgeFiles(emptyFileDirectory, maximumAge);
        }

        /// <summary>
        /// Archives a file that, after being converted to line coverage, did not produce any coverage.
        /// </summary>
        public void ArchiveFileWithoutLineCoverage(string tracePath)
        {
            MoveFileToArchive(tracePath, noLineCoverageDirectory);
        }

        /// <summary>
        /// Deletes all files without line coverage that are older than the given maximum age from the archive.
        /// </summary>
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

    public interface IDateTimeProvider
    {
        DateTime Now { get; }
    }

    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
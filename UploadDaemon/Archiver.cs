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

        public Archiver(string traceDirectory, IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
            this.uploadedDirectory = Path.Combine(traceDirectory, "uploaded");
            this.missingVersionDirectory = Path.Combine(traceDirectory, "missing-version");
            this.emptyFileDirectory = Path.Combine(traceDirectory, "empty-traces");
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

        public void ArchiveEmptyFile(string tracePath)
        {
            Archive(tracePath, emptyFileDirectory);
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
                    logger.Error(e, "Unable to create archive directory {archivePath}. Trace file {tracePath} cannot be archived and will be processed again later", targetDirectory, tracePath);
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
<<<<<<< HEAD
                logger.Error(e, "Unable to archive {tracePath} to {archivePath}. The file will remain there" +
                    " which may lead to it being uploaded multiple times", tracePath, targetDirectory);
            }
        }
=======
                logger.Error(e, "Unable to create archive directory {archivePath}. Trace {trace} cannot be archived and will be processed again later", targetDirectory, tracePath);
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
>>>>>>> origin/master
    }
}
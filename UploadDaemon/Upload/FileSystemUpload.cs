using System;
using System.IO;
using System.IO.Abstractions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads trace files to a folder on the file system (e.g. a network share).
    /// </summary>
    internal class FileSystemUpload : IUpload
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string targetDirectory;
        private readonly IFileSystem fileSystem;

        public FileSystemUpload(string targetDirectory, IFileSystem fileSystem)
        {
            this.targetDirectory = targetDirectory;
            this.fileSystem = fileSystem;
        }

        /// <summary>
        /// Performs the upload asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="version">The application version (read from a version assembly).</param>
        /// <returns>Whether the upload was successful.</returns>
        public Task<bool> UploadAsync(string filePath, string version)
        {
            logger.Debug("Uploading {tracePath} to {targetDirectory}", filePath, targetDirectory);

            string fileName = Path.GetFileName(filePath);
            string targetPath = Path.Combine(targetDirectory, fileName);

            try
            {
                fileSystem.File.Move(filePath, targetPath);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to upload {tracePath} to {targetDirectory}. Will retry later.", filePath, targetDirectory);
                return Task.FromResult(false);
            }
        }

        public string Describe()
        {
            return $"file system directory {targetDirectory}";
        }
    }
}
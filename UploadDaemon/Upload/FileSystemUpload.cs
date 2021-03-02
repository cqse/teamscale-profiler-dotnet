using System;
using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using NLog;
using UploadDaemon.Report;
using UploadDaemon.SymbolAnalysis;

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
                fileSystem.File.Copy(filePath, targetPath);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to upload {tracePath} to {targetDirectory}. Will retry later", filePath, targetDirectory);
                return Task.FromResult(false);
            }
        }

        public string Describe()
        {
            return $"file system directory {targetDirectory}";
        }

        /// <inheritDoc/>
        public Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, ICoverageReport coverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
        {
            long unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string filePath = Path.Combine(targetDirectory, $"{unixSeconds}.{coverageReport.FileExtension}");
            string metadataFilePath = Path.Combine(targetDirectory, $"{unixSeconds}.metadata");

            try
            {
                fileSystem.File.WriteAllText(filePath, coverageReport.ToString());
                fileSystem.File.WriteAllText(metadataFilePath, revisionOrTimestamp.ToRevisionFileContent());
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to upload line coverage from {trace} to {targetDirectory}. Will retry later",
                    originalTraceFilePath, targetDirectory);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// We can merge all coverage destined for the same target directory as the line coverage files
        /// don't contain any meta data anymore so they are indistinguishable at that point.
        /// </summary>
        public object GetTargetId()
        {
            return targetDirectory;
        }
    }
}

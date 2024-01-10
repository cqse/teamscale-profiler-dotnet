using System;
using System.Collections.Generic;
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
            string fileName = Path.GetFileName(filePath);
            string targetPath = Path.Combine(targetDirectory, fileName);

            try
            {
                EnsureTargetDirectoryExists(targetDirectory);

                logger.Debug("Copying {tracePath} to {targetDirectory}", filePath, targetDirectory);
                fileSystem.File.Copy(filePath, targetPath);
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to upload {tracePath} to {targetDirectory}. Will retry later", filePath, targetDirectory);
                return Task.FromResult(false);
            }
        }

        private void EnsureTargetDirectoryExists(string targetDirectory)
        {
            if (File.Exists(targetDirectory))
            {
                throw new IOException($"{targetDirectory} exists, but is a file");
            }

            if (!Directory.Exists(targetDirectory))
            {
                logger.Debug("Creating target directory: {targetDirectory}", targetDirectory);
                Directory.CreateDirectory(targetDirectory);
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

            try
            {
                EnsureTargetDirectoryExists(targetDirectory);
                List<string> reports = coverageReport.ToStringList();
                int i = 1;
                foreach (string report in reports)
                {
                    string filePath = Path.Combine(targetDirectory, $"{unixSeconds}_{i}.{coverageReport.FileExtension}");
                    fileSystem.File.WriteAllText(filePath, report);

                    string metadataFilePath = Path.Combine(targetDirectory, $"{unixSeconds}_{i}.{coverageReport.FileExtension}.metadata");
                    fileSystem.File.WriteAllText(metadataFilePath, revisionOrTimestamp.ToRevisionFileContent());
                    i++;
                }
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

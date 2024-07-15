using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Configuration;
using UploadDaemon.Report;
using System.IO.Compression;
using System.Collections.Generic;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads trace files to Artifactory.
    /// </summary>
    internal class ArtifactoryUpload : IUpload
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient client = new HttpClient();

        private readonly Artifactory artifactory;

        public ArtifactoryUpload(Artifactory artifactory)
        {
            this.artifactory = artifactory;

            HttpClientUtils.SetUpArtifactoryAuthentication(client, artifactory);
        }

        /// <summary>
        /// Performs the upload asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="version">The application version (read from a version assembly).</param>
        /// <returns>Whether the upload was successful.</returns>
        public async Task<bool> UploadAsync(string filePath, string version)
        {
            logger.Error("Default schema upload to artifactory is only supported for local conversion");
            return false;
        }

        public string Describe()
        {
            return artifactory.ToString();
        }

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, ICoverageReport coverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
        {
            if (revisionOrTimestamp.IsRevision)
            {
                logger.Error("You need to provide a timestamp in the revision txt in order to upload to artifactory");
                return false;
            }
            string[] branchAndTimestamp = revisionOrTimestamp.Value.Split(':');
            string url = $"{artifactory.Url}/uploads/{branchAndTimestamp[0]}/{branchAndTimestamp[1]}";
            if (artifactory.PathSuffix != null)
            {
                string encodedPathSuffix = HttpUtility.UrlEncode(artifactory.PathSuffix);
                url = $"{url}/{encodedPathSuffix}";
            }

            try
            {
                bool result = true;
                List<string> reports = coverageReport.ToStringList();
                int index = 1;
                foreach (string report in reports)
                {
                    string covFileName = "";
                    if (coverageReport.UploadFormat == "SIMPLE")
                    {
                        covFileName = $"{artifactory.Partition}/simple_{index}.txt";
                    }
                    else
                    {
                        covFileName = $"{artifactory.Partition}/testwise_{index}.json";
                    }
                    byte[] reportBytes = CreateZipFile(report, covFileName);

                    String reportName = $"report_{index}.zip";
                    string reportUrl = $"{url}/{reportName}";

                    logger.Debug("Uploading line coverage from {trace} to {artifactory} ({url})", originalTraceFilePath, artifactory.ToString(), reportUrl);

                    result = result && await PerformLineCoverageUpload(originalTraceFilePath, revisionOrTimestamp.Value, reportUrl, reportBytes);
                    index++;
                }
                return result;
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage from {trace} to {artifactory} failed due to an exception." +
                    " Will retry later", originalTraceFilePath, artifactory.ToString());
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePath, string timestampValue, string url, byte[] stream)
        {
            using (HttpResponseMessage response = await HttpClientUtils.UploadPut(client, url, stream))
            {
                if (response.IsSuccessStatusCode)
                {
                    logger.Info("Successfully uploaded line coverage from {trace} with {parameterValue} to {artifactory}",
                        originalTraceFilePath, timestampValue, artifactory.ToString());
                    return true;
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync();
                    logger.Error("Upload of line coverage to {artifactory} failed with status code {statusCode}. This coverage is lost." +
                        "\n{responseBody}", artifactory.ToString(), response.StatusCode, body);
                    return false;
                }
            }
        }

        private static byte[] CreateZipFile(string lineCoverageReport, string entryName)
        {
            byte[] compressedBytes;
            byte[] reportBytes = Encoding.UTF8.GetBytes(lineCoverageReport);
            using (var outStream = new MemoryStream())
            using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
            {
                var fileInArchive = archive.CreateEntry(entryName);
                using (var entryStream = fileInArchive.Open())
                using (var fileToCompressStream = new MemoryStream(reportBytes))
                {
                    fileToCompressStream.CopyTo(entryStream);
                }
                compressedBytes = outStream.ToArray();
            }
            return compressedBytes;
        }

        /// <inheritdoc/>
        public object GetTargetId()
        {
            return (artifactory.Url, artifactory.Partition);
        }
    }
}

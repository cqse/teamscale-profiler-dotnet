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

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, ICoverageReport lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
        {
            if (revisionOrTimestamp.IsRevision)
            {
                logger.Error("You need to provide a timestamp in the revision txt in order to upload to artifactory");
                return false;
            }
            string[] branchAndTimestamp = revisionOrTimestamp.Value.Split(':');
            string url = $"{artifactory.Url}/uploads/{branchAndTimestamp[0]}/{branchAndTimestamp[1]}";
            if (lineCoverageReport.UploadFormat == "SIMPLE")
            {
                url = $"{url}/{artifactory.Partition}/simple";
            } else
            {
                url = $"{url}/{artifactory.Partition}/testwise";
            }
            if (artifactory.PathSuffix != null)
            {
                string encodedPathSuffix = HttpUtility.UrlEncode(artifactory.PathSuffix);
                url = $"{url}/{encodedPathSuffix}";
            }
            String reportName = "";
            if (lineCoverageReport.UploadFormat == "SIMPLE")
            {
                reportName = "report.simple";
            } else
            {
                reportName = "report.testwise";
            }
            url = $"{url}/{reportName}";

            logger.Debug("Uploading line coverage from {trace} to {artifactory} ({url})", originalTraceFilePath, artifactory.ToString(), url);

            try
            {
                byte[] reportBytes = CreateZipFile(lineCoverageReport.ToString());
                using (MemoryStream stream = new MemoryStream(reportBytes))
                {
                    return await PerformLineCoverageUpload(originalTraceFilePath, revisionOrTimestamp.Value, url, stream, reportName);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage from {trace} to {artifactory} failed due to an exception." +
                    " Will retry later", originalTraceFilePath, artifactory.ToString());
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePath, string timestampValue, string url, MemoryStream stream, String reportName)
        {
            using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPartPut(client, url, "report", stream, reportName))
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

        private static byte[] CreateZipFile(string lineCoverageReport)
        {
            byte[] compressedBytes;
            byte[] reportBytes = Encoding.UTF8.GetBytes(lineCoverageReport);
            using (var outStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(outStream, ZipArchiveMode.Create, true))
                {
                    var fileInArchive = archive.CreateEntry("coverage.txt");
                    using (var entryStream = fileInArchive.Open())
                    using (var fileToCompressStream = new MemoryStream(reportBytes))
                    {
                        fileToCompressStream.CopyTo(entryStream);
                    }
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

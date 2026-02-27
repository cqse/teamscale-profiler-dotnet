using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Configuration;
using System.IO.Compression;
using System.Reflection;

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

        public string Describe()
        {
            return artifactory.ToString();
        }

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
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
            string fileName = $"{artifactory.Partition}/report.txt";

            logger.Debug("Uploading line coverage from {trace} to {artifactory} ({url})", originalTraceFilePath, artifactory.ToString(), url);

            try
            {
                byte[] reportBytes = CreateZipFile(lineCoverageReport, fileName);

                string reportName = $"report.zip";
                string reportUrl = $"{url}/{reportName}";

                return await PerformLineCoverageUpload(originalTraceFilePath, revisionOrTimestamp.Value, reportUrl, reportBytes);
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

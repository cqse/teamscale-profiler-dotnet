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

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads trace files to Teamscale.
    /// </summary>
    internal class TeamscaleUpload : IUpload
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient client = new HttpClient();

        private readonly TeamscaleServer server;
        private readonly MessageFormatter messageFormatter;

        public TeamscaleUpload(TeamscaleServer server)
        {
            this.server = server;
            this.messageFormatter = new MessageFormatter(server);

            HttpClientUtils.SetUpBasicAuthentication(client, server);
        }

        /// <summary>
        /// Performs the upload asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="version">The application version (read from a version assembly).</param>
        /// <returns>Whether the upload was successful.</returns>
        public async Task<bool> UploadAsync(string filePath, string version)
        {
            logger.Debug("Uploading {trace} with version {version} to {teamscale}", filePath, version, server.ToString());

            string message = messageFormatter.Format(version);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(server.Project);
            string encodedVersion = HttpUtility.UrlEncode(version);
            string encodedPartition = HttpUtility.UrlEncode(server.Partition);
            string url = $"{server.Url}/p/{encodedProject}/dotnet-ephemeral-trace-upload?version={encodedVersion}" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true";

            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, url, "report", filePath))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded {trace} with version {version} to {teamscale}", filePath, version, server.ToString());
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of {trace} to {teamscale} failed with status code {statusCode}\n{responseBody}",
                            filePath, server.ToString(), response.StatusCode, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of {trace} to {teamscale} failed due to an exception",
                    filePath, server.ToString());
                return false;
            }
        }

        public string Describe()
        {
            return server.ToString();
        }

        /// <inheritDoc/>
        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, ICoverageReport coverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
        {
            string timestampParameter;
            if (revisionOrTimestamp.IsRevision)
            {
                timestampParameter = "revision";
            }
            else
            {
                timestampParameter = "t";
            }

            string message = messageFormatter.Format(timestampParameter);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(server.Project);
            string encodedTimestamp = HttpUtility.UrlEncode(revisionOrTimestamp.Value);
            string encodedPartition = HttpUtility.UrlEncode(server.Partition);
            string url = $"{server.Url}/p/{encodedProject}/external-report?format={coverageReport.UploadFormat}" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true" +
                $"&{timestampParameter}={encodedTimestamp}";

            logger.Debug("Uploading line coverage from {trace} to {teamscale} ({url})", originalTraceFilePath, server.ToString(), url);

            try
            {
                byte[] reportBytes = Encoding.UTF8.GetBytes(coverageReport.ToString());
                using (MemoryStream stream = new MemoryStream(reportBytes))
                {
                    return await PerformLineCoverageUpload(originalTraceFilePath, timestampParameter, revisionOrTimestamp.Value, url, stream);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage from {trace} to {teamscale} failed due to an exception." +
                    " Will retry later", originalTraceFilePath, server.ToString());
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePath, string timestampParameter, string timestampValue, string url, MemoryStream stream)
        {
            using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, url, "report", stream, "report.simple"))
            {
                if (response.IsSuccessStatusCode)
                {
                    logger.Info("Successfully uploaded line coverage from {trace} with {parameter}={parameterValue} to {teamscale}",
                        originalTraceFilePath, timestampParameter, timestampValue, server.ToString());
                    return true;
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync();
                    logger.Error("Upload of line coverage to {teamscale} failed with status code {statusCode}. This coverage is lost." +
                        "\n{responseBody}", server.ToString(), response.StatusCode, body);
                    return false;
                }
            }
        }

        /// <inheritdoc/>
        public object GetTargetId()
        {
            return (server.Url, server.Project, server.Partition);
        }
    }
}

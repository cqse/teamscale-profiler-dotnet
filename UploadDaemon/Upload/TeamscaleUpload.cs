using NLog;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using UploadDaemon.Configuration;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads trace files to Teamscale.
    /// </summary>
    internal class TeamscaleUpload : IUpload
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient client = new HttpClient();

        public readonly TeamscaleServer Server;
        private readonly MessageFormatter messageFormatter;

        public TeamscaleUpload(TeamscaleServer server)
        {
            this.Server = server;
            this.messageFormatter = new MessageFormatter(server);
            HttpClientUtils.SetUpBasicAuthentication(client, server);
        }

        public TeamscaleUpload(string targetProject, TeamscaleServer server)
        {
            Server = new TeamscaleServer(targetProject, server, logger);
            messageFormatter = new MessageFormatter(Server);
            HttpClientUtils.SetUpBasicAuthentication(client, Server);
        }

        /// <summary>
        /// Performs the upload asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="version">The application version (read from a version assembly).</param>
        /// <returns>Whether the upload was successful.</returns>
        public async Task<bool> UploadAsync(string filePath, string version)
        {
            logger.Debug("Uploading {trace} with version {version} to {teamscale}", filePath, version, Server.ToString());

            string message = messageFormatter.Format(version);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(Server.Project);
            string encodedVersion = HttpUtility.UrlEncode(version);
            string encodedPartition = HttpUtility.UrlEncode(Server.Partition);
            string url = $"{Server.Url}/p/{encodedProject}/dotnet-ephemeral-trace-upload?version={encodedVersion}" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true";

            return await DoAsyncUpload(filePath, version, url);
        }

        private async Task<bool> DoAsyncUpload(String filePath, string version, String encodedUrl)
        {
            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, encodedUrl, "report", filePath))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded {trace} with version {version} to {teamscale}", filePath, version, Server.ToString());
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of {trace} to {teamscale} failed with status code {statusCode}\n{responseBody}",
                            filePath, Server.ToString(), response.StatusCode, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of {trace} to {teamscale} failed due to an exception",
                    filePath, Server.ToString());
                return false;
            }
        }

        public string Describe()
        {
            return Server.ToString();
        }

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
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
            string encodedProject = HttpUtility.UrlEncode(Server.Project);
            string encodedTimestamp = HttpUtility.UrlEncode(revisionOrTimestamp.Value);
            string encodedPartition = HttpUtility.UrlEncode(Server.Partition);
            string url = $"{Server.Url}/p/{encodedProject}/external-report?format=SIMPLE" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true" +
                $"&{timestampParameter}={encodedTimestamp}";

            logger.Debug("Uploading line coverage from {trace} to {teamscale} ({url})", originalTraceFilePath, Server.ToString(), url);

            try
            {
                byte[] reportBytes = Encoding.UTF8.GetBytes(lineCoverageReport);
                using (MemoryStream stream = new MemoryStream(reportBytes))
                {
                    return await PerformLineCoverageUpload(originalTraceFilePath, timestampParameter, revisionOrTimestamp.Value, url, stream);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage from {trace} to {teamscale} failed due to an exception." +
                    " Will retry later", originalTraceFilePath, Server.ToString());
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePath, string timestampParameter, string timestampValue, string url, MemoryStream stream)
        {
            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, url, "report", stream, "report.simple"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded line coverage from {trace} with {parameter}={parameterValue} to {teamscale}",
                            originalTraceFilePath, timestampParameter, timestampValue, Server.ToString());
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of line coverage to {teamscale} failed with status code {statusCode}. This coverage is lost." +
                            "\n{responseBody}", Server.ToString(), response.StatusCode, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e);
                return false;
            }
        }

        /// <inheritdoc/>
        public object GetTargetId()
        {
            return (Server.Url, Server.Project, Server.Partition);
        }
    }
}

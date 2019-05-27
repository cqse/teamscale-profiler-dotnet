using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;
using Common;
using UploadDaemon.SymbolAnalysis;
using System.Collections.Generic;

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

        public async Task<bool> UploadLineCoverageAsync(string originalTraceFilePaths, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)
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
            logger.Debug("Uploading line coverage with {parameter}={parameterValue} to {teamscale} from {traces}",
                timestampParameter, revisionOrTimestamp.Value, server.ToString(), originalTraceFilePaths);

            string message = messageFormatter.Format(timestampParameter);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(server.Project);
            string encodedTimestamp = HttpUtility.UrlEncode(revisionOrTimestamp.Value);
            string encodedPartition = HttpUtility.UrlEncode(server.Partition);
            string url = $"{server.Url}/p/{encodedProject}/external-report?format=SIMPLE" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true" +
                $"&{timestampParameter}={encodedTimestamp}";

            byte[] reportBytes = Encoding.ASCII.GetBytes(lineCoverageReport);
            logger.Debug("Merged line coverage report has {count}kb", reportBytes.Length / 1024.0);

            try
            {
                using (MemoryStream stream = new MemoryStream(reportBytes))
                {
                    return await PerformLineCoverageUpload(originalTraceFilePaths, timestampParameter, revisionOrTimestamp.Value, url, stream);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage to {teamscale} failed due to an exception." +
                    " Will retry later. Trace files: {traces}", server.ToString(), originalTraceFilePaths);
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePaths, string timestampParameter, string timestampValue, string url, MemoryStream stream)
        {
            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, url, "report", stream, "report.simple"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded line coverage with {parameter}={parameterValue} to {teamscale} from {traces}",
                            timestampParameter, timestampValue, server.ToString(), originalTraceFilePaths);
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of line coverage to {teamscale} failed with status code {statusCode}. This coverage is lost." +
                            " Original trace files: {traces}\n{responseBody}", server.ToString(), response.StatusCode,
                            originalTraceFilePaths, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage to {teamscale} failed with an exception. This coverage is lost." +
                    " Original trace files: {traces}", server.ToString(), originalTraceFilePaths);
                return false;
            }
        }

        /// <inheritdoc/>
        public object GetTargetId()
        {
            return (server.Url, server.Project, server.Partition);
        }
    }
}

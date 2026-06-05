using NLog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using UploadDaemon.SymbolAnalysis;
using UploadDaemon.Report;
using System.Collections.Generic;
using UploadDaemon.Configuration;

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

            string message = messageFormatter.Format(revisionOrTimestamp);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(server.Project);
            string encodedTimestamp = HttpUtility.UrlEncode(revisionOrTimestamp.Value);
            string encodedPartition = HttpUtility.UrlEncode(server.Partition);
            string encodedFormat = HttpUtility.UrlEncode(coverageReport.UploadFormat);
            string url = $"{server.Url}/api/projects/{encodedProject}/external-analysis/session/auto-create/report?format={encodedFormat}" +
                $"&message={encodedMessage}&partition={encodedPartition}" +
                $"&{timestampParameter}={encodedTimestamp}";

            if (!string.IsNullOrEmpty(server.PathPrefix))
            {
                url += $"&path-prefix={HttpUtility.UrlEncode(server.PathPrefix)}";
            }

            logger.Debug("Uploading line coverage from {trace} to {teamscale} ({url})", originalTraceFilePath, server.ToString(), url);

            try
            {
                List<string> reports = coverageReport.ToStringList();
                string reportFileName = $"report.{coverageReport.FileExtension}";
                return await PerformLineCoverageUpload(originalTraceFilePath, timestampParameter, revisionOrTimestamp.Value, url, reports, reportFileName);
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of line coverage from {trace} to {teamscale} failed due to an exception." +
                    " Will retry later", originalTraceFilePath, server.ToString());
                return false;
            }
        }

        private async Task<bool> PerformLineCoverageUpload(string originalTraceFilePath, string timestampParameter, string timestampValue, string url, List<string> reports, string reportFileName)
        {
            using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPartList(client, url, "report", reports, reportFileName))
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


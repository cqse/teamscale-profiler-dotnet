using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NLog;

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

        SetUpBasicAuthentication(server);
    }

    private void SetUpBasicAuthentication(TeamscaleServer server)
    {
        byte[] byteArray = Encoding.ASCII.GetBytes($"{server.Username}:{server.AccessToken}");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
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
                    logger.Error("Upload of {trace} with version {version} to {teamscale} failed with status code {statusCode}\n{responseBody}",
                        filePath, version, server.ToString(), response.StatusCode, body);
                    return false;
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Upload of {trace} to {teamscale} failed due to an exception", filePath, server.ToString());
            return false;
        }
    }
}
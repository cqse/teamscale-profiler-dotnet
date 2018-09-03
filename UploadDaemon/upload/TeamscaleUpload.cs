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
        logger.Debug("Uploading {tracePath} with version {version} to {teamscale}", filePath, server.ToString(), version);
        using (MultipartFormDataContent content = new MultipartFormDataContent("Upload----" + DateTime.Now.Ticks.ToString("x")))
        {
            string fileName = Path.GetFileName(filePath);
            content.Add(new StreamContent(new FileStream(filePath, FileMode.Open)), "report", fileName);

            string message = messageFormatter.Format(version);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedProject = HttpUtility.UrlEncode(server.Project);
            string encodedVersion = HttpUtility.UrlEncode(version);
            string encodedPartition = HttpUtility.UrlEncode(server.Partition);
            string url = $"{server.Url}/p/{encodedProject}/dotnet-ephemeral-trace-upload?version={encodedVersion}" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true";

            try
            {
                return await PerformUpload(url, content, filePath, version);
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of {trace} to {teamscale} failed due to an exception",
                    filePath, server.ToString());
                return false;
            }
        }
    }

    /// <summary>
    /// Performs an upload of the given content to the given URL. May throw exceptions when the upload fails.
    /// </summary>
    private async Task<bool> PerformUpload(string url, MultipartFormDataContent content, string filePath, string version)
    {
        using (HttpResponseMessage response = await client.PostAsync(url, content))
        {
            if (response.IsSuccessStatusCode)
            {
                logger.Info("Successfully uploaded {tracePath} with version {version} to {teamscale}", filePath, server.ToString(), version);
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
}
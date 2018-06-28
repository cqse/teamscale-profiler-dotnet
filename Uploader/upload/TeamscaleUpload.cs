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
class TeamscaleUpload : IUpload
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();

    private readonly HttpClient client;

    private readonly TeamscaleServer server;

    public TeamscaleUpload(TeamscaleServer server)
    {
        this.client = new HttpClient();
        var byteArray = Encoding.ASCII.GetBytes($"{server.Username}:{server.AccessToken}");
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        this.server = server;
    }

    /// <summary>
    /// Performs the upload asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the file to upload.</param>
    /// <param name="version">The application version (read from a version assembly).</param>
    /// <param name="message">The upload commit message.</param>
    /// <param name="partition">The partition to upload to.</param>
    /// <returns>Whether the upload was successful.</returns>
    public async Task<bool> UploadAsync(string filePath, string version, string message, string partition)
    {
        logger.Debug("Uploading {tracePath} to {teamscale} with version {version} into partition {partition}", filePath, server.ToString(), version, partition);
        using (MultipartFormDataContent content = new MultipartFormDataContent("Upload----" + DateTime.Now.Ticks.ToString("x")))
        {
            string fileName = Path.GetFileName(filePath);
            content.Add(new StreamContent(new FileStream(filePath, FileMode.Open)), "report", fileName);

            string encodedVersion = HttpUtility.UrlEncode(version);
            string encodedMessage = HttpUtility.UrlEncode(message);
            string encodedPartition = HttpUtility.UrlEncode(partition);
            string url = $"{server.Url}/p/{server.Project}/dotnet-ephemeral-trace-upload?version={encodedVersion}" +
                $"&message={encodedMessage}&partition={encodedPartition}&adjusttimestamp=true&movetolastcommit=true";

            using (HttpResponseMessage response = await client.PostAsync(url, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    logger.Info("Successfully uploaded {trace} to {teamscale} with version {version} into partition {partition}", filePath, server.ToString(), version, partition);
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

}
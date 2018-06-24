using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using NLog;

/// <summary>
/// Uploads trace files to Teamscale.
/// </summary>
class TeamscaleUpload
{
    private readonly Logger logger = LogManager.GetCurrentClassLogger();

    private HttpClient client;

    private TeamscaleServer server;

    public TeamscaleUpload(HttpClient client, TeamscaleServer server)
    {
        this.client = client;
        this.server = server;
    }

    /// <summary>
    /// Performs the upload asynchronously.
    /// </summary>
    /// <param name="filePath">Path to the file to upload.</param>
    /// <param name="version">The application version (read from a version assembly).</param>
    /// <param name="message">The upload commit message.</param>
    /// <param name="partition">The partition to upload to.</param>
    /// <returns></returns>
    public async Task<bool> UploadAsync(string filePath, string version, string message, string partition)
    {
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
                    logger.Info("Successfully uploaded {trace}", filePath);
                    return true;
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync();
                    logger.Error("Upload of {trace} failed with status code {statusCode}\n{responseBody}", filePath, response.StatusCode, body);
                    return false;
                }
            }
        }
    }

}
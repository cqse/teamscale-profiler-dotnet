using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Common;
using NLog;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads trace files to the CQSE file upload service.
    /// </summary>
    internal class UploadServiceUpload : IUpload
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient client = new HttpClient();

        private readonly string url;

        public UploadServiceUpload(string url)
        {
            this.url = url;
        }

        /// <summary>
        /// Performs the upload asynchronously.
        /// </summary>
        /// <param name="filePath">Path to the file to upload.</param>
        /// <param name="version">The application version (read from a version assembly).</param>
        /// <returns>Whether the upload was successful.</returns>
        public async Task<bool> UploadAsync(string filePath, string version)
        {
            logger.Debug("Uploading {trace} with version {version} to {url}", filePath, version, url);

            try
            {
                using (HttpResponseMessage response = await HttpClientUtils.UploadMultiPart(client, url, "file", filePath))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        logger.Info("Successfully uploaded {trace} with version {version} to {url}", filePath, url, version);
                        return true;
                    }
                    else
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Upload of {trace} with version {version} to {url} failed with status code {statusCode}\n{responseBody}",
                            filePath, version, url, response.StatusCode, body);
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Upload of {trace} with version {version} to {url} failed due to an exception", filePath, version, url);
                return false;
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
                    logger.Info("Successfully uploaded {trace} with version {version} to {url}", filePath, url, version);
                    return true;
                }
                else
                {
                    string body = await response.Content.ReadAsStringAsync();
                    logger.Error("Upload of {trace} to {url} failed with status code {statusCode}\n{responseBody}",
                        filePath, url, response.StatusCode, body);
                    return false;
                }
            }
        }

        public string Describe()
        {
            return $"HTTP endpoint {url}";
        }
    }
}
using NLog;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// Utilities for using the HttpClient
    /// </summary>
    public class HttpClientUtils
    {
        private static readonly Logger logger = LogManager.GetLogger("HttpClientUtils");

        /// <summary>
        /// Disables all SSL validation.
        /// </summary>
        public static void DisableSslValidation()
        {
            // c.f. https://stackoverflow.com/a/18232008/1396068
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// Changes the given client's authorization to Basic with the credentials configured for the given server.
        /// </summary>
        public static void SetUpBasicAuthentication(HttpClient client, TeamscaleServer server)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes($"{server.Username}:{server.AccessKey}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        /// <summary>
        /// Uploads the given file in a multi-part request.
        /// </summary>
        /// <returns>The HTTP response. The caller must dispose of it.</returns>
        /// <exception cref="IOException">In case there are network or file system errors.</exception>
        /// <exception cref="HttpRequestException">In case there are network errors.</exception>
        public static async Task<HttpResponseMessage> UploadMultiPart(HttpClient client, string url, string multipartParameterName, string filePath)
        {
            return await UploadMultiPart(client, url, multipartParameterName, new FileStream(filePath, FileMode.Open), Path.GetFileName(filePath));
        }

        /// <summary>
        /// Uploads the given file in a multi-part request.
        /// </summary>
        /// <returns>The HTTP response. The caller must dispose of it.</returns>
        /// <exception cref="IOException">In case there are network or file system errors.</exception>
        /// <exception cref="HttpRequestException">In case there are network errors.</exception>
        public static Task<HttpResponseMessage> UploadMultiPart(HttpClient client, string url, string multipartParameterName, Stream stream, string fileName)
        {
            logger.Debug("Using MultipartFormDataContent");
            using (MultipartFormDataContent content = new MultipartFormDataContent("Upload----" + DateTime.Now.Ticks.ToString("x")))
            {
                logger.Debug("adding content");
                content.Add(new StreamContent(stream), multipartParameterName, fileName);

                logger.Debug("PostAsync");
                return client.PostAsync(url, content);
            }
        }
    }
}

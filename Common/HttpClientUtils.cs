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
        /// <summary>
        /// Sets common options for all HTTP requests.
        /// </summary>
        public static void ConfigureHttpStack(bool disableSslValidation)
        {
            // Teamscale's Jetty only supports TLS 1.2 so we enforce it here
            // Otherwise we get weird errors about aborted SSL connections from the .NET
            // network stack that are hard to debug
            ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            if (disableSslValidation)
            {
                DisableSslValidation();
            }
        }

        /// <summary>
        /// Disables all SSL validation.
        /// </summary>
        private static void DisableSslValidation()
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
        public static async Task<HttpResponseMessage> UploadMultiPart(HttpClient client, string url, string multipartParameterName, Stream stream, string fileName)
        {
            using (MultipartFormDataContent content = new MultipartFormDataContent("Upload----" + DateTime.Now.Ticks.ToString("x")))
            {
                content.Add(new StreamContent(stream), multipartParameterName, fileName);

                return await client.PostAsync(url, content);
            }
        }
    }
}

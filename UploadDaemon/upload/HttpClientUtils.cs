using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Utilities for the HttpClient.
/// </summary>
internal static class HttpClientUtils
{
    /// <summary>
    /// Uploads the given file in a multi-part request.
    /// </summary>
    /// <returns>The HTTP response. The caller must dispose of it.</returns>
    /// <exception cref="IOException">In case there are network or file system errors.</exception>
    /// <exception cref="HttpRequestException">In case there are network errors.</exception>
    public static async Task<HttpResponseMessage> UploadMultiPart(HttpClient client, string url, string multipartParameter, string filePath)
    {
        using (MultipartFormDataContent content = new MultipartFormDataContent("Upload----" + DateTime.Now.Ticks.ToString("x")))
        {
            string fileName = Path.GetFileName(filePath);
            content.Add(new StreamContent(new FileStream(filePath, FileMode.Open)), multipartParameter, fileName);

            return await client.PostAsync(url, content);
        }
    }
}
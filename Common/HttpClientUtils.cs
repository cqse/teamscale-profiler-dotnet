﻿using System;
using System.Net.Http;
using System.Text;

namespace Common
{
    /// <summary>
    /// Utilities for using the HttpClient
    /// </summary>
    public class HttpClientUtils
    {
        /// <summary>
        /// Changes the given client's authorization to Basic with the credentials configured for the given server.
        /// </summary>
        public static void SetUpBasicAuthentication(HttpClient client, TeamscaleServer server)
        {
            byte[] byteArray = Encoding.ASCII.GetBytes($"{server.Username}:{server.AccessKey}");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }
    }
}
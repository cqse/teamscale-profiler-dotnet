using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

using System.Net;

namespace Common
{
    /// <summary>
    /// Data class that is deserialized from the JSON configuration file.
    /// </summary>
    public class UploadConfig
    {
        public const string ConfigFileName = "UploadDaemon.json";

        /// <summary>
        /// The Teamscale server to upload to.
        /// </summary>
        public TeamscaleServer Teamscale { get; set; } = null;

        /// <summary>
        /// The assembly from which to read the version number.
        /// </summary>
        public string VersionAssembly { get; set; }

        /// <summary>
        /// The directory to upload the traces to.
        /// </summary>
        public string Directory { get; set; } = null;

        /// <summary>
        /// The url to POST the traces to.
        /// </summary>
        public string FileUpload { get; set; } = null;

        /// <summary>
        /// The Azure File Storage to upload to.
        /// </summary>
        public AzureFileStorage AzureFileStorage { get; set; } = null;

        /// <summary>
        /// Whether to skip SSL certificate validation.
        /// </summary>
        public bool DisableSslValidation { get; set; } = false;

        /// <summary>
        /// Validates the configuration and returns all collected error messages. An empty list
        /// means the configuration is valid.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Validate()
        {
            if (Teamscale == null && Directory == null && FileUpload == null && AzureFileStorage == null)
            {
                yield return @"You must provide either" +
                    @" a Teamscale server (property ""teamscale"")" +
                    @" or a directory (property ""directory"")" +
                    @" or an HTTP endpoint (property ""fileUpload"")" +
                    @" or an Azure File Storage (property ""azureFileStorage"")" +
                    @" to upload trace files to.";
            }
            if (VersionAssembly == null)
            {
                yield return @"You must provide an assembly name (property ""versionAssembly"", without the file extension) to read the program version from";
            }
        }

        /// <summary>
        /// Returns a deep copy of this config.
        /// </summary>
        public UploadConfig Clone()
        {
            return JsonConvert.DeserializeObject<UploadConfig>(JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// Tries to read the config JSON file.
        /// </summary>
        /// <exception cref="Exception">Throws an exception in case reading or deserializing goes wrong.</exception>
        public static UploadConfig ReadConfig(IFileSystem fileSystem, string configFilePath)
        {
            string json = fileSystem.File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<UploadConfig>(json);
        }
    }
}
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using UploadDaemon.Upload;

namespace UploadDaemon
{
    /// <summary>
    /// Data class that is deserialized from the JSON configuration file.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Name of the JSON config file.
        /// </summary>
        public const string ConfigFileName = "UploadDaemon.json";

        /// <summary>
        /// Path to the config file.
        /// </summary>
        public static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

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
        /// Validates the configuration and returns all collected error messages. An empty list
        /// means the configuration is valid.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> Validate()
        {
            if (Teamscale == null && Directory == null)
            {
                yield return @"You must provide either a Teamscale server (property ""teamscale"") or a directory (property ""directory"") to upload trace files to.";
            }
            if (VersionAssembly == null)
            {
                yield return @"You must provide an assembly name (property ""versionAssembly"", without the file extension) to read the program version from";
            }
        }

        /// <summary>
        /// Creates an IUpload based on this configuration.
        /// </summary>
        /// <param name="fileSystem">The file system to use</param>
        public IUpload CreateUpload(IFileSystem fileSystem)
        {
            if (Teamscale != null)
            {
                return new TeamscaleUpload(Teamscale);
            }
            return new FileSystemUpload(Directory, fileSystem);
        }

        /// <summary>
        /// Tries to read the config JSON file.
        /// </summary>
        /// <exception cref="Exception">Throws an exception in case reading or deserializing goes wrong.</exception>
        public static Config ReadConfig(IFileSystem fileSystem)
        {
            string json = fileSystem.File.ReadAllText(ConfigFilePath);
            return JsonConvert.DeserializeObject<Config>(json);
        }
    }
}
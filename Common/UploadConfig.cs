using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common
{
    /// <summary>
    /// Data class that is deserialized from the JSON configuration file.
    /// </summary>
    public class UploadConfig
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
                yield return @"You must provide either a Teamscale server or a directory to upload trace files to.";
            }
            if (VersionAssembly == null)
            {
                yield return @"You must provide an assembly name (without the file extension) to read the program version from";
            }
            if (VersionAssembly != null && (VersionAssembly.EndsWith(".dll") || VersionAssembly.EndsWith(".exe")))
            {
                yield return @"The version assembly must be given without the file extension";
            }
            if (Directory != null && !System.IO.Directory.Exists(Directory))
            {
                yield return $"The directory {Directory} does not exist";
            }

            IEnumerable<string> errors = Teamscale?.Validate() ?? Enumerable.Empty<string>();
            foreach (string error in errors)
            {
                yield return error;
            }
        }

        /// <summary>
        /// Returns a deep copy of this config.
        /// </summary>
        public UploadConfig Clone()
        {
            return new UploadConfig()
            {
                Teamscale = Teamscale?.Clone(),
                Directory = Directory,
                VersionAssembly = VersionAssembly,
            };
        }
    }
}
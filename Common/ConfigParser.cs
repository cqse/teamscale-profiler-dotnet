using System.Collections.Generic;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using Common;

namespace Common
{
    /// <summary>
    /// Parses the YAML config file into C# objects.
    /// </summary>
    public class ConfigParser
    {
        /// <summary>
        /// Represents the config file.
        /// </summary>
        public class YamlConfig
        {
            /// <summary>
            /// List of sections that contain config values that may only apply to certain processes.
            /// </summary>
            public List<ProcessSection> Match { get; set; }

            /// <summary>
            /// Whether SSL validation should be globally disabled. Null if the user did not set this property.
            /// </summary>
            public bool? DisableSslValidation { get; set; }
        }

        /// <summary>
        /// One of the sections under the "match" key.
        /// </summary>
        public class ProcessSection
        {
            /// <summary>
            /// Case-insensitive name of the profiled executable to which this section applies.
            /// If not given, this is null.
            /// </summary>
            public string ExecutableName { get; set; }

            /// <summary>
            /// Regex for the entire path of the profiled executable to which this section applies.
            /// If not given, this is null.
            /// </summary>
            public string ExecutablePathRegex { get; set; }

            /// <summary>
            /// Profiler options. Never null.
            /// </summary>
            public Dictionary<string, string> Profiler { get; set; }

            /// <summary>
            /// Uploader options. Never null.
            /// </summary>
            public UploaderSubsection Uploader { get; set; }
        }

        /// <summary>
        /// Contains uploader-specific options. All fields may be null in which case the user
        /// didn't specify this option.
        /// </summary>
        public class UploaderSubsection
        {
            /// <summary>
            /// The assembly from which to read the version number.
            /// </summary>
            public string VersionAssembly { get; set; }

            /// <summary>
            /// The Teamscale server to upload to.
            /// </summary>
            public TeamscaleServer Teamscale { get; set; }

            /// <summary>
            /// The url to POST the traces to.
            /// </summary>
            public string FileUpload { get; set; }

            /// <summary>
            /// The directory to upload the traces to.
            /// </summary>
            public string Directory { get; set; }

            /// <summary>
            /// The Azure File Storage to upload to.
            /// </summary>
            public AzureFileStorage AzureFileStorage { get; set; }

            /// <summary>
            /// Whether the profiler should be enabled.
            /// </summary>
            public bool? Enabled { get; set; }

            /// <summary>
            /// An optional prefix to prepend to the version before the upload.
            /// </summary>
            public string VersionPrefix { get; set; }
        }

        /// <summary>
        /// Parses the given YAML string into a config object.
        /// </summary>
        /// <exception cref="System.Exception">Thrown if parsing fails (the deserializer library doesn't specify which exceptions may be thrown)</exception>
        public static YamlConfig Parse(string yaml)
        {
            var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();
            YamlConfig result = deserializer.Deserialize<YamlConfig>(yaml);
            foreach (ProcessSection section in result.Match)
            {
                if (section.Profiler == null)
                {
                    section.Profiler = new Dictionary<string, string>();
                }
                if (section.Uploader == null)
                {
                    section.Uploader = new UploaderSubsection();
                }
            }
            return result;
        }
    }
}
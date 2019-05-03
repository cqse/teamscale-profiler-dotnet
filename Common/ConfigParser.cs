using System.Collections.Generic;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

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

            /// <summary>
            /// The interval to use for regular uploads (specified in minutes). Null if the user did not set this property.
            /// </summary>
            public int? UploadIntervalInMinutes { get; set; }
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
        /// Include and exclude patterns.
        /// </summary>
        public class IncludeExcludePatterns
        {
            /// <summary>
            /// Glob patterns that select what should be included.
            /// </summary>
            public List<string> Include { get; set; } = null;

            /// <summary>
            /// Glob patterns that select what should be excluded.
            /// Excludes override includes.
            /// </summary>
            public List<string> Exclude { get; set; } = null;
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
            /// The directory to upload the traces to.
            /// </summary>
            public string Directory { get; set; }

            /// <summary>
            /// The Azure File Storage to upload to.
            /// </summary>
            public AzureFileStorage AzureFileStorage { get; set; }

            /// <summary>
            /// Whether the uploader should be enabled.
            /// </summary>
            public bool? Enabled { get; set; }

            /// <summary>
            /// Whether the uploader should merge line coverage before uploading it.
            /// </summary>
            public bool? MergeLineCoverage { get; set; }

            /// <summary>
            /// An optional prefix to prepend to the version before the upload.
            /// </summary>
            public string VersionPrefix { get; set; }

            /// <summary>
            /// Directory from which to read PDB files to resolve method IDs in the trace files.
            /// </summary>
            public string PdbDirectory { get; set; }

            /// <summary>
            /// File that contains the code revision to which to upload line coverage if PDBs are used to resolve the traces locally.
            /// </summary>
            public string RevisionFile { get; set; }

            /// <summary>
            /// Patterns to select which assemblies to analyze.
            /// </summary>
            public IncludeExcludePatterns AssemblyPatterns { get; set; }
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

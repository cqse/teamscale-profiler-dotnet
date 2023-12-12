using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Configuration
{
    /// <summary>
    /// Stores the uploader's config and applies it depending on the
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Path to the config file. It's located one directory above the uploader's DLLs.
        /// </summary>
        public static readonly string ConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\Profiler.yml");

        /// <summary>
        /// Special directory name that denotes the assembly directory e.g. for resolving PDBs. */
        /// </summary>
        private const string AssemblyDirectory = "@AssemblyDir";

        /// <summary>
        /// Thrown if the config for a process is invalid.
        /// </summary>
        public class InvalidConfigException : Exception
        {
            public InvalidConfigException(string processPath, IEnumerable<string> errors)
                : base($"Config for process {processPath} is invalid: " + string.Join(", ", errors))
            {
            }

            public InvalidConfigException(string message)
                : base(message)
            {
            }

            public InvalidConfigException(string message, Exception e)
                : base(message, e)
            {
            }
        }

        /// <summary>
        /// Config values that apply to a particular process.
        /// All fields may be null if the user did not explicitly set them
        /// </summary>
        public class ConfigForProcess
        {
            /// <summary>
            /// The path to the process for which this configuration was created.
            /// </summary>
            private readonly string ProcessPath;

            /// <summary>
            /// The assembly from which to read the version number.
            /// </summary>
            public string VersionAssembly { get; private set; } = null;

            /// <summary>
            /// The Teamscale server to upload to.
            /// </summary>
            public TeamscaleServer Teamscale { get; private set; } = null;

            /// <summary>
            /// The directory to upload the traces to.
            /// </summary>
            public string Directory { get; private set; } = null;

            /// <summary>
            /// The Azure File Storage to upload to.
            /// </summary>
            public AzureFileStorage AzureFileStorage { get; private set; } = null;

            /// <summary>
            /// The Artifactory to upload to.
            /// </summary>
            public Artifactory Artifactory { get; private set; } = null;

            /// <summary>
            /// Whether the uploader should be enabled for this process.
            /// </summary>
            public bool Enabled { get; private set; } = true;

            /// <summary>
            /// Whether the uploader should merge line coverage before uploading it.
            /// </summary>
            public bool MergeLineCoverage { get; private set; } = true;

            /// <summary>
            /// An optional prefix to prepend to the version before the upload.
            /// Defaults to the empty string in case no prefix should be prepended.
            /// This property is never null.
            /// </summary>
            public string VersionPrefix { get; set; } = string.Empty;

            /// <summary>
            /// Directory from which to read PDB files to resolve method IDs in the trace files.
            /// Defaults to null;
            /// </summary>
            public string PdbDirectory { get; set; } = null;

            /// <summary>
            /// File that contains the code revision to which to upload line coverage if PDBs are used to resolve the traces locally.
            /// Defaults to null.
            /// </summary>
            public string RevisionFile { get; set; } = null;

            /// <summary>
            /// Patterns to select which assemblies to analyze.
            /// Defaults to sane default patterns.
            /// This property is never null.
            /// </summary>
            public GlobPatternList AssemblyPatterns { get; set; } = new GlobPatternList(DefaultAssemblyIncludePatterns, DefaultAssemblyExcludePatterns);

            private static readonly List<String> DefaultAssemblyIncludePatterns = new List<string> { "*" };

            private static readonly List<String> DefaultAssemblyExcludePatterns = new List<string> { "Microsoft.*", "Newtonsoft.*", "System.*",
                "System", "mscorlib", "log4net*", "EntityFramework*", "Antlr*", "Anonymously Hosted *", "App_*"};

            public ConfigForProcess(string processPath)
            {
                this.ProcessPath = processPath;
            }

            /// <summary>
            /// Applies the given uploader option section to this config object.
            /// </summary>
            public void ApplySection(ConfigParser.UploaderSubsection section)
            {
                VersionAssembly = section.VersionAssembly ?? VersionAssembly;
                Teamscale = section.Teamscale ?? Teamscale;
                Directory = section.Directory ?? Directory;
                AzureFileStorage = section.AzureFileStorage ?? AzureFileStorage;
                Artifactory = section.Artifactory ?? Artifactory;
                Enabled = section.Enabled ?? Enabled;
                VersionPrefix = section.VersionPrefix ?? VersionPrefix;
                PdbDirectory = section.PdbDirectory ?? PdbDirectory;
                RevisionFile = section.RevisionFile ?? RevisionFile;
                MergeLineCoverage = section.MergeLineCoverage ?? MergeLineCoverage;

                if (section.AssemblyPatterns != null)
                {
                    // we ensure that something is always included by using "*" as the include if the user doesn't include anything
                    List<string> includes = section.AssemblyPatterns.Include ?? DefaultAssemblyIncludePatterns;
                    // unless the user explicitly overrides the excludes, we use the sane exclude patterns to prevent
                    // the common error case of including System and other 3rd party assemblies. This prevents spamming the
                    // logs with lots of useless errors/warnings both in the Uploader and in Teamscale
                    List<string> excludes = section.AssemblyPatterns.Exclude ?? DefaultAssemblyExcludePatterns;
                    AssemblyPatterns = new GlobPatternList(includes, excludes);
                }
            }

            /// <summary>
            /// Validates the configuration and returns all collected error messages. An empty list
            /// means the configuration is valid.
            /// </summary>
            public IEnumerable<string> Validate()
            {
                if (Teamscale == null && Directory == null && AzureFileStorage == null && Artifactory == null)
                {
                    yield return $"Invalid configuration for process {ProcessPath}. You must provide either" +
                        @" a Teamscale server (property ""teamscale"")" +
                        @" or a directory (property ""directory"")" +
                        @" or an Azure File Storage (property ""azureFileStorage"")" +
                        @" or an Artifactory (property ""artifactory"")" +
                        @" to upload coverage files to.";
                }
                if (VersionAssembly != null && PdbDirectory != null)
                {
                    yield return $"Invalid configuration for process {ProcessPath}." +
                        @" You configured both method coverage upload (via property ""versionAssembly"")" +
                        @" and line coverage upload (via property ""pdbDirectory""). Please decide which you would" +
                        @" like to use and remove the other.";
                }
                if (VersionAssembly == null && PdbDirectory == null)
                {
                    yield return $"Invalid configuration for process {ProcessPath}." +
                        @" You must provide an assembly name (property ""versionAssembly""," +
                        @" without the file extension) to read the program version from in order to upload method coverage." +
                        @" Alternatively, you can configure line coverage upload (properties ""pdbDirectory"" and ""revisionFile"").";
                }
            }
        }

        /// <summary>
        /// Returns true if the path should be interpreted relatively to an assembly, e.g. starts with (case insensitive) @AssemblyDir.
        /// </summary>
        public static bool IsAssemblyRelativePath(string path)
        {
            return path.StartsWith(AssemblyDirectory, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Resolves a path relatively to an assembly if it starts with (case insensitive) @AssemblyDir. Returns null otherwise.
        /// </summary>
        public static string ResolveAssemblyRelativePath(string path, string assemblyPath)
        {
            if (!IsAssemblyRelativePath(path))
            {
                return null;
            }

            return Path.GetDirectoryName(assemblyPath) + path.Substring(AssemblyDirectory.Length);
        }

        private readonly List<ConfigParser.ProcessSection> Sections;

        /// <summary>
        /// Whether SSL validation should be globally disabled.
        /// </summary>
        public bool DisableSslValidation { get; private set; }

        /// <summary>
        /// The interval to use for regular uploads (specified in minutes). A value &lt;= 0 means regular uploads are disabled.
        /// </summary>
        public TimeSpan UploadInterval { get; private set; }

        /// <summary>
        /// Whether the uploader should archive the generated line coverage and the merged line coverage to disk.
        /// </summary>
        public bool ArchiveLineCoverage { get; private set; }

        /// <summary>
        /// The thresholds for purging upload archives. A value of <code>null</code> means purging is disabled.
        /// </summary>
        public ConfigParser.PurgeUploadArchivesSection ArchivePurgingThresholds { get; private set; }

        /// <summary>
        /// Returns all configured trace directories in which the uploader should
        /// regularly check for traces to upload.
        /// </summary>
        public IEnumerable<string> TraceDirectoriesToWatch =>
            Sections.Select(section => GetTraceDirectory(section.Profiler))
                .Where(directory => directory != null);

        public Config(ConfigParser.YamlConfig config)
        {
            this.Sections = config.Match;
            this.DisableSslValidation = config.DisableSslValidation ?? true;
            this.UploadInterval = TimeSpan.FromMinutes(config.UploadIntervalInMinutes ?? 5);
            this.ArchiveLineCoverage = config.ArchiveLineCoverage ?? false;
            this.ArchivePurgingThresholds = config.ArchivePurgingThresholdsInDays ?? new ConfigParser.PurgeUploadArchivesSection();
        }

        /// <summary>
        /// Creates the configuration that should be applied to the given profiled process.
        /// </summary>
        public ConfigForProcess CreateConfigForProcess(string profiledProcessPath, ParsedTraceFile traceFile = null)
        {
            ConfigForProcess config = new ConfigForProcess(profiledProcessPath);
            foreach (ConfigParser.ProcessSection section in Sections)
            {
                if (SectionApplies(section, profiledProcessPath, traceFile))
                {
                    config.ApplySection(section.Uploader);
                }
            }

            IEnumerable<string> errors = config.Validate();
            if (errors.Count() > 0)
            {
                throw new InvalidConfigException(profiledProcessPath, errors);
            }
            return config;
        }

        /// <summary>
        /// Reads in the central YAML config file and parses its contents.
        /// </summary>
        /// <exception cref="System.Exception">The underlying YAML library may throw any number of unknown
        /// exceptions in case of invalid input or when the given file is not readable.</exception>
        public static Config ReadConfigFile(string configFilePath)
        {
            try
            {
                return Read(File.ReadAllText(configFilePath));
            }
            catch (InvalidConfigException e)
            {
                throw new InvalidConfigException($"{e.Message}: The uploader will only watch for trace files in the targetdir" +
                    $" directories configured in {configFilePath}");
            }
        }

        /// <summary>
        /// Parses the given YAML as a config file.
        /// </summary>
        /// <exception cref="System.Exception">The underlying YAML library may throw any number of unknown
        /// exceptions in case of invalid input.</exception>
        public static Config Read(string yaml)
        {
            ConfigParser.YamlConfig yamlConfig = ConfigParser.Parse(yaml);
            Config config = new Config(yamlConfig);
            if (config.TraceDirectoriesToWatch.Any())
            {
                return config;
            }

            throw new InvalidConfigException("You must configure at least one targetdir profiler option in the YAML config file");
        }

        /// <summary>
        /// Returns true if the given section applies to the given profiled process.
        /// </summary>
        private static bool SectionApplies(ConfigParser.ProcessSection section, string profiledProcessPath, ParsedTraceFile traceFile = null)
        {
            bool?[] checks = new[] {
                MatchesExecutableName(section, profiledProcessPath),
                MatchesExecutablePathRegex(section, profiledProcessPath),
                MatchesLoadedAssemblyPathRegex(section, traceFile),
            };

            // The section applies if at least one of the check criteria is set (!= null) and all of these are true.
            return checks.Where(check => check != null).All(check => check == true);
        }

        /// <summary>
        /// If executable name is set, the executable's name must match case-insensitive
        /// </summary>
        private static bool? MatchesExecutableName(ConfigParser.ProcessSection section, string profiledProcessPath)
        {
            if (section.ExecutableName == null)
            {
                return null;
            }

            string profiledProcessName = Path.GetFileName(profiledProcessPath);
            return section.ExecutableName.Equals(profiledProcessName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// If executable path regex is set, the process must match
        /// </summary>
        private static bool? MatchesExecutablePathRegex(ConfigParser.ProcessSection section, string profiledProcessPath)
        {
            if (section.ExecutablePathRegex == null)
            {
                return null;
            }

            return Regex.IsMatch(profiledProcessPath, $"^{section.ExecutablePathRegex}$");
        }

        /// <summary>
        /// If loaded assembly path regex is set, at least one of the loaded assembly's path must match
        /// </summary>
        private static bool? MatchesLoadedAssemblyPathRegex(ConfigParser.ProcessSection section, ParsedTraceFile traceFile = null)
        {
            if (section.LoadedAssemblyPathRegex == null || traceFile == null)
            {
                return null;
            }

            Regex regex = new Regex($"^{section.LoadedAssemblyPathRegex}$");
            return traceFile.LoadedAssemblies.Any(assembly => regex.IsMatch(assembly.path));
        }

        /// <summary>
        /// Determines the directory to which the profiler will write trace files. If this is not set,
        /// null is returned.
        ///
        /// The profiler treats its options as case-insensitive, so we have to do the same here.
        /// </summary>
        private static string GetTraceDirectory(Dictionary<string, string> profilerOptions)
        {
            foreach (string key in profilerOptions.Keys)
            {
                if (key.Equals("targetdir", StringComparison.OrdinalIgnoreCase))
                {
                    return profilerOptions[key];
                }
            }
            return null;
        }
    }
}

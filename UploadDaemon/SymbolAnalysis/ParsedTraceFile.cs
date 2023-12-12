using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Parses the text of a trace file into a list of assembly names and method IDs that can be translated to line coverage
    /// with the help of PDB files.
    /// </summary>
    public class ParsedTraceFile
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// All assembly names mentioned in the trace file.
        /// </summary>
        public List<(string name, string path)> LoadedAssemblies { get; } = new List<(string name, string path)>();

        /// <summary>
        /// Path to this trace file.
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// All methods that are reported as covered. A method is identified by the name of its assembly
        /// (first elment in the tuple) and its ID (second element in the tuple).
        /// </summary>
        public List<(string, uint)> CoveredMethods { get; } = new List<(string, uint)>();

        private static readonly Regex AssemblyLineRegex = new Regex(@"^Assembly=(?<name>[^:]+):(?<id>\d+).*?(?: Path:(?<path>.*))?$");
        private static readonly Regex CoverageLineRegex = new Regex(@"^(?:Inlined|Jitted)=(\d+):(?:\d+:)?(\d+)");

        /// <summary>
        /// The name of the Resource .resx file that holed information about embedded upload targets.
        /// </summary>
        private const String TeamscaleResourceName = "Teamscale";

        /// <summary>
        /// The uploads targets (revision/timestamp and optionally teamscale project) that are retrieved from resource files that are embedded into assemblies referenced in the trace file.
        /// </summary>
        public readonly List<(string project, RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();

        public ParsedTraceFile(string[] lines, string filePath)
        {
            this.FilePath = filePath;

            Dictionary<uint, (string name, string path)> assemblyTokens = lines.Select(line => AssemblyLineRegex.Match(line))
                .Where(match => match.Success)
                .ToDictionary(
                    match => Convert.ToUInt32(match.Groups["id"].Value),
                    match => (name: match.Groups["name"].Value, path: match.Groups["path"].Value)
                );
            this.LoadedAssemblies = assemblyTokens.Values.ToList();
            SearchForEmbeddedUploadTargets();
            IEnumerable<Match> coverageMatches = lines.Select(line => CoverageLineRegex.Match(line))
                            .Where(match => match.Success);
            foreach (Match match in coverageMatches)
            {
                uint assemblyId = Convert.ToUInt32(match.Groups[1].Value);
                if (!assemblyTokens.TryGetValue(assemblyId, out (string name, string path) assembly))
                {
                    logger.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                        " Please report it to CQSE. Coverage for this assembly will be ignored.", filePath, assemblyId);
                    continue;
                }
                CoveredMethods.Add((assembly.name, Convert.ToUInt32(match.Groups[2].Value)));
            }
        }

        /// <summary>
        /// Checks the loaded assemblies for resources that contain information about target revision or teamscale projects.
        /// </summary>
        private void SearchForEmbeddedUploadTargets()
        {
            foreach ((_, string path) in this.LoadedAssemblies)
            {
                Assembly assembly = LoadAssemblyFromPath(path);
                if (assembly == null || assembly.DefinedTypes == null)
                {
                    continue;
                }
                TypeInfo teamscaleResourceType = assembly.DefinedTypes.FirstOrDefault(x => x.Name == TeamscaleResourceName) ?? null;
                if (teamscaleResourceType == null)
                {
                    continue;
                }
                logger.Info("Found embedded Teamscale resource in {assembly} that can be used to identify upload targets.", assembly);
                ResourceManager teamscaleResourceManager = new ResourceManager(teamscaleResourceType.FullName, assembly);
                string embeddedTeamscaleProject = teamscaleResourceManager.GetString("Project");
                string embeddedRevision = teamscaleResourceManager.GetString("Revision");
                string embeddedTimestamp = teamscaleResourceManager.GetString("Timestamp");
                AddUploadTarget(embeddedRevision, embeddedTimestamp, embeddedTeamscaleProject, embeddedUploadTargets, assembly.FullName);
            }
        }

        /// <summary>
        /// Adds a revision or timestamp and optionally a project to the list of upload targets. This method checks if both, revision and timestamp, are declared, or neither.
        /// </summary>
        /// <param name="revision"></param>
        /// <param name="timestamp"></param>
        /// <param name="project"></param>
        /// <param name="uploadTargets"></param>
        /// <param name="origin"></param>
        public static void AddUploadTarget(string revision, string timestamp, string project, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets, string origin)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            if (revision == null && timestamp == null)
            {
                logger.Error("Not all required fields in {origin}. Please specify either 'Revision' or 'Timestamp'", origin);
                return;
            }
            if (revision != null && timestamp != null)
            {
                logger.Error("'Revision' and 'Timestamp' are both set in {origin}. Please set only one, not both.", origin);
                return;
            }
            if (revision != null)
            {
                uploadTargets.Add((project, new RevisionOrTimestamp(revision, true)));
            }
            else
            {
                uploadTargets.Add((project, new RevisionOrTimestamp(timestamp, false)));
            }
        }

        private Assembly LoadAssemblyFromPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return null;
            }
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(path);
                // Check that defined types can actually be loaded
                if (assembly == null)
                {
                    return null;
                }
                IEnumerable<TypeInfo> ignored = assembly.DefinedTypes;
            }
            catch (Exception e)
            {
                logger.Debug("Could not load {assembly}. Skipping upload resource discovery. {e}", path, e);
                return null;
            }
            return assembly;
        }
    }
}

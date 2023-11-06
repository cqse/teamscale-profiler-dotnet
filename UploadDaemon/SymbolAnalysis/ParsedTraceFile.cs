using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;

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
        /// The uploads targets (revision and optionally teamscale project) that are embedded into assemblies referenced in the trace file.
        /// </summary>
        public readonly List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets = new List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)>();

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
                TypeInfo teamscaleResourceType = assembly.DefinedTypes.First(x => x.Name == "Teamscale_Resource");
                if (teamscaleResourceType != null)
                {
                    logger.Info("Found embedded Teamscale resource in {assembly} that can be used to identify upload targets.", assembly.FullName);
                    ResourceManager teamscaleResourceManager = new ResourceManager(teamscaleResourceType.FullName, assembly);
                    string embeddedTeamscaleProject = teamscaleResourceManager.GetString("Teamscale_Project");
                    string embeddedRevision = teamscaleResourceManager.GetString("RevisionOrTimestamp");
                    string isRevision = teamscaleResourceManager.GetString("isRevision");

                    if (embeddedRevision == null || isRevision == null)
                    {
                        logger.Error("Not all required fields in embedded resource found in {assembly}. Please specify atleast 'RevisionOrTimestamp' and 'isRevision'.", assembly.FullName);
                        continue;
                    }
                    embeddedUploadTargets.Add((embeddedTeamscaleProject, new RevisionFileUtils.RevisionOrTimestamp(embeddedRevision, bool.Parse(isRevision))));
                }
            }
        }

        private Assembly LoadAssemblyFromPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return null;
            }
            Assembly assembly = null;
            try
            {
                assembly = Assembly.LoadFrom(path);
            }
            catch (Exception e)
            {
                logger.Warn("Could not load {assembly}. Skipping upload resource discovery. {e}", path, e);
            }
            return assembly;
        }
    }
}

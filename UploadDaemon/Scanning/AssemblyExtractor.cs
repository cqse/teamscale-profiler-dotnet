using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.Scanning
{
    public class AssemblyExtractor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The name of the Resource .resx file that holed information about embedded upload targets.
        /// </summary>
        private const String TeamscaleResourceName = "Teamscale";
        private static readonly Regex AssemblyLineRegex = new Regex(@"^Assembly=(?<name>[^:]+):(?<id>\d+).*?(?: Path:(?<path>.*))?$");

        public readonly Dictionary<uint, (string name, string path)> Assemblies = new Dictionary<uint, (string name, string path)>();
        public readonly List<(string project, RevisionOrTimestamp revisionOrTimestamp)> EmbeddedUploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();

        public void ExtractAssemblies(string[] lines)
        {
            foreach (string line in lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, 2);
                if (keyValuePair.Length < 2)
                {
                    continue;
                }

                if (keyValuePair[0] == "Assembly")
                {
                    Match assemblyMatch = AssemblyLineRegex.Match(line);
                    Assemblies[Convert.ToUInt32(assemblyMatch.Groups["id"].Value)] = (assemblyMatch.Groups["name"].Value, assemblyMatch.Groups["path"].Value);
                }
            }

            SearchForEmbeddedUploadTargets(Assemblies, EmbeddedUploadTargets);
        }

        /// <summary>
        /// Checks the loaded assemblies for resources that contain information about target revision or teamscale projects.
        /// </summary>
        private void SearchForEmbeddedUploadTargets(Dictionary<uint, (string, string)> assemblyTokens, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets)
        {
            foreach (KeyValuePair<uint, (string, string)> entry in assemblyTokens)
            {
                Assembly assembly = LoadAssemblyFromPath(entry.Value.Item2);
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
                AddUploadTarget(embeddedRevision, embeddedTimestamp, embeddedTeamscaleProject, uploadTargets, assembly.FullName);
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

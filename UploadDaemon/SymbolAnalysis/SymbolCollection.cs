using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cqse.ConQAT.Dotnet.Bummer;
using NLog;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Stores mappings parsed from a collection of PDB files.
    /// </summary>
    public class SymbolCollection
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Location in a source file that spans a range of lines.
        /// </summary>
        public class SourceLocation
        {
            /// <summary>
            ///  The source file path. May be null when the PDB contains no source file path.
            /// </summary>
            public string SourceFile { get; set; }

            /// <summary>
            /// The start line (inclusive).
            /// </summary>
            public uint StartLine { get; set; }

            /// <summary>
            /// The end line (inclusive).
            /// </summary>
            public uint EndLine { get; set; }
        }

        private readonly Dictionary<string, Dictionary<uint, SourceLocation>> mappings = new Dictionary<string, Dictionary<uint, SourceLocation>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The paths of the PDB symbol files considered in this collection.
        /// </summary>
        public HashSet<string> SymbolFilePaths { get; private set; }

        public SymbolCollection(List<AssemblyMethodMappings> mappings)
        {
            WarnInCaseOfDuplicateMappings(mappings);

            SymbolFilePaths = new HashSet<string>(mappings.Select(mapping => mapping.SymbolFileName));

            foreach (AssemblyMethodMappings mapping in mappings)
            {
                // in case of duplicate mappings we merge the mappings by reusing the existing dictionary here
                if (!this.mappings.TryGetValue(mapping.AssemblyName, out Dictionary<uint, SourceLocation> assemblyMap))
                {
                    assemblyMap = new Dictionary<uint, SourceLocation>();
                }

                foreach (MethodMapping methodMapping in mapping.MethodMappings)
                {
                    assemblyMap[methodMapping.MethodToken] = new SourceLocation
                    {
                        SourceFile = methodMapping.SourceFile,
                        StartLine = methodMapping.StartLine,
                        EndLine = methodMapping.EndLine
                    };
                }

                this.mappings[mapping.AssemblyName] = assemblyMap;
            }
        }

        /// <summary>
        /// Returns true if no mappings are contained in this collection.
        /// </summary>
        public bool IsEmpty => mappings.Count() == 0 || mappings.Values.All(mapping => mapping.Count() == 0);

        private void WarnInCaseOfDuplicateMappings(List<AssemblyMethodMappings> mappings)
        {
            IEnumerable<IGrouping<string, AssemblyMethodMappings>> duplicateMappings = mappings.GroupBy(mapping => mapping.AssemblyName).Where(grouping => grouping.Count() > 1);
            foreach (IGrouping<string, AssemblyMethodMappings> duplicateMapping in duplicateMappings)
            {
                logger.Warn("Found more than one PDB file that provide mappings for assembly {assemblyName}: {pdbFiles}. The mappings will potentially overwrite each other." +
                    " You should make sure only one PDB file is included for each assembly by setting appropriate include/exclude patterns for the Upload Daemon.",
                    duplicateMapping.Key, string.Join(", ", duplicateMapping));
            }
        }

        /// <summary>
        /// Returns the source location for the given method in the given assembly or null if the method cannot be resolved.
        /// </summary>
        public SourceLocation Resolve(string assemblyName, uint methodToken)
        {
            if (!mappings.TryGetValue(assemblyName, out Dictionary<uint, SourceLocation> assemblyMappings))
            {
                return null;
            }
            if (!assemblyMappings.TryGetValue(methodToken, out SourceLocation location))
            {
                return null;
            }
            return location;
        }

        /// <summary>
        /// Creates a symbol collection from the given PDB files.
        /// </summary>
        public static SymbolCollection CreateFromFiles(IEnumerable<string> pdbFilePaths)
        {
            MethodMapper mapper = new MethodMapper();
            List<AssemblyMethodMappings> mappings = new List<AssemblyMethodMappings>();
            foreach (string filePath in pdbFilePaths)
            {
                logger.Debug("Loading mappings from PDB {filePath}", filePath);
                string assemblyName = Path.GetFileNameWithoutExtension(filePath);
                try
                {
                    AssemblyMethodMappings assemblyMappings = mapper.GetMethodMappings(filePath, assemblyName);
                    mappings.Add(assemblyMappings);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Failed to parse PDB file {pdbFile}. This file will be ignored and no coverage will be reported for its corresponding assembly." +
                        " You may get follow-up errors that method IDs cannot be resolved for this assembly", filePath);
                }
            }
            return new SymbolCollection(mappings);
        }

        /// <summary>
        /// Merges all passed symbol collections into a single one.
        /// </summary>
        public static SymbolCollection Merge(List<SymbolCollection> collections)
        {
            SymbolCollection mergedCollection = new SymbolCollection(new List<AssemblyMethodMappings>());
            foreach (SymbolCollection collection in collections)
            {
                foreach (string path in collection.SymbolFilePaths)
                {
                    mergedCollection.SymbolFilePaths.Add(path);
                }

                foreach(KeyValuePair<string, Dictionary<uint, SourceLocation>> entry in collection.mappings)
                {
                    // in case of duplicate mappings we merge the mappings by reusing the existing dictionary here
                    if (!mergedCollection.mappings.TryGetValue(entry.Key, out Dictionary<uint, SourceLocation> assemblyMap))
                    {
                        assemblyMap = new Dictionary<uint, SourceLocation>();
                        mergedCollection.mappings[entry.Key] = assemblyMap;
                    }

                    foreach (KeyValuePair<uint, SourceLocation> methodMapping in entry.Value)
                    {
                        assemblyMap[methodMapping.Key] = methodMapping.Value;
                    }
                }
            }

            return mergedCollection;
        }
    }
}
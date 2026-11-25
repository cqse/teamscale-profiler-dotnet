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
        /// The source file mappings of each method, by assembly name and method token. Use <see cref="GetKey"/> to create keys.
        /// </summary>
        private readonly Dictionary<(string assemblyName, uint methodToken), List<SourceFileMapping>> mappings = new Dictionary<(string, uint), List<SourceFileMapping>>();

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
                // in case of duplicate mappings, the mappings of the same method overwrite each other
                foreach (MethodMapping methodMapping in mapping.MethodMappings)
                {
                    this.mappings[GetKey(mapping.AssemblyName, methodMapping.MethodToken)] = methodMapping.SourceFileMappings;
                }
            }
        }

        /// <summary>
        /// Returns the key for the given method in the given assembly. Assembly names are case-insensitive.
        /// </summary>
        private static (string, uint) GetKey(string assemblyName, uint methodToken) => (assemblyName.ToUpperInvariant(), methodToken);

        /// <summary>
        /// Returns true if no mappings are contained in this collection.
        /// </summary>
        public bool IsEmpty => mappings.Count == 0;

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
        /// Returns the source file mappings for the given method in the given assembly, or null if the method cannot be
        /// resolved.
        /// </summary>
        public List<SourceFileMapping> Resolve(string assemblyName, uint methodToken)
        {
            mappings.TryGetValue(GetKey(assemblyName, methodToken), out List<SourceFileMapping> sourceFileMappings);
            return sourceFileMappings;
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

                // in case of duplicate mappings, the mappings of the same method overwrite each other
                foreach (KeyValuePair<(string, uint), List<SourceFileMapping>> entry in collection.mappings)
                {
                    mergedCollection.mappings[entry.Key] = entry.Value;
                }
            }

            return mergedCollection;
        }
    }
}
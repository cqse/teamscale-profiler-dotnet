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
        /// The lines of a method in a source file. A compact copy of a <see cref="SourceFileMapping"/>, since symbol
        /// collections are cached for a long time and may contain many methods.
        /// </summary>
        public class SourceLocation
        {
            /// <summary>
            /// The source file path. May be null when the PDB contains no source file path.
            /// </summary>
            public string SourceFile { get; }

            /// <summary>
            /// The sorted, non-overlapping ranges of inclusive start and end lines.
            /// </summary>
            public IReadOnlyList<(uint StartLine, uint EndLine)> LineRanges { get; }

            public SourceLocation(string sourceFile, IReadOnlyList<(uint StartLine, uint EndLine)> lineRanges)
            {
                SourceFile = sourceFile;
                LineRanges = lineRanges;
            }
        }

        /// <summary>
        /// The source locations of each method, by assembly name and method token. The assembly names are normalized with <see cref="NormalizeAssemblyName"/>.
        /// </summary>
        private readonly Dictionary<(string assemblyName, uint methodToken), SourceLocation[]> mappings = new Dictionary<(string, uint), SourceLocation[]>();

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
                // normalized once, so that all keys of the assembly share the same string
                string assemblyName = NormalizeAssemblyName(mapping.AssemblyName);

                // in case of duplicate mappings, the mappings of the same method overwrite each other
                foreach (MethodMapping methodMapping in mapping.MethodMappings)
                {
                    this.mappings[(assemblyName, methodMapping.MethodToken)] = methodMapping.SourceFileMappings
                        .Select(sourceFileMapping => new SourceLocation(sourceFileMapping.SourceFile,
                            sourceFileMapping.LineRanges.Select(range => (range.StartLine, range.EndLine)).ToArray()))
                        .ToArray();
                }
            }
        }

        /// <summary>
        /// Returns the given assembly name as it is used in the keys. Assembly names are case-insensitive.
        /// </summary>
        private static string NormalizeAssemblyName(string assemblyName) => assemblyName.ToUpperInvariant();

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
        /// Returns the source locations for the given method in the given assembly, one per source file, or null if the
        /// method cannot be resolved.
        /// </summary>
        public IReadOnlyList<SourceLocation> Resolve(string assemblyName, uint methodToken)
        {
            mappings.TryGetValue((NormalizeAssemblyName(assemblyName), methodToken), out SourceLocation[] locations);
            return locations;
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
                foreach (KeyValuePair<(string, uint), SourceLocation[]> entry in collection.mappings)
                {
                    mergedCollection.mappings[entry.Key] = entry.Value;
                }
            }

            return mergedCollection;
        }
    }
}
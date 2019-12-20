using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cqse.ConQAT.Dotnet.Bummer;
using NLog;
using UploadDaemon.Configuration;

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

        public SymbolCollection(List<AssemblyMethodMappings> mappings)
        {
            logger.Debug("Creating symbol collection from mappings");
            WarnInCaseOfDuplicateMappings(mappings);

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
        private static SymbolCollection CreateFromFiles(List<string> pdbFilePaths)
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
        /// Creates a symbol collection based on the PDB files in the given symbol directory whose file names without extension
        /// match the given pattern list.
        ///
        /// May throw exceptions if e.g. the symbol directory cannot be read. If one PDB file cannot be read or parsed, it will
        /// be ignored. No exception is thrown in this case.
        /// </summary>
        public static SymbolCollection CreateFromPdbFiles(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            logger.Debug("Searching PDB files in {symbolDirectory} using pattern '{pattern}'.", symbolDirectory, assemblyPatterns.Describe());
            List<string> pdbFiles = Directory.EnumerateFiles(symbolDirectory, "*.pdb", SearchOption.AllDirectories).ToList();
            List<string> relevantFiles = pdbFiles.Where(file => assemblyPatterns.Matches(Path.GetFileNameWithoutExtension(file))).ToList();
            return CreateFromFiles(relevantFiles);
        }
    }
}
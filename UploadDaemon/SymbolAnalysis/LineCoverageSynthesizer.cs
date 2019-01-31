using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Converts a trace file to a line coverage report with the help of PDB files.
    /// </summary>
    public class LineCoverageSynthesizer : ILineCoverageSynthesizer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Sane default include/exclude patterns for selecting which assemblies to analyze.
        /// </summary>
        // TODO scan trace from ASP .NET app for common patterns
        public static readonly GlobPatternList DefaultAssemblyPatterns = new GlobPatternList(new List<string> { "*" },
            new List<string> { "Microsoft.*", "Newtonsoft.*", "System.*", "System", "mscorlib", "log4net*", "EntityFramework*", "Antlr*",
                "Anonymously Hosted *"});

        /// The line coverage collected for one file.
        /// </summary>
        private class FileCoverage
        {
            /// <summary>
            /// The ranges of inclusive start and end lines that are covered in the file.
            /// </summary>
            public List<(uint, uint)> CoveredLineRanges = new List<(uint, uint)>();
        }

        /// <summary>
        /// Converts the given trace file to a line coverage report (format SIMPLE) with the PDB files
        /// in the given symbol directory.
        ///
        /// The assembly patterns are used to select both the assemblies from the trace files for which
        /// coverage should be generated and the PDB files which should be searched for mappings.
        ///
        /// May throw exceptions if converting the trace file fails completely. Partial failures (e.g. missing
        /// PDB) are logged and no exception is thrown.
        /// </summary>
        public string ConvertToLineCoverageReport(ParsedTraceFile traceFile, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            SymbolCollection symbolCollection = SymbolCollection.CreateFromPdbFiles(symbolDirectory, assemblyPatterns);
            if (symbolCollection.IsEmpty)
            {
                throw new LineCoverageConversionFailedException($"Failed to convert {traceFile.FilePath} to line coverage." +
                    $" Found no symbols in {symbolDirectory} matching {assemblyPatterns.Describe()}");
            }

            Dictionary<string, FileCoverage> lineCoverage = ConvertToLineCoverage(traceFile, symbolCollection, symbolDirectory, assemblyPatterns);
            if (lineCoverage.Count == 0 || lineCoverage.Values.All(fileCoverage => fileCoverage.CoveredLineRanges.Count() == 0))
            {
                throw new LineCoverageConversionFailedException($"Failed to convert {traceFile.FilePath} to line coverage." +
                    $" The trace produced no coverage. Either it really doesn't contain any relevant coverage or" +
                    $" the assembly patterns {assemblyPatterns.Describe()} are incorrect.");
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("# isMethodAccurate=true");
            foreach (string file in lineCoverage.Keys)
            {
                report.AppendLine(file);
                foreach ((uint startLine, uint endLine) in lineCoverage[file].CoveredLineRanges)
                {
                    report.AppendLine($"{startLine}-{endLine}");
                }
            }
            return report.ToString();
        }

        private class AssemblyResolutionCount
        {
            public int resolvedMethods = 0;
            public int unresolvedMethods = 0;
            public int TotalMethods => resolvedMethods + unresolvedMethods;
            public string UnresolvedPercentage => string.Format("{0:F1}%", unresolvedMethods * 100 / (double)TotalMethods);
        }

        /// <summary>
        /// Converts the given trace file to a dictionary containing all covered lines of each source file for which
        /// coverage could be resolved with the PDB files in the given symbol directory.
        /// </summary>
        private static Dictionary<string, FileCoverage> ConvertToLineCoverage(ParsedTraceFile traceFile, SymbolCollection symbolCollection,
            string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            Dictionary<string, AssemblyResolutionCount> resolutionCounts = new Dictionary<string, AssemblyResolutionCount>();
            Dictionary<string, FileCoverage> lineCoverage = new Dictionary<string, FileCoverage>();
            foreach ((string assemblyName, uint methodId) in traceFile.CoveredMethods)
            {
                if (!assemblyPatterns.Matches(assemblyName))
                {
                    continue;
                }

                SymbolCollection.SourceLocation sourceLocation = symbolCollection.Resolve(assemblyName, methodId);
                if (!resolutionCounts.TryGetValue(assemblyName, out AssemblyResolutionCount count))
                {
                    count = new AssemblyResolutionCount();
                    resolutionCounts[assemblyName] = count;
                }

                if (sourceLocation == null)
                {
                    count.unresolvedMethods += 1;
                    logger.Debug("Could not resolve method ID {methodId} from assembly {assemblyName} in trace file {traceFile}" +
                        " with symbols from {symbolDirectory} with {assemblyPatterns}", methodId, assemblyName,
                        traceFile.FilePath, symbolDirectory, assemblyPatterns.Describe());
                    continue;
                }
                else
                {
                    count.resolvedMethods += 1;
                }

                AddToLineCoverage(lineCoverage, sourceLocation);
            }

            foreach (string assemblyName in resolutionCounts.Keys)
            {
                AssemblyResolutionCount count = resolutionCounts[assemblyName];
                if (count.unresolvedMethods == 0)
                {
                    continue;
                }

                logger.Warn("{count} of {total} ({percentage}) method IDs from assembly {assemblyName} could not be resolved in trace file {traceFile} with symbols from" +
                    " {symbolDirectory} with {assemblyPatterns}. Turn on debug logging to get the exact method IDs." +
                    " This may happen if the corresponding PDB file either could not be found or could not be parsed. Ensure the PDB file for this assembly is" +
                    " in the specified PDB folder where the Upload Daemon looks for it and it is included by the PDB file include/exclude patterns configured for the UploadDaemon. " +
                    " You can exclude this assembly from the coverage analysis to suppress this warning.",
                    count.unresolvedMethods, count.TotalMethods, count.UnresolvedPercentage,
                    assemblyName, traceFile.FilePath, symbolDirectory, assemblyPatterns.Describe());
            }

            return lineCoverage;
        }

        private static void AddToLineCoverage(Dictionary<string, FileCoverage> lineCoverage, SymbolCollection.SourceLocation sourceLocation)
        {
            if (!lineCoverage.TryGetValue(sourceLocation.SourceFile, out FileCoverage fileCoverage))
            {
                fileCoverage = new FileCoverage();
                lineCoverage[sourceLocation.SourceFile] = fileCoverage;
            }

            fileCoverage.CoveredLineRanges.Add((sourceLocation.StartLine, sourceLocation.EndLine));
        }

        public class LineCoverageConversionFailedException : Exception
        {
            public LineCoverageConversionFailedException(string message) : base(message)
            {
            }
        }
    }
}
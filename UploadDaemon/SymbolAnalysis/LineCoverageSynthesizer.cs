using Cqse.ConQAT.Dotnet.Bummer;
using NLog;
using System;
using System.Collections.Generic;
using UploadDaemon.Configuration;
using UploadDaemon.Report.Simple;
using UploadDaemon.Scanning;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Converts a trace file to a line coverage report with the help of PDB files.
    /// </summary>
    public class LineCoverageSynthesizer : ILineCoverageSynthesizer
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly SymbolCollectionResolver symbolCollectionResolver;

        public LineCoverageSynthesizer()
        {
            this.symbolCollectionResolver = new SymbolCollectionResolver();
        }

        /// <inheritdoc/>
        public SimpleCoverageReport ConvertToLineCoverage(Trace trace, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            SymbolCollection symbolCollection = symbolCollectionResolver.ResolveFrom(symbolDirectory, assemblyPatterns);
            if (symbolCollection.IsEmpty)
            {
                throw new LineCoverageConversionFailedException($"Failed to convert {trace.OriginTraceFilePath} to line coverage." +
                    $" Found no symbols in {symbolDirectory} matching {assemblyPatterns.Describe()}");
            }

            return ConvertToLineCoverage(trace, symbolCollection, symbolDirectory, assemblyPatterns);
        }

        private class AssemblyResolutionCount
        {
            public int resolvedMethods = 0;
            public int unresolvedMethods = 0;
            public int methodsWithoutSourceFile = 0;
            public int methodsWithCompilerHiddenLines = 0;
            public int TotalMethods => resolvedMethods + unresolvedMethods;
            public string UnresolvedPercentage => string.Format("{0:F1}%", unresolvedMethods * 100 / (double)TotalMethods);
            public string WithoutSourceFilePercentage => string.Format("{0:F1}%", methodsWithoutSourceFile * 100 / (double)TotalMethods);
            public string WithCompilerHiddenLinesPercentage => string.Format("{0:F1}%", methodsWithCompilerHiddenLines * 100 / (double)TotalMethods);
        }

        /// <summary>
        /// Converts the given trace file to a dictionary containing all covered lines of each source file for which
        /// coverage could be resolved with the PDB files in the given symbol directory.
        ///
        /// Public for testing.
        /// </summary>
        public static SimpleCoverageReport ConvertToLineCoverage(Trace trace, SymbolCollection symbolCollection,
            string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            logger.Debug("Converting trace {traceFile} to line coverage", trace);
            Dictionary<string, AssemblyResolutionCount> resolutionCounts = new Dictionary<string, AssemblyResolutionCount>();
            Dictionary<string, FileCoverage> lineCoverageByFile = new Dictionary<string, FileCoverage>();
            HashSet<string> includedAssemblies = new HashSet<string>();
            HashSet<string> excludedAssemblies = new HashSet<string>();
            foreach ((string assemblyName, uint methodId) in trace.CoveredMethods)
            {
                if (excludedAssemblies.Contains(assemblyName) || (!includedAssemblies.Contains(assemblyName) && !assemblyPatterns.Matches(assemblyName)))
                {
                    excludedAssemblies.Add(assemblyName);
                    continue;
                }
                includedAssemblies.Add(assemblyName);

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
                        trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
                    continue;
                }
                else if (string.IsNullOrEmpty(sourceLocation.SourceFile))
                {
                    count.methodsWithoutSourceFile += 1;
                    logger.Debug("Could not resolve source file of method ID {methodId} from assembly {assemblyName} in trace file {traceFile}" +
                        " with symbols from {symbolDirectory} with {assemblyPatterns}", methodId, assemblyName,
                        trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
                    continue;
                }
                else if (sourceLocation.StartLine == PdbFile.CompilerHiddenLine || sourceLocation.EndLine == PdbFile.CompilerHiddenLine)
                {
                    count.methodsWithCompilerHiddenLines += 1;
                    logger.Debug("Resolved lines of method ID {methodId} from assembly {assemblyName} contain compiler hidden lines in trace file {traceFile}" +
                        " with symbols from {symbolDirectory} with {assemblyPatterns}", methodId, assemblyName,
                        trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
                    continue;
                }

                count.resolvedMethods += 1;
                AddToLineCoverage(lineCoverageByFile, sourceLocation);
            }

            LogResolutionFailures(trace, symbolDirectory, assemblyPatterns, resolutionCounts);

            return new SimpleCoverageReport(lineCoverageByFile);
        }

        private static void LogResolutionFailures(Trace trace, string symbolDirectory, GlobPatternList assemblyPatterns, Dictionary<string, AssemblyResolutionCount> resolutionCounts)
        {
            foreach (string assemblyName in resolutionCounts.Keys)
            {
                AssemblyResolutionCount count = resolutionCounts[assemblyName];
                LogResolutionFailuresForAssembly(trace, symbolDirectory, assemblyPatterns, assemblyName, count);
            }
        }

        private static void LogResolutionFailuresForAssembly(Trace trace, string symbolDirectory, GlobPatternList assemblyPatterns, string assemblyName, AssemblyResolutionCount count)
        {
            if (count.unresolvedMethods > 0)
            {
                logger.Debug("{count} of {total} ({percentage}) method IDs from assembly {assemblyName} could not be resolved in trace file {traceFile} with symbols from" +
                    " {symbolDirectory} with {assemblyPatterns}. Turn on debug logging to get the exact method IDs." +
                    " This may happen if the corresponding PDB file either could not be found or could not be parsed. Ensure the PDB file for this assembly is" +
                    " in the specified PDB folder where the Upload Daemon looks for it and it is included by the PDB file include/exclude patterns configured for the UploadDaemon. " +
                    " You can exclude this assembly from the coverage analysis to suppress this warning.",
                    count.unresolvedMethods, count.TotalMethods, count.UnresolvedPercentage,
                    assemblyName, trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
            }
            if (count.methodsWithoutSourceFile > 0)
            {
                logger.Debug("{count} of {total} ({percentage}) method IDs from assembly {assemblyName} do not have a source file in the corresponding PDB file." +
                    " Read from trace file {traceFile} with symbols from" +
                    " {symbolDirectory} with {assemblyPatterns}. Turn on debug logging to get the exact method IDs." +
                    " This sometimes happens and may be an indication of broken PDB files. Please make sure your PDB files are correct." +
                    " You can exclude this assembly from the coverage analysis to suppress this warning.",
                    count.methodsWithoutSourceFile, count.TotalMethods, count.WithoutSourceFilePercentage,
                    assemblyName, trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
            }
            if (count.methodsWithoutSourceFile > 0)
            {
                logger.Debug("{count} of {total} ({percentage}) method IDs from assembly {assemblyName} contain compiler hidden lines in the corresponding PDB file." +
                    " Read from trace file {traceFile} with symbols from" +
                    " {symbolDirectory} with {assemblyPatterns}. Turn on debug logging to get the exact method IDs." +
                    " This is usually not a problem as the compiler may generate additional code that does not correspond to any source code." +
                    " You can exclude this assembly from the coverage analysis to suppress this warning.",
                    count.methodsWithCompilerHiddenLines, count.TotalMethods, count.WithCompilerHiddenLinesPercentage,
                    assemblyName, trace.OriginTraceFilePath, symbolDirectory, assemblyPatterns.Describe());
            }
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

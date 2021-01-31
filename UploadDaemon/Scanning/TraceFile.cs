using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UploadDaemon.Scanning
{
    /// <summary>
    /// Represents one trace file found by the TraceFileScanner.
    /// </summary>
    public class TraceFile
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Regex TraceFileRegex = new Regex(@"^coverage_\d*_\d*.txt$");
        private static readonly Regex ProcessLineRegex = new Regex(@"^Process=(.*)", RegexOptions.IgnoreCase);
        private static readonly Regex AssemblyLineRegex = new Regex(@"^Assembly=([^:]+):(\d+)");
        private static readonly Regex CoverageLineRegex = new Regex(@"^(?:Inlined|Jitted|Called)=(\d+):(?:\d+:)?(\d+)");
        /// <summary>
        /// The lines of text contained in the trace.
        /// </summary>
        private string[] lines;

        /// <summary>
        /// Returns true if the given file name looks like a trace file.
        /// </summary>
        public static bool IsTraceFile(string fileName)
        {
            return TraceFileRegex.IsMatch(fileName);
        }

        /// <summary>
        /// The path to the file.
        /// </summary>
        public string FilePath { get; private set; }

        public TraceFile(string filePath, string[] lines)
        {
            this.FilePath = filePath;
            this.lines = lines;
        }

        /// <summary>
        /// Given the lines of text in a trace file and a version assembly (without the file extension), returns the version of that assembly in the trace file
        /// or null if the assembly cannot be found in the trace.
        /// </summary>
        public string FindVersion(string versionAssembly)
        {
            Regex versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*Version:([^ ]*).*", RegexOptions.IgnoreCase);
            Match matchingLine = lines.Select(line => versionAssemblyRegex.Match(line)).Where(match => match.Success).FirstOrDefault();
            return matchingLine?.Groups[1]?.Value;
        }

        /// <summary>
        /// Given the lines of text in a trace file, returns the process that was profiled or null if no process can be found.
        /// </summary>
        public string FindProcessPath()
        {
            foreach (string line in lines)
            {
                Match match = ProcessLineRegex.Match(line);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            return null;
        }

        /// <summary>
        /// All methods that are reported as covered. A method is identified by the name of its assembly
        /// (first elment in the tuple) and its ID (second element in the tuple).
        /// </summary>
        public List<(string, uint)> FindCoveredMethods()
        {
            Dictionary<uint, string> assemblyTokens = lines.Select(line => AssemblyLineRegex.Match(line))
                .Where(match => match.Success)
                .ToDictionary(match => Convert.ToUInt32(match.Groups[2].Value), match => match.Groups[1].Value);

            List<(string, uint)> coveredMethods = new List<(string, uint)>();
            foreach (string line in lines)
            {
                // Try matching a coverage line first, because this is the most prevalent case.
                Match coverageMatch = CoverageLineRegex.Match(line);
                if (coverageMatch.Success)
                {
                    uint assemblyId = Convert.ToUInt32(coverageMatch.Groups[1].Value);
                    if (!assemblyTokens.TryGetValue(assemblyId, out string assemblyName))
                    {
                        logger.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                            " Please report it to CQSE. Coverage for this assembly will be ignored.", FilePath, assemblyId);
                        continue;
                    }
                    coveredMethods.Add((assemblyName, Convert.ToUInt32(coverageMatch.Groups[2].Value)));
                }
            }

            return coveredMethods;
        }

        /// <summary>
        /// Given the lines from a trace file, returns true if the trace file contains no actual coverage information - only metadata.
        /// </summary>
        public bool IsEmpty()
        {
            return !lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
        }
    }
}

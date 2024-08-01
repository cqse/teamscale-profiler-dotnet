using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;

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

        /// <summary>
        /// The lines of text contained in the trace.
        /// </summary>
        public string[] Lines
        {
            get; 
            private set;
        }

        /// <summary>
        /// Returns true if the given file name looks like a trace file.
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
            this.Lines = lines;
        }

        /// <summary>
        /// Given the lines of text in a trace file and a version assembly (without the file extension), returns the version of that assembly in the trace file
        /// or null if the assembly cannot be found in the trace.
        /// </summary>
        public string FindVersion(string versionAssembly)
        {
            Regex versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*Version:([^ ]*).*", RegexOptions.IgnoreCase);
            Match matchingLine = Lines.Select(line => versionAssemblyRegex.Match(line)).Where(match => match.Success).FirstOrDefault();
            return matchingLine?.Groups[1]?.Value;
        }

        public ICoverageReport ToReport(Func<Trace, SimpleCoverageReport> traceResolver, Dictionary<uint, (string name, string path)> assemblies)
        {
            return new TraceFileParser(FilePath, Lines, assemblies, traceResolver).ParseTraceFile();
        }

        /// <summary>
        /// Given the lines of text in a trace file, returns the process that was profiled or null if no process can be found.
        /// </summary>
        public string FindProcessPath()
        {
            foreach (string line in Lines)
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
        /// Given the lines from a trace file, returns true if the trace file contains no actual coverage information - only metadata.
        /// </summary>
        public bool IsEmpty()
        {
            return !Lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined=") || line.StartsWith("Called="));
        }
    }
}

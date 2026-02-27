using System.Linq;
using System.Text.RegularExpressions;

namespace UploadDaemon.Scanning
{
    /// <summary>
    /// Represents one trace file found by the TraceFileScanner.
    /// </summary>
    public class TraceFile
    {
        private static readonly Regex TraceFileRegex = new Regex(@"^coverage_\d*_\d*.txt$");
        private static readonly Regex ProcessRegex = new Regex(@"^Process=(.*)", RegexOptions.IgnoreCase);

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

        /// <summary>
        /// The lines of text contained in the trace.
        /// </summary>
        public string[] Lines { get; private set; }

        public TraceFile(string filePath, string[] lines)
        {
            this.FilePath = filePath;
            this.Lines = lines;
        }

        /// <summary>
        /// Given the lines of text in a trace file, returns the process that was profiled or null if no process can be found.
        /// </summary>
        public string FindProcessPath()
        {
            foreach (string line in Lines)
            {
                Match match = ProcessRegex.Match(line);
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
            return !Lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
        }
    }
}
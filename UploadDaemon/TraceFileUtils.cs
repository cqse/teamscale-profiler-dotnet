using System.Linq;
using System.Text.RegularExpressions;

namespace UploadDaemon
{
    /// <summary>
    /// Utilities for working with trace files.
    /// </summary>
    public class TraceFileUtils
    {
        private static readonly Regex TraceFileRegex = new Regex(@"^coverage_\d*_\d*.txt$");
        private static readonly Regex ProcessRegex = new Regex(@"^Process=(.*)", RegexOptions.IgnoreCase);

        /// <summary>
        /// Given the lines of text in a trace file and a version assembly (without the file extension), returns the version of that assembly in the trace file
        /// or null if the assembly cannot be found in the trace.
        /// </summary>
        public static string FindVersion(string[] lines, string versionAssembly)
        {
            Regex versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*Version:([^ ]*).*", RegexOptions.IgnoreCase);
            Match matchingLine = lines.Select(line => versionAssemblyRegex.Match(line)).Where(match => match.Success).FirstOrDefault();
            return matchingLine?.Groups[1]?.Value;
        }

        /// <summary>
        /// Returns true if the given file name looks like a trace file.
        /// </summary>
        public static bool IsTraceFile(string fileName)
        {
            return TraceFileRegex.IsMatch(fileName);
        }

        /// <summary>
        /// Given the lines of text in a trace file, returns the process that was profiled or null if no process can be found.
        /// </summary>
        public static string FindProcessPath(string[] lines)
        {
            foreach (string line in lines)
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
        public static bool IsEmpty(string[] lines)
        {
            return !lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
        }
    }
}
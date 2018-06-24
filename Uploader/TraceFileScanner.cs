using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Scans the trace directory for trace files that are ready to upload or archive.
/// </summary>
public class TraceFileScanner
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static readonly Regex TRACE_FILE_REGEX = new Regex(@"^coverage_\d*_\d*.txt$");

    private readonly string traceDirectory;
    private readonly Regex versionAssemblyRegex;
    private readonly IFileSystem fileSystem;

    public TraceFileScanner(string traceDirectory, string versionAssembly, IFileSystem fileSystem)
    {
        this.traceDirectory = traceDirectory;
        this.versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*Version:([^ ]*).*", RegexOptions.IgnoreCase);
        this.fileSystem = fileSystem;
    }

    /// <summary>
    /// Returns all trace files that can be uploaded or archived.
    /// </summary>
    public IEnumerable<ScannedFile> ListTraceFilesReadyForUpload()
    {
        IEnumerable<string> files;
        try
        {
            files = fileSystem.Directory.EnumerateFiles(traceDirectory);
        }
        catch (Exception e)
        {
            logger.Error(e, "Unable to list files in {traceDirectory}. Will retry later", traceDirectory);
            yield break;
        }

        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            if (!IsTraceFile(fileName))
            {
                continue;
            }

            ScannedFile scannedFile = ScanFile(filePath);
            if (scannedFile != null)
            {
                yield return scannedFile;
            }
        }
    }

    /// <summary>
    /// Scans the given file path and returns the resulting ScannedFile or null in case the file should be ignored.
    /// </summary>
    private ScannedFile ScanFile(string filePath)
    {
        string[] lines;
        try
        {
            lines = fileSystem.File.ReadAllLines(filePath);
        }
        catch (Exception e)
        {
            logger.Error(e, "Unable to read from file {trace}. Ignoring this file", filePath);
            return null;
        }

        if (!IsFinished(lines))
        {
            return null;
        }

        string version = FindVersion(lines);
        return new ScannedFile
        {
            FilePath = filePath,
            Version = version,
        };
    }

    private bool IsFinished(string[] lines)
    {
        return lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
    }

    private string FindVersion(string[] lines)
    {
        string matchingLine = lines.FirstOrDefault(line => versionAssemblyRegex.IsMatch(line));
        if (matchingLine == null)
        {
            return null;
        }

        Match match = versionAssemblyRegex.Match(matchingLine);
        return match.Groups[1].Value;
    }

    private bool IsTraceFile(string fileName)
    {
        return TRACE_FILE_REGEX.IsMatch(fileName);
    }

    /// <summary>
    /// A single file that can either be uploaded or archived.
    /// </summary>
    public class ScannedFile
    {
        /// <summary>
        /// The path to the file.
        /// </summary>
        public string FilePath;

        /// <summary>
        /// The parsed version of the version assembly or null in case the version assembly was not in the file.
        /// </summary>
        public string Version;

        public override bool Equals(object obj)
        {
            var file = obj as ScannedFile;
            return file != null &&
                   FilePath == file.FilePath &&
                   Version == file.Version;
        }

        public override int GetHashCode()
        {
            var hashCode = -1491167301;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FilePath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Version);
            return hashCode;
        }
    }
}

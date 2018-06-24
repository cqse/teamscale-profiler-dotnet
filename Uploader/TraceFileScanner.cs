using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Scans the trace directory for trace files that are ready to upload or archive.
/// </summary>
class TraceFileScanner
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private static readonly Regex TRACE_FILE_REGEX = new Regex(@"^coverage_\d*_\d*.txt$");

    private readonly string traceDirectory;
    private readonly Regex versionAssemblyRegex;

    TraceFileScanner(string traceDirectory, string versionAssembly)
    {
        this.traceDirectory = traceDirectory;
        this.versionAssemblyRegex = new Regex(@"^Assembly=" + Regex.Escape(versionAssembly) + @".*", RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Returns all trace files that can be uploaded or archived.
    /// </summary>
    public IEnumerable<ScannedFile> ListTraceFilesReadyForUpload()
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(traceDirectory);
        }
        catch (Exception e)
        {
            logger.Error(e, "Unable to list files in {traceDirectory}. Will retry later", traceDirectory);
            yield break;
        }

        foreach (string fileName in files)
        {
            if (!IsTraceFile(fileName))
            {
                continue;
            }

            string filePath = Path.Combine(traceDirectory, fileName);
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
            lines = File.ReadAllLines(filePath);
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

        if (ContainsVersionAssembly(lines))
        {
            return new ScannedFile
            {
                FilePath = filePath,
                Result = EScanResult.READY_FOR_UPLOAD
            };
        }

        return new ScannedFile
        {
            FilePath = filePath,
            Result = EScanResult.MISSING_VERSION_ASSEMBLY
        };
    }

    private bool IsFinished(string[] lines)
    {
        return lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined="));
    }

    private bool ContainsVersionAssembly(string[] lines)
    {
        return lines.Any(line => versionAssemblyRegex.IsMatch(line));
    }

    private bool IsTraceFile(string fileName)
    {
        return TRACE_FILE_REGEX.IsMatch(fileName);
    }

    /// <summary>
    /// Indicates the result state of a scanned file.
    /// </summary>
    public enum EScanResult
    {
        /// <summary>
        /// File can be uploaded.
        /// </summary>
        READY_FOR_UPLOAD,

        /// <summary>
        /// File does not contain the version assembly, i.e. should be archived.
        /// </summary>
        MISSING_VERSION_ASSEMBLY
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
        /// The result of the scan.
        /// </summary>
        public EScanResult Result;
    }
}

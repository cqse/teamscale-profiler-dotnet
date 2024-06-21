using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;
using UploadDaemon.Report.Testwise;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.Scanning
{
    /// <summary>
    /// Represents one trace file found by the TraceFileScanner.
    /// </summary>
    public class TraceFile
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The name of the Resource .resx file that holed information about embedded upload targets.
        /// </summary>
        private const String TeamscaleResourceName = "Teamscale";


        private static readonly Regex TraceFileRegex = new Regex(@"^coverage_\d*_\d*.txt$");
        private static readonly Regex ProcessLineRegex = new Regex(@"^Process=(.*)", RegexOptions.IgnoreCase);
        private static readonly Regex AssemblyLineRegex = new Regex(@"^Assembly=([^:]+):(\d+)");
        private static readonly Regex CoverageLineRegex = new Regex(@"^(?:Inlined|Jitted|Called)=(?<assembly>\d+):(?:\d+:)?(?<functionToken>\d+)");
        private static readonly Regex TestCaseLineRegex = new Regex(@"^(?:Test)=(?<event>Start|End):(?<date>[^:]+):(?<testname>[^:]+)(?::(?<duration>\d+))?");

        public readonly Dictionary<uint, string> assemblies = new Dictionary<uint, string>();


        /// <summary>
        /// The lines of text contained in the trace.
        /// </summary>
        private string[] lines;

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

        public ICoverageReport ToReport(Func<Trace, SimpleCoverageReport> traceResolver)
        {

            DateTime traceStart = default;
            bool isTestwiseTrace = false;
            Trace noTestTrace = new Trace();
            string noTestName = "No Test";
            string currentTestName = noTestName;
            DateTime currentTestStart = default;
            Trace currentTestTrace = noTestTrace;
            DateTime currentTestEnd;
            long testDuration = 0;
            string currentTestResult;
            IList<Test> tests = new List<Test>();

            foreach (string line in lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, count: 2);
                if (keyValuePair.Length < 2)
                {
                    logger.Warn("Invalid line in trace file {}: {}", FilePath, line);
                    continue;
                }
                string key = keyValuePair[0];
                string value = keyValuePair[1];

                switch (key)
                {
                    case "Started":
                        traceStart = ParseProfilerDateTimeString(value);
                        break;
                    case "Info":
                        isTestwiseTrace |= value.StartsWith("TIA enabled");
                        break;
                    case "Assembly":
                        Match assemblyMatch = AssemblyLineRegex.Match(line);
                        assemblies[Convert.ToUInt32(assemblyMatch.Groups[2].Value)] = assemblyMatch.Groups[1].Value;
                        break;
                    case "Test":
                        Match testCaseMatch = TestCaseLineRegex.Match(line);
                        string startOrEnd = testCaseMatch.Groups["event"].Value;
                        if (startOrEnd.Equals("Start"))
                        {
                            currentTestName = testCaseMatch.Groups["testname"].Value;
                            currentTestStart = ParseProfilerDateTimeString(testCaseMatch.Groups["date"].Value);
                            currentTestTrace = new Trace() { OriginTraceFilePath = this.FilePath };
                        }
                        else if (startOrEnd.Equals("End"))
                        {
                            if (currentTestTrace == noTestTrace)
                            {
                                throw new InvalidTraceFileException($"encountered end of test that did not start: {line}");
                            }

                            currentTestEnd = ParseProfilerDateTimeString(testCaseMatch.Groups["date"].Value);
                            currentTestResult = testCaseMatch.Groups["testname"].Value;
                            Int64.TryParse(testCaseMatch.Groups["duration"].Value, out testDuration);
                            tests.Add(new Test(currentTestName, traceResolver(currentTestTrace))
                            {
                                Start = currentTestStart,
                                End = currentTestEnd,
                                DurationMillis = testDuration,
                                Result = currentTestResult
                            });
                            currentTestName = noTestName;
                            currentTestTrace = noTestTrace;
                        }
                        break;
                    case "Inlined":
                    case "Jitted":
                    case "Called":
                        Match coverageMatch = CoverageLineRegex.Match(line);
                        uint assemblyId = Convert.ToUInt32(coverageMatch.Groups["assembly"].Value);
                        if (!assemblies.TryGetValue(assemblyId, out string assemblyName))
                        {
                            logger.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                                " Please report it to CQSE. Coverage for this assembly will be ignored.", FilePath, assemblyId);
                            continue;
                        }
                        currentTestTrace.CoveredMethods.Add((assemblyName, Convert.ToUInt32(coverageMatch.Groups["functionToken"].Value)));
                        break;
                    case "Stopped":
                        if (currentTestTrace.IsEmpty)
                        {
                            break;
                        }
                        if (currentTestTrace == noTestTrace)
                        {
                            currentTestStart = traceStart;
                        }
                        currentTestEnd = ParseProfilerDateTimeString(value);
                        currentTestResult = "SKIPPED";
                        tests.Add(new Test(currentTestName, traceResolver(currentTestTrace))
                        {
                            Start = currentTestStart,
                            End = currentTestEnd,
                            Result = currentTestResult
                        });
                        currentTestTrace = noTestTrace;
                        break;
                }
            }

            List<(string project, RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();
            SearchForEmbeddedUploadTargets(assemblies, embeddedUploadTargets);

            if (isTestwiseTrace)
            {
                return new TestwiseCoverageReport(tests.ToArray(), embeddedUploadTargets);
            }
            else
            {
                return traceResolver(noTestTrace);
            }
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

        private DateTime ParseProfilerDateTimeString(string dateTimeString)
        {
            // 20210129_1026440836
            string format = "yyyyMMdd_HHmmss0fff";
            return DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Given the lines from a trace file, returns true if the trace file contains no actual coverage information - only metadata.
        /// </summary>
        public bool IsEmpty()
        {
            return !lines.Any(line => line.StartsWith("Jitted=") || line.StartsWith("Inlined=") || line.StartsWith("Called="));
        }

        /// <summary>
        /// Checks the loaded assemblies for resources that contain information about target revision or teamscale projects.
        /// </summary>
        private void SearchForEmbeddedUploadTargets(Dictionary<uint, string> assemblyTokens, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets)
        {
            foreach (KeyValuePair<uint, string> entry in assemblyTokens)
            {
                Assembly assembly = LoadAssemblyFromPath(entry.Value);
                if (assembly == null || assembly.DefinedTypes == null)
                {
                    continue;
                }
                TypeInfo teamscaleResourceType = assembly.DefinedTypes.FirstOrDefault(x => x.Name == TeamscaleResourceName) ?? null;
                if (teamscaleResourceType == null)
                {
                    continue;
                }
                logger.Info("Found embedded Teamscale resource in {assembly} that can be used to identify upload targets.", assembly);
                ResourceManager teamscaleResourceManager = new ResourceManager(teamscaleResourceType.FullName, assembly);
                string embeddedTeamscaleProject = teamscaleResourceManager.GetString("Project");
                string embeddedRevision = teamscaleResourceManager.GetString("Revision");
                string embeddedTimestamp = teamscaleResourceManager.GetString("Timestamp");
                AddUploadTarget(embeddedRevision, embeddedTimestamp, embeddedTeamscaleProject, uploadTargets, assembly.FullName);
            }
        }

        /// <summary>
        /// Adds a revision or timestamp and optionally a project to the list of upload targets. This method checks if both, revision and timestamp, are declared, or neither.
        /// </summary>
        /// <param name="revision"></param>
        /// <param name="timestamp"></param>
        /// <param name="project"></param>
        /// <param name="uploadTargets"></param>
        /// <param name="origin"></param>
        public static void AddUploadTarget(string revision, string timestamp, string project, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> uploadTargets, string origin)
        {
            Logger logger = LogManager.GetCurrentClassLogger();

            if (revision == null && timestamp == null)
            {
                logger.Error("Not all required fields in {origin}. Please specify either 'Revision' or 'Timestamp'", origin);
                return;
            }
            if (revision != null && timestamp != null)
            {
                logger.Error("'Revision' and 'Timestamp' are both set in {origin}. Please set only one, not both.", origin);
                return;
            }
            if (revision != null)
            {
                uploadTargets.Add((project, new RevisionOrTimestamp(revision, true)));
            }
            else
            {
                uploadTargets.Add((project, new RevisionOrTimestamp(timestamp, false)));
            }
        }

        private Assembly LoadAssemblyFromPath(string path)
        {
            if (String.IsNullOrEmpty(path))
            {
                return null;
            }
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(path);
                // Check that defined types can actually be loaded
                if (assembly == null)
                {
                    return null;
                }
                IEnumerable<TypeInfo> ignored = assembly.DefinedTypes;
            }
            catch (Exception e)
            {
                logger.Debug("Could not load {assembly}. Skipping upload resource discovery. {e}", path, e);
                return null;
            }
            return assembly;
        }
    }
}

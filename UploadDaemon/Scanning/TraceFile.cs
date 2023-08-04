using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;
using UploadDaemon.Report.Testwise;

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
        private static readonly Regex TestCaseLineRegex = new Regex(@"^(?:Test)=((?:Start|End)):([^:]+):([^:]+)(?::(\d+))?");

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
            Dictionary<uint, string> assemblyTokens = new Dictionary<uint, string>();

            DateTime traceStart = default;
            bool isTestwiseTrace = false;
            Trace noTestTrace = new Trace();
            string noTestName = "No Test";
            string currentTestName = noTestName;
            DateTime currentTestStart = default;
            Trace currentTestTrace = noTestTrace;
            DateTime currentTestEnd;
            long testDuration;
            string currentTestResult;
            IList<Test> tests = new List<Test>();

            foreach (string line in lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, count:2);
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
                        assemblyTokens[Convert.ToUInt32(assemblyMatch.Groups[2].Value)] = assemblyMatch.Groups[1].Value;
                        break;
                    case "Test":
                        Match testCaseMatch = TestCaseLineRegex.Match(line);
                        string startOrEnd = testCaseMatch.Groups[1].Value;
                        if (startOrEnd.Equals("Start"))
                        {
                            currentTestName = testCaseMatch.Groups[3].Value;
                            currentTestStart = ParseProfilerDateTimeString(testCaseMatch.Groups[2].Value);
                            currentTestTrace = new Trace() { OriginTraceFilePath = this.FilePath };
                        }
                        else if (startOrEnd.Equals("End"))
                        {
                            if (currentTestTrace == noTestTrace)
                            {
                                throw new InvalidTraceFileException($"encountered end of test that did not start: {line}");
                            }

                            currentTestEnd = ParseProfilerDateTimeString(testCaseMatch.Groups[2].Value);
                            currentTestResult = testCaseMatch.Groups[3].Value;
                            Int64.TryParse(testCaseMatch.Groups[4].Value, out testDuration);
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
                        uint assemblyId = Convert.ToUInt32(coverageMatch.Groups[1].Value);
                        if (!assemblyTokens.TryGetValue(assemblyId, out string assemblyName))
                        {
                            logger.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                                " Please report it to CQSE. Coverage for this assembly will be ignored.", FilePath, assemblyId);
                            continue;
                        }
                        currentTestTrace.CoveredMethods.Add((assemblyName, Convert.ToUInt32(coverageMatch.Groups[2].Value)));
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

            if (isTestwiseTrace)
            {
                return new TestwiseCoverageReport(tests.ToArray());
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
    }
}

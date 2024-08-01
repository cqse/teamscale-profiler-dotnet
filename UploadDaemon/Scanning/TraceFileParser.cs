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
    internal class TraceFileParser
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly string NO_TEST = "No Test";

        private static readonly Regex TestCaseStartRegex = new Regex(@"Start:(?<date>[^:]+):(?<testname>.+)");
        private static readonly Regex TestCaseEndRegex = new Regex(@"End:(?<date>[^:]+):(?<testresult>[^:]+)(?::(?<duration>\d+))?");

        private readonly string FilePath;
        private readonly string[] Lines;
        private readonly Func<Trace, SimpleCoverageReport> TraceResolver;
        private readonly Dictionary<uint, (string name, string path)> Assemblies;

        private Trace TraceOutsideTestExecution = new Trace();
        private IList<Test> Tests = new List<Test>();

        private DateTime TraceStart = default;
        private string CurrentTestName = "No Test";
        private Trace CurrentTestTrace = new Trace();

        private DateTime CurrentTestStart = default;
        private DateTime CurrentTestEnd;

        private long TestDuration = 0;
        private string CurrentTestResult;

        public TraceFileParser(string filePath, string[] lines, Dictionary<uint, (string name, string path)> assemblies, Func<Trace, SimpleCoverageReport> traceResolver)
        {
            FilePath = filePath;
            Lines = lines;
            Assemblies = assemblies;
            TraceResolver = traceResolver;
        }

        public ICoverageReport ParseTraceFile()
        {
            foreach (string line in Lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, count: 2);
                string key = keyValuePair[0];
                string value = keyValuePair[1];
                switch (key)
                {
                    case "Started":
                        HandleTraceFileStart(value);
                        break;
                    case "Stopped":
                        HandleTraceFileEnd(value);
                        break;
                    case "Test":
                        HandleTestStartOrEnd(value);
                        break;

                    case "Inlined":
                    case "Jitted":
                    case "Called":
                        HandleCoverageLine(value);
                        break;
                }
            }

            if (Tests.Count > 0)
            {
                return new TestwiseCoverageReport(Tests.ToArray());
            }
            else
            {
                return TraceResolver(TraceOutsideTestExecution);
            }
        }

        private void HandleTraceFileStart(string startTime)
        {
            TraceStart = ParseProfilerDateTimeString(startTime);
        }

        private void HandleTraceFileEnd(string endTime)
        {
            if (CurrentTestTrace.IsEmpty)
            {
                return;
            }
            if (CurrentTestTrace == TraceOutsideTestExecution)
            {
                CurrentTestStart = TraceStart;
            }
            CurrentTestEnd = ParseProfilerDateTimeString(endTime);
            CurrentTestResult = "SKIPPED";
            Tests.Add(new Test(CurrentTestName, TraceResolver(CurrentTestTrace))
            {
                Start = CurrentTestStart,
                End = CurrentTestEnd,
                Result = CurrentTestResult
            });
            CurrentTestTrace = TraceOutsideTestExecution;
        }

        private void HandleTestStartOrEnd(string testMessage)
        {
            if (testMessage.StartsWith("Start"))
            {
                Match testCaseMatch = TestCaseStartRegex.Match(testMessage);
                CurrentTestName = testCaseMatch.Groups["testname"].Value;
                CurrentTestStart = ParseProfilerDateTimeString(testCaseMatch.Groups["date"].Value);
                CurrentTestTrace = new Trace() { OriginTraceFilePath = FilePath };
            }
            else
            {
                Match testCaseMatch = TestCaseEndRegex.Match(testMessage);
                if (CurrentTestTrace == TraceOutsideTestExecution)
                {
                    throw new InvalidTraceFileException($"encountered end of test that did not start: {testMessage}");
                }
                CurrentTestEnd = ParseProfilerDateTimeString(testCaseMatch.Groups["date"].Value);
                CurrentTestResult = testCaseMatch.Groups["testresult"].Value;
                Int64.TryParse(testCaseMatch.Groups["duration"].Value, out TestDuration);
                Tests.Add(new Test(CurrentTestName, TraceResolver(CurrentTestTrace))
                {
                    Start = CurrentTestStart,
                    End = CurrentTestEnd,
                    DurationMillis = TestDuration,
                    Result = CurrentTestResult
                });
                CurrentTestName = NO_TEST;
                CurrentTestTrace = TraceOutsideTestExecution;
            }
        }

        private void HandleCoverageLine(string coverage)
        {
            String[] coverageMatch = coverage.Split(new[] {':' }, count: 2);
            uint assemblyId = Convert.ToUInt32(coverageMatch[1]);
            if (!Assemblies.TryGetValue(assemblyId, out (string, string) entry))
            {
                logger.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                    " Please report it to CQSE. Coverage for this assembly will be ignored.", FilePath, assemblyId);
                return;
            }
            CurrentTestTrace.CoveredMethods.Add((entry.Item1, Convert.ToUInt32(coverageMatch[2])));
        }

        private DateTime ParseProfilerDateTimeString(string dateTimeString)
        {
            // 20210129_1026440836
            string format = "yyyyMMdd_HHmmss0fff";
            return DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
        }
    }
}

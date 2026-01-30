using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UploadDaemon.Report;
using UploadDaemon.Report.Testwise;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Scanning
{
    public class TraceFileParser
    {
        private static readonly Logger LOGGER = LogManager.GetCurrentClassLogger();
        private const string NO_TEST = "No Test";

        private static readonly Regex TestCaseStartRegex = new Regex(@"Start:(?<date>[^:]+):(?<testname>.+)");
        private static readonly Regex TestCaseEndRegex = new Regex(@"End:(?<date>[^:]+):(?<testresult>[^:]+)(?::(?<duration>\d+))?");

        private readonly string FilePath;
        private readonly string[] Lines;
        private readonly ILineCoverageSynthesizer LineCoverageSynthesizer;
        private readonly Dictionary<uint, (string name, string path)> Assemblies;

        private readonly Trace TraceOutsideTestExecution = new Trace();
        private readonly IList<Test> Tests = new List<Test>();

        private DateTime TraceStart = default;
        private string CurrentTestName = "No Test";
        //TODO can we move this to a member variable, or do we reuse this info?
        private bool Testwise = false;
        private Trace CurrentTestTrace;


        private DateTime CurrentTestStart = default;
        private DateTime CurrentTestEnd;

        private long TestDuration = 0;
        private string CurrentTestResult;

        public TraceFileParser(TraceFile traceFile, Dictionary<uint, (string name, string path)> assemblies, ILineCoverageSynthesizer lineCoverageSynthesizer)
        {
            FilePath = traceFile.FilePath;
            Lines = traceFile.Lines;
            Assemblies = assemblies;
            LineCoverageSynthesizer = lineCoverageSynthesizer;

            CurrentTestTrace = TraceOutsideTestExecution;
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
                        HandleTestEvent(value);
                        break;

                    case "Inlined":
                    case "Jitted":
                    case "Called":
                        HandleCoverageLine(value);
                        break;
                    case "Info":
                        if (value.StartsWith("TIA enabled")) {
                            Testwise = true;
                        }
                        break;
                }
            }

            if (Testwise)
            {
                //TODO why do we need a traceresolver in the other case when we can just save the tests here? is it resolved on another path?
                return new TestwiseCoverageReport(Tests.ToArray());
            }
            else
            {
                return LineCoverageSynthesizer.ConvertToLineCoverage(TraceOutsideTestExecution);
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
            Tests.Add(new Test(CurrentTestName, LineCoverageSynthesizer.ConvertToLineCoverage(CurrentTestTrace))
            {
                Start = CurrentTestStart,
                End = CurrentTestEnd,
                Result = CurrentTestResult
            });
            CurrentTestTrace = TraceOutsideTestExecution;
        }

        private void HandleTestEvent(string testMessage)
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
                long.TryParse(testCaseMatch.Groups["duration"].Value, out TestDuration);
                Tests.Add(new Test(CurrentTestName, LineCoverageSynthesizer.ConvertToLineCoverage(CurrentTestTrace))
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
            String[] coverageMatch = coverage.Split(new[] {':'}, count: 2);
            uint assemblyId = Convert.ToUInt32(coverageMatch[0]);
            if (!Assemblies.TryGetValue(assemblyId, out (string, string) entry))
            {
                LOGGER.Warn("Invalid trace file {traceFile}: could not resolve assembly ID {assemblyId}. This is a bug in the profiler." +
                    " Please report it to CQSE. Coverage for this assembly will be ignored.", FilePath, assemblyId);
                return;
            }
            CurrentTestTrace.CoveredMethods.Add((entry.Item1, Convert.ToUInt32(coverageMatch[1])));
        }

        private DateTime ParseProfilerDateTimeString(string dateTimeString)
        {
            // 20210129_1026440836
            string format = "yyyyMMdd_HHmmss0fff";
            return DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture);
        }
    }
}

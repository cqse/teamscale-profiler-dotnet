
using Cqse.Teamscale.Profiler.Commons.Ipc;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    public class TiaProfiler : Profiler
    {
        private readonly IpcConfig ipcConfig;

        public TiaProfiler(DirectoryInfo basePath, DirectoryInfo targetDir, IpcConfig ipcConfig) : base(basePath, targetDir)
        {
            this.ipcConfig = ipcConfig;
            this.LightMode = true;
        }
            
        public override void RegisterOn(ProcessStartInfo processInfo)
        {
            base.RegisterOn(processInfo);
            
            processInfo.Environment["COR_PROFILER_TIA"] = "true";
            processInfo.Environment["COR_PROFILER_TIA_SUBSCRIBE_SOCKET"] = ipcConfig.PublishSocket; // PUB-SUB
            processInfo.Environment["COR_PROFILER_TIA_REQUEST_SOCKET"] = ipcConfig.RequestSocket; // REQ-REP
        }

        public TiaTestResult Result
        {
                get
                {
                    FileInfo actualTrace = GetSingleTraceFile();
                    TiaTestResult testResult = new TiaTestResult(File.ReadAllLines(actualTrace.FullName));
                    Assert.That(testResult.TraceLines, Has.One.EqualTo($"Info=TIA enabled. SUB: {ipcConfig.PublishSocket} REQ: {ipcConfig.RequestSocket}"));
                    Assert.That(testResult.TraceLines, Has.One.StartsWith("Stopped="));
                    return testResult;
                }
        }

        public class TiaTestResult
        {
            public IEnumerable<string> TraceLines { get; }
            public List<TestCase> TestCases { get; }

            public IEnumerable<string> TestCaseNames => TestCases.Select(t => t.Name);

            public List<TestCase> this[string testName]
            {
                get => GetTestCases(testName);
            }

            public TiaTestResult(IEnumerable<string> traceLines)
            {
                this.TraceLines = traceLines;
                this.TestCases = TestCase.FromTraceLines(this.TraceLines);
            }

            internal List<TestCase> GetTestCases(string testName)
            {
                return TestCases.Where(testCase => testCase.Name == testName).ToList();
            }

            internal TestCase GetTestCase(string testName)
            {
                IEnumerable<TestCase> testCases = GetTestCases(testName);
                Assert.That(testCases, Has.Exactly(1).Items);
                return testCases.First();
            }
        }

        public class TestCase
        {
            public string Name { get; }
            public IEnumerable<string> TraceLines { get; }

            public bool IsSynthetic => string.IsNullOrEmpty(this.Name);

            private TestCase(string name, IEnumerable<string> lines)
            {
                this.Name = name;
                this.TraceLines = lines;
            }

            internal static List<TestCase> FromTraceLines(IEnumerable<string> traceLines)
            {
                List<TestCase> testCases = new List<TestCase>();
                List<string> currentLines = new List<string>();
                testCases.Add(new TestCase(string.Empty, currentLines));
                foreach (var line in traceLines)
                {
                    if (line.StartsWith("Test=Start:"))
                    {
                        string[] parts = line.Substring("Test=".Length).Split(':');
                        bool mergeNext = false;
                        parts = parts.Aggregate(new List<string>(), (acc, part) =>
                        {
                            // @"Test = Start:20210126_0105500760:Test\:Case\\\\\:\\\:";
                            string unescaped = part.Replace("\\\\", "\\");
                            bool mergeCurrent = mergeNext;
                            if (part.Reverse().TakeWhile(c => c == '\\').Count() % 2 != 0)
                            {
                                unescaped = unescaped.Substring(0, unescaped.Length - 1) + ":";
                                mergeNext = true;
                            }
                            else
                            {
                                mergeNext = false;
                            }

                            if (mergeCurrent)
                            {
                                acc[acc.Count - 1] += unescaped;
                            }
                            else
                            {
                                acc.Add(unescaped);
                            }

                            return acc;
                        }).ToArray();
                        currentLines = new List<string>();
                        testCases.Add(new TestCase(parts[2], currentLines));
                    }

                    currentLines.Add(line);
                }

                return testCases;
            }
        }
    }
}
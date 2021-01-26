using Cqse.Teamscale.Profiler.Commons.Ipc;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    /// <summary>
    /// Test case for coverage profiler.
    /// </summary>
    [TestFixture(Bitness.x64, IpcImplementation.NetMQ)]
    [TestFixture(Bitness.x86, IpcImplementation.NetMQ)]
    [TestFixture(Bitness.x64, IpcImplementation.Native)]
    [TestFixture(Bitness.x86, IpcImplementation.Native)]
    public class TiaProfilerTest : ProfilerTestBase
    {
        private readonly Bitness bitness;
        private readonly IpcImplementation ipcImplementation;
        private RecordingProfilerIpc profilerIpc;

        public enum IpcImplementation
        {
            /// <summary>
            /// The default NetMQ based IPC server implementation
            /// </summary>
            NetMQ,

            /// <summary>
            /// Alternate native libzmq based IPC implementation
            /// </summary>
            Native,
        }

        public TiaProfilerTest(Bitness bitness, IpcImplementation ipcImplementation)
        {
            this.bitness = bitness;
            this.ipcImplementation = ipcImplementation;
        }

        [SetUp]
        public void StartZmq()
        {
            profilerIpc = CreateProfilerIpc();
        }

        protected virtual RecordingProfilerIpc CreateProfilerIpc(IpcConfig config = null)
        {
            if (this.ipcImplementation == IpcImplementation.Native)
            {
                return new NativeRecordingProfilerIpc(config);
            }

            return new RecordingProfilerIpc(config);
        }

        [TearDown]
        public void StopZmq()
        {
            profilerIpc?.Dispose();
        }

        [Test]
        public void TestCaseDefinedBeforeStartup()
        {
            profilerIpc.StartTest("startup");
            TiaTestResult testResult = StartTiaTestProcess().Stop();

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "startup" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.None.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [Test]
        public void TestCaseWithSpecialCharacters()
        {
            profilerIpc.StartTest(@"Test:Case\\:\:");
            TiaTestResult testResult = StartTiaTestProcess().Stop();

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, @"Test:Case\\:\:" }));
        }

        [Test]
        public void TestCaseDefinedAfterStartup()
        {
            TiaTestResult testResult = StartTiaTestProcess().RunTestCase("A").Stop();

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [Test]
        public void ThreeTestCasesWithDifferentAction()
        {
            TiaTestResult testResult = StartTiaTestProcess().RunTestCase("A", "B", "C").Stop();

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A", "B", "C" }));
            Assert.That(testResult["A"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["B"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["C"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
        }

        [Test]
        public void ThreeTestCasesWithSameAction()
        {
            TiaTestResult testResult = StartTiaTestProcess().RunTestCase("A", "A", "A").Stop();

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A", "A", "A" }));
            Assert.That(testResult["A"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["A"][1].TraceLines, Has.None.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["A"][2].TraceLines, Has.None.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
        }

        [Test]
        public void NoIpcRunning()
        {
            profilerIpc.StartTest("should not be triggered");
            profilerIpc.Dispose();

            TiaTestResult testResult = StartTiaTestProcess().Stop(assertReceivedRequests: false);

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(profilerIpc.ReceivedRequests, Is.Empty);

            profilerIpc = null;
        }

        [Ignore("TODO: No idea why this is not working... manual tests have no problem with this.")]
        [Test]
        public void IpcStartedAfterStartup()
        {
            RecordingProfilerIpc oldProfilerIpc = profilerIpc;
            oldProfilerIpc.StartTest("should not be triggered");
            oldProfilerIpc.Dispose();

            TiaTestProcess process = StartTiaTestProcess();

            profilerIpc = CreateProfilerIpc(oldProfilerIpc.Config);

            TiaTestResult testResult = process.RunTestCase("A").Stop(assertReceivedRequests: false);

            Assert.That(oldProfilerIpc.ReceivedRequests, Is.Empty);
            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_disconnected" }));

            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.None.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        private TiaTestProcess StartTiaTestProcess()
        {
            string executable = "ProfilerTestee32.exe";
            if (this.bitness == Bitness.x64)
            {
                executable = "ProfilerTestee64.exe";
            }

            ProfilerTestProcess testProcess = StartProfiler(executable, arguments: "interactive", lightMode: true, bitness: bitness, environment: CreateTiaEnvironment());
            Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo("interactive"));
            Assert.That(testProcess.Process.HasExited, Is.False);
            return new TiaTestProcess(testProcess, () => profilerIpc);
        }

        private class TiaTestProcess
        {
            private readonly ProfilerTestProcess testProcess;
            private readonly Func<RecordingProfilerIpc> profilerIpc;

            internal TiaTestProcess(ProfilerTestProcess testProcess, Func<RecordingProfilerIpc> profilerIpc)
            {
                this.testProcess = testProcess;
                this.profilerIpc = profilerIpc;
            }

            internal TiaTestResult Stop(bool assertReceivedRequests = true)
            {
                testProcess.Process.StandardInput.WriteLine();
                testProcess.Process.WaitForExit();
                Assert.That(testProcess.Process.ExitCode, Is.Zero);
                FileInfo actualTrace = AssertSingleTrace(testProcess.GetTraceFiles());
                TiaTestResult testResult = new TiaTestResult(actualTrace);
                Assert.That(testResult.TraceLines, Has.One.EqualTo($"Info=TIA enabled. SUB: {profilerIpc().Config.PublishSocket} REQ: {profilerIpc().Config.RequestSocket}"));
                Assert.That(testResult.TraceLines, Has.One.StartsWith("Stopped="));
                if (assertReceivedRequests)
                {
                    Assert.That(profilerIpc().ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
                }
                return testResult;
            }

            internal TiaTestProcess Input(string testName)
            {
                testProcess.Process.StandardInput.WriteLine(testName);
                Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo(testName));
                return this;
            }

            internal TiaTestProcess RunTestCase(params string[] testNames)
            {
                foreach (string testName in testNames)
                {
                    profilerIpc().StartTest(testName);
                    Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly, so the profiler registers the change
                    this.Input(testName);
                    Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly, so the profiler registers the change
                    profilerIpc().EndTest(ETestExecutionResult.PASSED);
                }

                return this;
            }
        }

        private class TiaTestResult
        {
            public IEnumerable<string> TraceLines { get; }
            public List<TestCase> TestCases { get; }

            public IEnumerable<string> TestCaseNames => TestCases.Select(t => t.Name);

            public List<TestCase> this[string testName]
            {
                get => GetTestCases(testName);
            }

            public TiaTestResult(FileInfo tracefile)
            {
                this.TraceLines = File.ReadAllLines(tracefile.FullName);
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

        private class TestCase
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

        private Dictionary<string, List<string>> GroupEventsByTest(string[] lines)
        {
            var eventsByTest = new Dictionary<string, List<string>>();
            string currentTest = string.Empty;
            foreach (var line in lines)
            {
                if (line.StartsWith("Test="))
                {
                    currentTest = line.Substring("Test=".Length);
                }

                if (!eventsByTest.ContainsKey(currentTest))
                {
                    eventsByTest[currentTest] = new List<string>();
                }

                eventsByTest[currentTest].Add(line);
            }

            return eventsByTest;
        }

        private IDictionary<string, string> CreateTiaEnvironment()
        {
            return new Dictionary<string, string>()
            {
                ["COR_PROFILER_TIA"] = "true",
                ["COR_PROFILER_TIA_SUBSCRIBE_SOCKET"] = profilerIpc.Config.PublishSocket, // PUB-SUB
                ["COR_PROFILER_TIA_REQUEST_SOCKET"] = profilerIpc.Config.RequestSocket, // REQ-REP
            };
        }
    }
}

using Cqse.Teamscale.Profiler.Commons.Ipc;
using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using static Cqse.Teamscale.Profiler.Dotnet.Proxies.TiaProfiler;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    /// <summary>
    /// Test case for coverage profiler.
    /// </summary>
    [TestFixture(Bitness.x64, IpcImplementation.NetMQ)]
    [TestFixture(Bitness.x86, IpcImplementation.NetMQ)]

    public class TiaProfilerTest : TiaProfilerTestBase
    {
        private Testee testee;
        private Bitness bitness;

        public TiaProfilerTest(Bitness bitness, IpcImplementation ipcImplementation) : base(ipcImplementation)
        {
            this.bitness = bitness;
        }

        [SetUp]
        public void SetupProfilerAndTestee()
        {
            string executable = "ProfilerTestee32.exe";
            if (bitness == Bitness.x64)
            {
                executable = "ProfilerTestee64.exe";
            }
            testee = new Testee(GetTestProgram(executable), bitness);
        }

        [Test]
        public void ConnectsToAndDisconnectsFromIpc()
        {
            Stop(Start(testee, profilerUnderTest));

            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_disconnected", "testname" }));
        }

        [Test]
        public void TestCaseDefinedBeforeStartup()
        {
            profilerIpc.StartTest("startup");

            Stop(Start(testee, profilerUnderTest));

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "startup" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.None.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [TestCase(null)]
        [TestCase("")]
        public void TestCaseMayNotBeNullOrEmpty(string testName)
        {
            Assert.Throws<ArgumentException>(() => profilerIpc.StartTest(testName));
        }

        [Test]
        public void TestCaseWithSpecialCharacters()
        {
            profilerIpc.StartTest(@"Test:Case\\:\:");

            Stop(Start(testee, profilerUnderTest));

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, @"Test:Case\\:\:" }));
        }

        [Test]
        public void TestCaseDefinedAfterStartup()
        {
            TesteeProcess testeeProcess = Start(testee, profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            Stop(testeeProcess);

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [Test]
        public void ThreeTestCasesWithDifferentAction()
        {
            TesteeProcess testeeProcess = Start(testee, profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("B", testeeProcess, profilerIpc);
            RunTestCase("C", testeeProcess, profilerIpc);
            Stop(testeeProcess);

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A", "B", "C" }));
            Assert.That(testResult["A"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["B"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["C"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
        }

        [Test]
        public void ThreeTestCasesWithSameAction()
        {
            TesteeProcess testeeProcess = Start(testee, profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("A", testeeProcess, profilerIpc);
            Stop(testeeProcess);

            TiaTestResult testResult = profilerUnderTest.Result;
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

            Stop(Start(testee, profilerUnderTest));

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(profilerIpc.ReceivedRequests, Is.Empty);

            profilerIpc = null;
        }

        [Test]
        public void IpcStartedAfterStartup()
        {
            RecordingProfilerIpc oldProfilerIpc = profilerIpc;
            oldProfilerIpc.StartTest("should not be triggered");
            oldProfilerIpc.EndTest(TestExecutionResult.Passed);
            profilerIpc.Dispose();
            TesteeProcess testeeProcess = Start(testee, profilerUnderTest);

            profilerIpc = CreateProfilerIpc(profilerIpc.Config);
            // Wait until the IPC server has started up. Not ideal but it fixes the test case.
            Thread.Sleep(500);
            RunTestCase("A", testeeProcess, profilerIpc);
            Stop(testeeProcess);

            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_disconnected", "testname" }));
            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        private static TesteeProcess Start(Testee testee, IProfiler profiler)
        {
            TesteeProcess process = testee.Start(arguments: "interactive", profiler);
            Assert.That(process.Output.ReadLine(), Is.EqualTo("interactive"));
            Assert.That(process.HasExited, Is.False);
            return process;
        }

        private static void RunTestCase(string testCaseName, TesteeProcess process, RecordingProfilerIpc profilerIpc)
        {
            profilerIpc.StartTest(testCaseName);
            Thread.Sleep(TimeSpan.FromMilliseconds(20)); // wait shortly, so the profiler registers the change

            process.Input.WriteLine(testCaseName);
            Assert.That(process.Output.ReadLine(), Is.EqualTo(testCaseName));
            Thread.Sleep(TimeSpan.FromMilliseconds(20)); // wait shortly

            profilerIpc.EndTest(TestExecutionResult.Passed);
            Thread.Sleep(TimeSpan.FromMilliseconds(20)); // wait shortly
        }

        private static void Stop(TesteeProcess process)
        {
            // process terminates on "empty" input
            process.Input.WriteLine();
            process.WaitForExit();
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
    }
}

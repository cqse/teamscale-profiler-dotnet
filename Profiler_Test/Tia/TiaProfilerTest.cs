using Cqse.Teamscale.Profiler.Commons.Ipc;
using Cqse.Teamscale.Profiler.Dotnet.Targets;
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
    [TestFixture(Bitness.x64, IpcImplementation.Native)]
    [TestFixture(Bitness.x86, IpcImplementation.Native)]
    public class TiaProfilerTest : TiaProfilerTestBase
    {
        private TiaTestee testee;
        private Bitness bitness;

        public TiaProfilerTest(Bitness bitness, IpcImplementation ipcImplementation) : base(ipcImplementation)
        {
            this.bitness = bitness;
        }

        [SetUp]
        public void SetupProfilerAndTestee()
        {
            profilerUnderTest.Bitness = bitness;
            testee = new TiaTestee(TestProgramsDirectory, bitness);
        }

        [Test]
        public void ConnectsToAndDisconnectsFromIpc()
        {
            testee.Start(profiler: profilerUnderTest).Stop();

            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
        }

        [Test]
        public void TestCaseDefinedBeforeStartup()
        {
            profilerIpc.StartTest("startup");

            testee.Start(profiler: profilerUnderTest).Stop();

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "startup" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.None.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [Test]
        public void TestCaseWithSpecialCharacters()
        {
            profilerIpc.StartTest(@"Test:Case\\:\:");

            testee.Start(profiler: profilerUnderTest).Stop();

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, @"Test:Case\\:\:" }));
        }

        [Test]
        public void TestCaseDefinedAfterStartup()
        {
            TiaTesteeProcess testeeProcess = testee.Start(profiler: profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            testeeProcess.Stop();

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        [Test]
        public void ThreeTestCasesWithDifferentAction()
        {
            TiaTesteeProcess testeeProcess = testee.Start(profiler: profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("B", testeeProcess, profilerIpc);
            RunTestCase("C", testeeProcess, profilerIpc);
            testeeProcess.Stop();

            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A", "B", "C" }));
            Assert.That(testResult["A"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["B"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(testResult["C"][0].TraceLines, Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
        }

        [Test]
        public void ThreeTestCasesWithSameAction()
        {
            TiaTesteeProcess testeeProcess = testee.Start(profiler: profilerUnderTest);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("A", testeeProcess, profilerIpc);
            RunTestCase("A", testeeProcess, profilerIpc);
            testeeProcess.Stop();

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

            testee.Start(profiler: profilerUnderTest).Stop();

            TiaTestResult testResult = profilerUnderTest.Result;
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

            TiaTesteeProcess testeeProcess = testee.Start(profiler: profilerUnderTest);

            profilerIpc = CreateProfilerIpc(oldProfilerIpc.Config);
            RunTestCase("A", testeeProcess, profilerIpc);
            testeeProcess.Stop();

            Assert.That(oldProfilerIpc.ReceivedRequests, Is.Empty);
            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_disconnected" }));
            TiaTestResult testResult = profilerUnderTest.Result;
            Assert.That(testResult.TestCaseNames, Is.EquivalentTo(new[] { string.Empty, "A" }));
            Assert.That(testResult.TestCases[0].TraceLines, Has.None.Matches("^(Inlines|Jitted|Called)"));
            Assert.That(testResult.TestCases[1].TraceLines, Has.Some.Matches("^(Inlines|Jitted|Called)"));
        }

        private static void RunTestCase(string testCaseName, TiaTesteeProcess process, RecordingProfilerIpc profilerIpc)
        {
            profilerIpc.StartTest(testCaseName);
            Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly, so the profiler registers the change
            process.RunTestCase(testCaseName);
            Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly
            profilerIpc.EndTest(ETestExecutionResult.PASSED);
            Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly
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

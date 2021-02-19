using NUnit.Framework;
using Profiler_Test.Tia;
using System.Collections.Generic;

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
        public TiaProfilerTest(Bitness bitness, IpcImplementation ipcImplementation) : base(bitness, ipcImplementation) { }

        protected override string ExecutableName
        {
            get
            {
                string executable = "ProfilerTestee32.exe";
                if (this.bitness == Bitness.x64)
                {
                    executable = "ProfilerTestee64.exe";
                }
                return executable;
            }
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

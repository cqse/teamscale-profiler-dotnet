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
    [TestFixture]
    public class TiaProfilerTest : ProfilerTestBase
    {
        private RecordingProfilerIpc profilerIpc;

        [SetUp]
        public void StartZmq()
        {
            profilerIpc = CreateProfilerIpc();
        }

        protected virtual RecordingProfilerIpc CreateProfilerIpc()
        {
            return new RecordingProfilerIpc();
        }

        [TearDown]
        public void StopZmq()
        {
            profilerIpc.Dispose();
        }

        /// <summary>
        /// Runs the profiler with command line argument and asserts its content is logged into the trace.
        /// </summary>
        [Test]
        public void TestThreeMethods()
        {
            profilerIpc.TestName = "startup";

            ProfilerTestProcess testProcess = StartProfiler("ProfilerTestee.exe", arguments: "interactive", lightMode: true, bitness: Bitness.x86, environment: CreateTiaEnvironment());
            Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo("interactive"));
            Assert.That(testProcess.Process.HasExited, Is.False);
            foreach (string testName in new[] { "A", "B", "C" })
            {
                profilerIpc.TestName = testName;
                Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly, so the profiler registers the change
                testProcess.Process.StandardInput.WriteLine(testName);
                Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo(testName));
            }

            testProcess.Process.StandardInput.WriteLine();
            testProcess.Process.WaitForExit();
            FileInfo actualTrace = AssertSingleTrace(testProcess.GetTraceFiles());
            string[] lines = File.ReadAllLines(actualTrace.FullName);
            Assert.That(lines, Has.One.EqualTo($"Info=TIA enabled. SUB: {profilerIpc.Config.PublishSocket} REQ: {profilerIpc.Config.RequestSocket}"));
            Assert.That(lines, Has.One.EqualTo("Test=startup"));
            Assert.That(lines, Has.One.EqualTo("Test=A"));
            Assert.That(lines, Has.One.EqualTo("Test=B"));
            Assert.That(lines, Has.One.EqualTo("Test=C"));
            Assert.That(lines, Has.One.StartsWith("Stopped="));
            Dictionary<string, List<string>> eventsByTest = GroupEventsByTest(lines);
            Assert.That(eventsByTest["A"], Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(eventsByTest["B"], Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(eventsByTest["C"], Has.One.StartsWith("Jitted=2").And.One.StartsWith("Called=2"));
            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
        }

        [Test]
        public void TestSameMethodThreeTimes()
        {
            profilerIpc.TestName = "startup";

            ProfilerTestProcess testProcess = StartProfiler("ProfilerTestee.exe", arguments: "interactive", lightMode: true, bitness: Bitness.x86, environment: CreateTiaEnvironment());
            Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo("interactive"));
            Assert.That(testProcess.Process.HasExited, Is.False);
            foreach (string testName in new[] { "A", "A", "A" })
            {
                profilerIpc.TestName = testName;
                Thread.Sleep(TimeSpan.FromMilliseconds(10)); // wait shortly, so the profiler registers the change
                testProcess.Process.StandardInput.WriteLine(testName);
                Assert.That(testProcess.Process.StandardOutput.ReadLine(), Is.EqualTo(testName));
            }

            testProcess.Process.StandardInput.WriteLine();
            testProcess.Process.WaitForExit();
            FileInfo actualTrace = AssertSingleTrace(testProcess.GetTraceFiles());
            string[] lines = File.ReadAllLines(actualTrace.FullName);
            Assert.That(lines, Has.One.EqualTo($"Info=TIA enabled. SUB: {profilerIpc.Config.PublishSocket} REQ: {profilerIpc.Config.RequestSocket}"));
            Assert.That(lines, Has.One.EqualTo("Test=startup"));
            Assert.That(lines, Has.Exactly(3).EqualTo("Test=A"));
            Dictionary<string, List<string>> eventsByTest = GroupEventsByTest(lines);
            Assert.That(eventsByTest["A"], Has.Exactly(3).StartsWith("Called=2"));
            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
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

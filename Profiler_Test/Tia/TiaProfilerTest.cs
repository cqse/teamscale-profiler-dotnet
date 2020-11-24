using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

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
        public void TestRequestTestNameOnStart()
        {
            profilerIpc.TestName = "sample test";

            FileInfo actualTrace = AssertSingleTrace(RunProfiler("ProfilerTestee.exe", arguments: "all", lightMode: true, bitness: Bitness.x86, environment: CreateTiaEnvironment()));
            string[] lines = File.ReadAllLines(actualTrace.FullName);
            Assert.That(lines, Has.One.StartsWith($"Info=TIA enabled. SUB: {profilerIpc.Config.PublishSocket} REQ: {profilerIpc.Config.RequestSocket}"));
            Assert.That(lines, Has.One.StartsWith("Stopped="));
            Assert.That(lines, Has.One.StartsWith("Test=sample test"));
            Assert.That(profilerIpc.ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
        }

        private IDictionary<string, string> CreateTiaEnvironment()
        {
            return new Dictionary<string, string>()
            {
                ["COR_PROFILER_TIA"] = "true",
                ["COR_PROFILER_TIA_SUBSCRIBE_SOCKET"] = profilerIpc.Config.PublishSocket, // PUB-SUB
                ["COR_PROFILER_TIA_REQUEST_SOCKET"] = profilerIpc.Config.RequestSocket, // REQ-REP
            }; ;
        }
    }
}

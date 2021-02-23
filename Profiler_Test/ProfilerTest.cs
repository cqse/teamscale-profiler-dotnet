using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Dotnet
{
    /// <summary>
    /// Test case for coverage profiler.
    /// </summary>
    [TestFixture]
    public class ProfilerTest : ProfilerTestBase
    {
        private Proxies.Profiler profiler;

        [SetUp]
        public void CreateProfiler()
        {
            profiler = new Proxies.Profiler(basePath: SolutionRoot, targetDir: TestTraceDirectory);
        }

        /// <summary>
        /// Runs the profiler with command line argument and asserts its content is logged into the trace.
        /// </summary>
        [Test]
        public void TestCommandLine()
        {
            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "all", profiler);

            string[] lines = profiler.GetSingleTrace();
            Assert.That(lines.Any(line => line.StartsWith("Info=Command Line: ") && line.EndsWith(" all")));
        }

        /// <summary>
        /// Makes sure that processes not matching the given process name are not profiled.
        /// </summary>
        [TestCase("w3wp.exe", ExpectedResult = 0)]
        [TestCase("ProfilerTestee.exe", ExpectedResult = 1)]
        [TestCase("profilerTesTEE.EXE", ExpectedResult = 1)]
        public int TestProcessSelection(string process)
        {
            profiler.TargetProcessName = process;

            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "none", profiler);

            return profiler.GetTraceFiles().Count;
        }

        /// <summary>
        /// Makes sure that processes not matching the given process name are not profiled.
        /// </summary>
        [TestCase(".*w3wp.exe", ExpectedResult = 0)]
        [TestCase(".*ProfilerTestee.exe", ExpectedResult = 1)]
        public int TestConfigFile(string regex)
        {
            var configFile = Path.Combine(TestTempDirectory, "profilerconfig.yml");
            File.WriteAllText(configFile, $@"
match:
  - profiler:
      enabled: false
  - executablePathRegex: {regex}
    profiler:
      enabled: true
");

            profiler.ConfigFilePath = configFile;
            
            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "none", profiler);

            return profiler.GetTraceFiles().Count;
        }

        /// <summary>
        /// Runs the profiler with the environment variable APP_POOL_ID set and asserts its content is logged into the trace.
        /// </summary>
        [Test]
        public void TestWithAppPool()
        {
            profiler.AppPoolId = "MyAppPool";

            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "none", profiler);

            string[] lines = profiler.GetSingleTrace();
            Assert.That(lines.Any(line => line.Equals("Info=IIS AppPool: MyAppPool")));
        }

        /// <summary>
        /// Runs the profiler without the environment variable APP_POOL_ID set and asserts the trace does not contain a line for the app pool.
        /// </summary>
        [Test]
        public void TestWithoutAppPool()
        {
            profiler.AppPoolId = null;

            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "none", profiler);

            string[] lines = profiler.GetSingleTrace();
            Assert.That(!lines.Any(line => line.StartsWith("Info=IIS AppPool:")));
        }

        /// <summary>
        /// Executes a test for the given profiler with the given application mode.
        /// </summary>
        [Test, Pairwise]
        public void ProfilerModeTest(
            [Values("none", "all")] string applicationMode,
            [Values(true, false)] bool isLightMode)
        {
            profiler.LightMode = isLightMode;

            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: applicationMode, profiler);

            AssertNormalizedTraceFileEqualsReference(profiler.GetSingleTraceFile(), new[] { 2 });
        }

        /// <summary>
        /// Tests that the profiler traces the assemblies of the given set and matches the reference profiling output.
        /// </summary>
        /// <param name="application">Path to the application to profile.</param>
        /// <param name="expectedAssemblyIds">Assembly IDs of the relevant assemblies to use for test</param>
        [TestCase("PdfizerConsole.exe", new int[] { 2, 3, 9 })]
        [TestCase("GeneratedTest.exe", new int[] { 2, 3, 4 })]
        public void TestProfiling(string application, int[] expectedAssemblyIds)
        {
            new Testee(GetTestProgram(application)).Run(profiler: profiler);

            AssertNormalizedTraceFileEqualsReference(profiler.GetSingleTraceFile(), expectedAssemblyIds);
        }

        [Test]
        public void TestAttachLog()
        {
            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "all", profiler: profiler);

            string[] lines = profiler.GetAttachLog();
            string firstLine = lines[0];
            Assert.That(firstLine.StartsWith("Attach"));
            Assert.That(firstLine.Contains("ProfilerTestee.exe"));
        }

        [Test]
        public void TestDetatchLog()
        {
            new Testee(GetTestProgram("ProfilerTestee.exe")).Run(arguments: "all", profiler: profiler);

            string[] lines = profiler.GetAttachLog();
            string secondLine = lines[1];
            Assert.That(secondLine.StartsWith("Detach"));
            Assert.That(secondLine.Contains("ProfilerTestee.exe"));
        }
    }
}

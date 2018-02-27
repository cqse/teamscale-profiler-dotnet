using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text;

namespace Cqse.Teamscale.Profiler.Dotnet
{
	/// <summary>
	/// Test case for coverage profiler.
	/// </summary>
	[TestFixture]
	public class ProfilerTest : ProfilerTestBase
	{
		/// <summary>
		/// Makes sure that processes not matching the given process name are not profiled.
		/// </summary>
		[TestCase("w3wp.exe", ExpectedResult = 0)]
		[TestCase("ProfilerTestee.exe", ExpectedResult = 1)]
		[TestCase("profilerTesTEE.EXE", ExpectedResult = 1)]
		public int TestProcessSelection(string process)
		{
			var environment = new Dictionary<string, string> { { "COR_PROFILER_PROCESS", process } };
			return RunProfiler("ProfilerTestee.exe", arguments: "none", lightMode: true, bitness: Bitness.x86, environment: environment).Count;
		}

		/// <summary>
		/// Runs the profiler with the environment variable APP_POOL_ID set and asserts its content is logged into the trace.
		/// </summary>
		[Test]
		public void TestWithAppPool()
		{
			var environment = new Dictionary<string, string> { { "APP_POOL_ID", "MyAppPool" } };
			FileInfo actualTrace = AssertSingleTrace(RunProfiler("ProfilerTestee.exe", arguments: "none", lightMode: true, environment: environment, bitness: Bitness.x86));
			string[] lines = File.ReadAllLines(actualTrace.FullName);
			Assert.That(lines.Any(line => line.Equals("Info=IIS AppPool: MyAppPool")));
		}

		/// <summary>
		/// Runs the profiler without the environment variable APP_POOL_ID set and asserts the trace does not contain a line for the app pool.
		/// </summary>
		[Test]
		public void TestWihoutAppPool()
		{
			FileInfo actualTrace = AssertSingleTrace(RunProfiler("ProfilerTestee.exe", arguments: "none", lightMode: true, bitness: Bitness.x86));
			string[] lines = File.ReadAllLines(actualTrace.FullName);
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
			List<FileInfo> traces = RunProfiler("ProfilerTestee.exe", arguments: applicationMode, lightMode: isLightMode, bitness: Bitness.x86);
			AssertNormalizedTraceFileEqualsReference(traces, new[] { 2 });
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
			List<FileInfo> traces = RunProfiler(application);
			AssertNormalizedTraceFileEqualsReference(traces, expectedAssemblyIds);
		}
	}
}

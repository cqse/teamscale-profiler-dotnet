using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		[TestCase("w3wp.exe", 0)]
		[TestCase("ProfilerTestee.exe", 1)]
		[TestCase("profilerTesTEE.EXE", 1)]
		public void TestProcessSelection(string process, int expectedTraces)
		{
			List<FileInfo> traces = RunProfiler("ProfilerTestee.exe", arguments: "none", lightMode: true, bitness: Bitness.x68, environment: new Dictionary<string, string> { { "COR_PROFILER_PROCESS", process } });
			Assert.That(traces, Has.Count.EqualTo(expectedTraces), "Got the wrong number of traces.");
		}

		/// <summary>
		/// Executes a test for the given profiler with the given application mode.
		/// </summary>
		[Test, Pairwise]
		public void ProfilerModeTest(
			[Values("none", "all")] string applicationMode,
			[Values(true, false)] bool isLightMode)
		{
			List<FileInfo> traces = RunProfiler("ProfilerTestee.exe", arguments: applicationMode, lightMode: isLightMode, bitness: Bitness.x68);
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

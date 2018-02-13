using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Cqse.Teamscale.Profiler.Dotnet
{

	/**
	 * Regression test for the .NET coverage profiler. Since the profiler is written
	 * in C++ and binds to Windows APIs, this test is a no-op on any OS other than
	 * Windows.
	 * <p>
	 * Executes a test application with the profiler attached and compares the trace
	 * file with a reference.
	 * <p>
	 * Location of the data used by this test:
	 * <ul>
	 * <li>The profiler binaries are located in the resources folder.
	 * <li>The reference output is located in the test-data folder.
	 * </ul>
	 */
	[TestFixture]
	public class DotNetProfilerRegressionTest : ProfilerTestBase
	{

		/**
		 * The name of the trace file that is created in the {@link #traceDirectory} of
		 * the analysis input data.
		 */
		private const string TRACE_FILE_NAME = "trace.txt";

		/**
		 * The temporary directory that contains the input for the Test Gap dashboard.
		 */
		private DirectoryInfo dashboardInputDirectory;

		/** The binary that is being profiled. */
		private FileInfo binaryFile;

		/** The temporary directory into which all output is written. */
		private DirectoryInfo workingDirectory;

		/** {@inheritDoc} */
		[SetUp]
		public void setUp()
		{

			dashboardInputDirectory = new DirectoryInfo(getAbsoluteResourcePath("dashboard-input"));
			workingDirectory = new DirectoryInfo(Path.Combine(TestContext.CurrentContext.WorkDirectory, "output"));
			binaryFile = new FileInfo(Path.Combine(dashboardInputDirectory.FullName, "v1/Binaries/ProfilerTestee.exe"));
			workingDirectory.Create();
		}

		/**
		 * Tests the normal .NET 4 profiler by executing the test application in "all"
		 * mode.
		 */
		[TestCase]
		public void testDotNet4FullAll()
		{
			runTest("all", false);
		}

		/**
		 * Tests the normal .NET 4 profiler by executing the test application in "none"
		 * mode.
		 */
		[TestCase]
		public void testDotNet4FullNone()
		{
			runTest("none", false);
		}

		/**
		 * Tests the light .NET 4 profiler by executing the test application in "all"
		 * mode.
		 */
		[TestCase]
		public void testDotNet4LightAll()
		{
			runTest("all", true);
		}

		/**
		 * Tests the light .NET 4 profiler by executing the test application in "none"
		 * mode.
		 */
		[TestCase]
		public void testDotNet40LightNone()
		{
			runTest("none", true);
		}

		/**
		 * Executes a test for the given profiler with the given application mode.
		 */
		public void runTest(string applicationMode, bool isLightMode)
		{
			runProfiler(applicationMode, isLightMode);

			string profilerName = "profiler-dotnet-newer";
			if (isLightMode)
			{
				profilerName = "profiler-dotnet-newer-light";
			}

			FileInfo referenceTraceFileInfo = new FileInfo(getAbsoluteResourcePath("reference-" + profilerName + "-" + applicationMode + ".txt"));
			assertNormalizedTraceFileEqualsReference(referenceTraceFileInfo, workingDirectory, new HashSet<string>() { "2" });
		}

		/** Executes the test application with the profiler attached. */
		private void runProfiler(string applicationCommandLineArgument, bool isLightMode)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(binaryFile.FullName, applicationCommandLineArgument);

			registerProfiler(startInfo, workingDirectory, isLightMode, "32");

			startInfo.WorkingDirectory = workingDirectory.FullName;

			Process.Start(startInfo).WaitForExit();
		}
	}

}

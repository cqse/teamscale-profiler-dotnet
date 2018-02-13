using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Cqse.Teamscale.Profiler.Dotnet
{
	/**
	 * Test case for coverage profiler. This will only execute and assert on Windows
	 * systems.
	 */
	[TestFixture]
	public class ProfilerRunnerTest : ProfilerTestBase
	{

		/** The name of the directory in which the test programs are located. */
		private const string TEST_PROGRAMS_DIR = "test-programs";

		/** The trace assembly keys of PDFizer's application assemblies. */
		private static readonly HashSet<string> PDFIZER_ASSEMBLIES = new HashSet<string> { "2", "3", "9" };

		/**
		 * The trace assembly keys of the GeneratedTest's application assemblies.
		 */
		private static readonly HashSet<string> GEN_TEST_ASSEMBLIES = new HashSet<string> { "2", "3", "4" };

		/** Test the PdfizerConsole application. */
		[TestCase]
		public void testPdfizer()
		{
			runTest("PdfizerConsole.exe", PDFIZER_ASSEMBLIES);
		}

		/** Test the generated application. */
		[TestCase]
		public void testGenerated()
		{
			runTest("GeneratedTest.exe", GEN_TEST_ASSEMBLIES);
		}

		/**
		 * Executes a test.
		 * 
		 * @param application
		 *            Path to the application to profile.
		 * @param assembliesToCompare
		 *            Assembly IDs of the relevant assemblies to use for test
		 *            assertion.
		 */
		public void runTest(string application, HashSet<string> assembliesToCompare)
		{
			DirectoryInfo workingDir = new DirectoryInfo(TestContext.CurrentContext.WorkDirectory);
			runProfiler(application, workingDir);

			assertNormalizedTraceFileEqualsReference(new FileInfo(getAbsoluteExePath(application + "_reference.txt")),
					workingDir, assembliesToCompare);
		}

		/** Executes the test application with the profiler attached. */
		private void runProfiler(string application, DirectoryInfo workingDir)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo(getAbsoluteExePath(application));
			registerProfiler(startInfo, workingDir, false, getBitness());
			startInfo.WorkingDirectory = getAbsoluteResourcePath(TEST_PROGRAMS_DIR);
			Process result = Process.Start(startInfo);
			result.WaitForExit();
			Assert.That(result.StandardOutput.ReadToEnd().Contains("SUCCESS"), "Program " + application + " did not execute properly.");
		}

		/**
		 * Returns the absolute path to the given file in the
		 * {@link #TEST_PROGRAMS_DIR}.
		 */
		private string getAbsoluteExePath(string filename)
		{
			return getAbsoluteResourcePath(TEST_PROGRAMS_DIR + Path.DirectorySeparatorChar + filename);
		}
	}

}

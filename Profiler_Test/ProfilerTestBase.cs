using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Cqse.Teamscale.Profiler.Dotnet
{
	/// <summary>
	/// Base class for testing the .NET Profiler.
	/// </summary>
	public abstract class ProfilerTestBase
	{
		/** Environment variable name to enable the profiler. */
		private const string PROFILER_ENABLE_KEY = "COR_ENABLE_PROFILING";

		/** Environment variable name for the profiler's class ID. */
		private const string PROFILER_CLASS_ID_KEY = "COR_PROFILER";

		/** Environment variable name for the directory to store profiler traces. */
		private const string PROFILER_TARGETDIR_KEY = "COR_PROFILER_TARGETDIR";

		/** Environment variable name for the path to the profiler DLL. */
		private const string PROFILER_PATH_KEY = "COR_PROFILER_PATH";

		/** Environment variable name to enable the profiler's light mode. */
		private const string PROFILER_LIGHT_MODE_KEY = "COR_PROFILER_LIGHT_MODE";

		/** The profiler's class ID. */
		private const string PROFILER_CLASS_ID = "{DD0A1BB6-11CE-11DD-8EE8-3F9E55D89593}";

		/** Label with which jitted methods are prefixed */
		public const string LABEL_JITTED = "Jitted";

		/** Label with which inlined methods are prefixed */
		public const string LABEL_INLINED = "Inlined";

		/** Label with which loaded assemblies are prefixed */
		public const string LABEL_ASSEMBLY = "Assembly";

		/** Separator used to concatenate method keys */
		public const char KEY_SEPARATOR = ':';

		/** Sets all environment variables the profiler needs. */
		protected void registerProfiler(ProcessStartInfo processInfo, DirectoryInfo targetDir, bool useLightMode, string bitness)
		{

			string profilerDll = "profiler-dotnet-newer/Profiler" + bitness + ".dll";

			// set environment variables for the profiler
			processInfo.Environment[PROFILER_PATH_KEY] = getAbsoluteResourcePath(profilerDll);
			processInfo.Environment[PROFILER_TARGETDIR_KEY] = targetDir.FullName;
			processInfo.Environment[PROFILER_CLASS_ID_KEY] = PROFILER_CLASS_ID;
			processInfo.Environment[PROFILER_ENABLE_KEY] = "1";
			if (useLightMode)
			{
				processInfo.Environment[PROFILER_LIGHT_MODE_KEY] = "1";
			}
		}

		/**
		 * Asserts that the trace file written by the profiler has the same contents as
		 * the given reference trace, modulo some normalization.
		 */
		protected void assertNormalizedTraceFileEqualsReference(FileInfo referenceTraceFile, DirectoryInfo directory,
				HashSet<string> assembliesToCompare)
		{
			Assert.AreEqual(getNormalizedTraceContent(referenceTraceFile, assembliesToCompare),
						getNormalizedTraceContent(getTraceFile(directory), assembliesToCompare),
						"The normalized contents of the trace files did not match");
		}

		/**
		 * Returns the trace.
		 *
		 * @throws AssertionError
		 *             if not exactly one trace was written.
		 */
		protected FileInfo getTraceFile(DirectoryInfo directory)
		{
			List<FileInfo> traceFiles = directory.EnumerateFiles().Where(file => file.Name.StartsWith("coverage_")).ToList();
			Assert.That(traceFiles, Has.Count.GreaterThan(0), "No coverage trace was written.");
			Assert.That(traceFiles, Has.Count.LessThanOrEqualTo(1), "More than one coverage trace was written: " + string.Join(", ", traceFiles));

			return traceFiles[0];
		}

		/**
		 * Executes a program from the resource folder.
		 */
		protected static Process executeProgramInResourceFolder(string program)
		{
			var process = Process.Start(getAbsoluteResourcePath(program));
			process.WaitForExit();
			return process;
		}

		/** Gets the absolute path to a resource file. */
		protected static string getAbsoluteResourcePath(string path)
		{
			return Path.GetFullPath(Path.Combine("..", "test-data", path));
		}

		/**
		 * Returns the inlined and jitted methods for the testee assembly. All other
		 * trace file content may vary across machines or versions of the .NET
		 * framework, including the number of actually jitted methods (in mscorlib).
		 */
		private static string getNormalizedTraceContent(FileInfo traceFile, HashSet<string> assembliesToCompare)
		{
			string[] content = File.ReadAllLines(traceFile.FullName);

			ILookup<string, string> traceMap = keyValuesMapFor(content);

			IEnumerable<string> inlined = filterMethodInvocationsByAssemblyNumber(traceMap[LABEL_INLINED], assembliesToCompare);
			IEnumerable<string> jitted = filterMethodInvocationsByAssemblyNumber(traceMap[LABEL_JITTED], assembliesToCompare);

			var invocations = inlined.Union(jitted).ToList();
			invocations.Sort();

			return string.Join("\\n", invocations);
		}

		/**
		 * Filters invoked methods, keeping only those that contain allowed assembly
		 * keys. Each line in the output corresponds to one method invocation.
		 */
		private static IEnumerable<string> filterMethodInvocationsByAssemblyNumber(IEnumerable<string> methodInvocations,
				HashSet<string> assembliesToCompare)
		{
			foreach (string methodInvocation in methodInvocations)
			{
				string assemblyToken = methodInvocation.Split(KEY_SEPARATOR, 2).First();

				if (assembliesToCompare.Contains(assemblyToken))
				{
					yield return methodInvocation;
				}
			}
		}

		/**
		 * Returns "64" if running on an x64 OS and having a .NET framework x64
		 * installed, or "32" in case of an x86 .NET framework.
		 */
		protected static string getBitness()
		{
			return executeProgramInResourceFolder("bitness-checker.exe").StandardOutput.ReadToEnd().Substring(0, 2);
		}


		/**
		 * Parse trace into map from key to list of values.
		 *
		 * @param coverageReport
		 *            The coverage report.
		 */
		public static ILookup<string, string> keyValuesMapFor(string[] coverageReport)
			=> coverageReport.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
				.Select(line => line.Split('=', 2))
				.ToLookup(split => split[0], split => split[0]);
	}
}

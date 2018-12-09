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
        /// <summary>
        /// Enumeration for processor bitness
        /// </summary>
        public enum Bitness
        {
            /// <summary>
            /// x86 architecture.
            /// </summary>
            x86,

            /// <summary>
            /// x64 architecture.
            /// </summary>
            x64
        }

        /** Environment variable name to enable the profiler. */
        private const string PROFILER_ENABLE_KEY = "COR_ENABLE_PROFILING";

        /** Environment variable name to enable the upload daemon. */
        private const string PROFILER_UPLOAD_DAEMON_KEY = "PROFILER_UPLOAD_DAEMON";

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

        /// <summary>
        /// The directory containing profiler solution.
        /// </summary>
        public static DirectoryInfo SolutionRoot => new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../"));

#if DEBUG
        private static readonly string Configuration = "Debug";
#else
        private static readonly string Configuration = "Release";
#endif

        public static readonly string Profiler32Dll = $"{SolutionRoot}/Profiler/bin/{Configuration}/Profiler32.dll";

        public static readonly string Profiler64Dll = $"{SolutionRoot}/Profiler/bin/{Configuration}/Profiler64.dll";

        /// <summary>
        /// Executes the test application with the profiler attached and returns the written traces.
        /// </summary>
        protected List<FileInfo> RunProfiler(string application, string arguments = null, bool lightMode = false, Bitness? bitness = null, IDictionary<string, string> environment = null)
        {
            DirectoryInfo targetDir = new DirectoryInfo(TestTempDirectory).CreateSubdirectory("traces");
            ProcessStartInfo startInfo = new ProcessStartInfo(GetTestDataPath("test-programs", application), arguments)
            {
                WorkingDirectory = GetTestDataPath("test-programs"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            RegisterProfiler(startInfo, targetDir, lightMode, bitness);

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> entry in environment)
                {
                    startInfo.Environment[entry.Key] = entry.Value;
                }
            }

            Process result = Process.Start(startInfo);
            result.StandardOutput.ReadToEnd();
            result.WaitForExit();

            Assert.That(result.ExitCode, Is.EqualTo(0), "Program " + application + " did not execute properly.");
            return GetTraceFiles(targetDir);
        }

        /// <summary>
        /// Sets all environment variables the profiler needs.
        /// </summary>
        protected void RegisterProfiler(ProcessStartInfo processInfo, DirectoryInfo targetDir, bool lightMode = false, Bitness? bitness = null)
        {
            if (bitness == null)
            {
                bitness = GetBitness();
            }

            string profilerDll = Profiler32Dll;
            if (bitness == Bitness.x64)
            {
                profilerDll = Profiler64Dll;
            }

            Assume.That(File.Exists(profilerDll), "Could not find profiler DLL at " + profilerDll);

            // set environment variables for the profiler
            processInfo.Environment[PROFILER_PATH_KEY] = Path.GetFullPath(profilerDll);
            processInfo.Environment[PROFILER_TARGETDIR_KEY] = targetDir.FullName;
            processInfo.Environment[PROFILER_CLASS_ID_KEY] = PROFILER_CLASS_ID;
            processInfo.Environment[PROFILER_ENABLE_KEY] = "1";
            processInfo.Environment[PROFILER_UPLOAD_DAEMON_KEY] = "0";
            if (lightMode)
            {
                processInfo.Environment[PROFILER_LIGHT_MODE_KEY] = "1";
            }
        }

        /// <summary>
        /// Asserts that the provided traces list contains exactly one item and returns that.
        /// </summary>
        protected FileInfo AssertSingleTrace(List<FileInfo> traces)
        {
            Assert.That(traces, Has.Count.GreaterThan(0), "No coverage trace was written.");
            Assert.That(traces, Has.Count.LessThanOrEqualTo(1), "More than one coverage trace was written: " + string.Join(", ", traces));
            return traces[0];
        }

        /// <summary>
        /// Asserts that the trace file written by the profiler has the same contents as the given reference trace, modulo some normalization.
        /// </summary>
        protected void AssertNormalizedTraceFileEqualsReference(List<FileInfo> traces, int[] assembliesToCompare)
        {
            FileInfo actual = AssertSingleTrace(traces);
            FileInfo referenceTraceFile = new FileInfo(GetTestDataPath("reference-traces", GetSanitizedTestName() + ".txt"));

            var assmeblyIds = new HashSet<int>(assembliesToCompare);
            Assert.AreEqual(ReadNormalizedTraceContent(referenceTraceFile, assmeblyIds),
                        ReadNormalizedTraceContent(actual, assmeblyIds),
                        "The normalized contents of the trace files did not match");
        }

        /// <summary>
        /// Returns the single trace file in the output directory.
        /// </summary>
        private List<FileInfo> GetTraceFiles(DirectoryInfo directory)
            => directory.EnumerateFiles().Where(file => file.Name.StartsWith("coverage_")).ToList();

        /// <summary>
        /// Returns the absolute path to a test data file.
        /// </summary>
        protected static string GetTestDataPath(params string[] path)
            => Path.Combine(SolutionRoot.FullName, "test-data", Path.Combine(path));

        /// <summary>
        /// Returns a test-specific directory for temp files
        /// </summary>
        protected static string TestTempDirectory =>
            Path.Combine(SolutionRoot.FullName, "test-tmp", GetSanitizedTestName());

        /// <summary>
        /// Creates a unique (and empty) temporary test directory for storing output.
        /// </summary>
        [SetUp]
        protected void CreateTemporaryTestDir()
        {
            var testDir = new DirectoryInfo(TestTempDirectory);
            if (testDir.Exists)
            {
                testDir.Delete(true);
            }

            testDir.Create();
        }

        /// <summary>
        /// Returns a sanatized name for the test case that is valid for paths.
        /// </summary>
        /// <returns></returns>
        private static string GetSanitizedTestName()
        {
            var testDirName = TestContext.CurrentContext.Test.FullName;
            char[] invalidChars = Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).Distinct().ToArray();
            return string.Join("", testDirName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Returns the inlined and jitted methods for the testee assembly. All other
        /// trace file content may vary across machines or versions of the.NET
        /// framework, including the number of actually jitted methods(in mscorlib).
        /// </summary>
        private static string ReadNormalizedTraceContent(FileInfo traceFile, HashSet<int> assembliesToCompare)
        {
            string[] content = File.ReadAllLines(traceFile.FullName);

            ILookup<string, string> traceMap = KeyValuesMapFor(content);

            IEnumerable<string> inlined = FilterMethodInvocationsByAssemblyNumber(traceMap[LABEL_INLINED], assembliesToCompare);
            IEnumerable<string> jitted = FilterMethodInvocationsByAssemblyNumber(traceMap[LABEL_JITTED], assembliesToCompare);

            var invocations = inlined.Union(jitted).ToList();
            invocations.Sort();

            return string.Join("\\n", invocations);
        }

        /// <summary>
        /// Filters invoked methods, keeping only those that contain allowed assembly keys. Each line in the output corresponds to one method invocation.
        /// </summary>
        private static IEnumerable<string> FilterMethodInvocationsByAssemblyNumber(IEnumerable<string> methodInvocations,
                HashSet<int> assembliesToCompare)
        {
            foreach (string methodInvocation in methodInvocations)
            {
                string assemblyToken = methodInvocation.Split(new char[] { KEY_SEPARATOR }, 2).First();

                if (int.TryParse(assemblyToken, out int assemblyId))
                {
                    if (assembliesToCompare.Contains(int.Parse(assemblyToken)))
                    {
                        yield return methodInvocation;
                    }
                }
            }
        }

        /// <summary>
        /// Returns "64" if running on an x64 OS and having a .NET framework x64 installed, or "32" in case of an x86.NET framework.
        /// </summary>
        private static Bitness GetBitness()
        {
            // TODO (MP) we could examine this now w/o staring an external process
            var startInfo = new ProcessStartInfo(GetTestDataPath("test-programs/bitness-checker.exe"));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var bitness = process.StandardOutput.ReadToEnd().Substring(0, 2);
            switch (bitness)
            {
                case "32":
                    return Bitness.x86;

                case "64":
                    return Bitness.x64;

                default:
                    throw new Exception("Unknown bitness: " + bitness);
            }
        }

        /// <summary>
        /// Parse trace into map from key to list of values.
        /// </summary>
        private static ILookup<string, string> KeyValuesMapFor(string[] coverageReport)
            => coverageReport.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                .Select(line => line.Split(new char[] { '=' }, 2))
                .ToLookup(split => split[0], split => split[0]);
    }
}
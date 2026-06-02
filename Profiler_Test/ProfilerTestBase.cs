using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Dotnet
{
    /// <summary>
    /// Base class for testing the .NET Profiler.
    /// </summary>
    public abstract class ProfilerTestBase
    {
        /// <summary>
        /// Label with which jitted methods are prefixed
        /// </summary>
        public const string LABEL_JITTED = "Jitted";

        /// <summary>
        /// Label with which inlined methods are prefixed
        /// </summary>
        public const string LABEL_INLINED = "Inlined";

        /// <summary>
        /// Label with which loaded assemblies are prefixed
        /// </summary>
        public const string LABEL_ASSEMBLY = "Assembly";

        /// <summary>
        /// Separator used to concatenate method keys
        /// </summary>
        public const char KEY_SEPARATOR = ':';

        private List<Process> startedProcesses = new List<Process>();

        /// <summary>
        /// The directory containing profiler solution.
        /// </summary>
        public static DirectoryInfo SolutionRoot => new DirectoryInfo(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../"));

#if DEBUG

        /// <summary>
        /// Field holding the build configuration, either 'Release' or 'Debug'
        /// </summary>
        protected const string Configuration = "Debug";

#else
        /// <summary>
        /// Field holding the build configuration, either 'Release' or 'Debug'
        /// </summary>
        protected const string Configuration = "Release";
#endif

        /// <summary>
        /// The directory containing the profiler DLLs
        /// </summary>
        protected static readonly string ProfilerDirectory = $"{SolutionRoot}/Profiler/bin/{Configuration}";

        [TearDown]
        public void TearDown()
        {
            startedProcesses.Where(process => !process.HasExited).ToList().ForEach(process => process.Kill());
        }

        /// <summary>
        /// Asserts that the trace file written by the profiler has the same contents as the given reference trace, modulo some normalization.
        /// </summary>
        protected void AssertNormalizedTraceFileEqualsReference(string[] actualTraceContent, int[] assembliesToCompare)
        {
            FileInfo referenceTraceFile = new FileInfo(GetTestDataPath("reference-traces", GetSanitizedTestName() + ".txt"));
            string[] referenceTraceFileContent = File.ReadAllLines(referenceTraceFile.FullName);

            var assemblyIds = new HashSet<int>(assembliesToCompare);
            Assert.AreEqual(ReadNormalizedTraceContent(referenceTraceFileContent, assemblyIds),
                        ReadNormalizedTraceContent(actualTraceContent, assemblyIds),
                        "The normalized contents of the trace files did not match");
        }

        /// <summary>
        /// Returns the absolute path to a test data file.
        /// </summary>
        protected static string GetTestDataPath(params string[] path)
            => Path.Combine(SolutionRoot.FullName, "test-data", Path.Combine(path));

        /// <summary>
        /// An executable file in the TestProgramsDirectory.
        /// </summary>
        protected FileInfo GetTestProgram(string executableName) => new FileInfo(Path.Combine(TestProgramsDirectory.FullName, executableName));

        /// <summary>
        /// The directory containing the testee binaries.
        /// </summary>
        private static DirectoryInfo TestProgramsDirectory =>
            new DirectoryInfo(GetTestDataPath("test-programs"));
        
        /// <summary>
        /// Returns a test-specific directory for temp files
        /// </summary>
        protected static string TestTempDirectory =>
            Path.Combine(SolutionRoot.FullName, "test-tmp", GetSanitizedTestName());
        
        /// <summary>
        /// A temporary directory for a profiler to write trace files to.
        /// </summary>
        protected static DirectoryInfo TestTraceDirectory =>
            new DirectoryInfo(TestTempDirectory).CreateSubdirectory("traces");

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
        private static string ReadNormalizedTraceContent(string[] traceFileContent, HashSet<int> assembliesToCompare)
        {
            ILookup<string, string> traceMap = KeyValuesMapFor(traceFileContent);

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
        /// Parse trace into map from key to list of values.
        /// </summary>
        private static ILookup<string, string> KeyValuesMapFor(string[] coverageReport)
            => coverageReport.Where(line => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                .Select(line => line.Split(new char[] { '=' }, 2))
                .ToLookup(split => split[0], split => split[0]);
    }
}


using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace Cqse.Teamscale.Profiler.Dotnet.Targets
{
    public class TiaTestee : Testee<TiaTesteeProcess>
    {
        public TiaTestee(DirectoryInfo testProgramsDirectory, Bitness bitness) : base(GetExecutableName(testProgramsDirectory, bitness)) {}

        private static FileInfo GetExecutableName(DirectoryInfo testProgramsDirectory, Bitness bitness)
        {
            string executable = "ProfilerTestee32.exe";
            if (bitness == Bitness.x64)
            {
                executable = "ProfilerTestee64.exe";
            }
            return new FileInfo(Path.Combine(testProgramsDirectory.FullName, executable));
        }

        /// <inheritDoc/>
        public override TiaTesteeProcess Start(string arguments = null, IProfiler profiler = null)
        {
            TiaTesteeProcess process = base.Start(arguments: "interactive", profiler);
            Assert.That(process.Output.ReadLine(), Is.EqualTo("interactive"));
            Assert.That(process.HasExited, Is.False);
            return process;
        }

        /// <inheritDoc/>
        protected override TiaTesteeProcess CreateProcess(Process process)
        {
            return new TiaTesteeProcess(process);
        }
    }

    public class TiaTesteeProcess : TesteeProcess
    {
        public TiaTesteeProcess(Process process) : base(process) { }

        /// <summary>
        /// Sends a command to execute a specific test case.
        /// </summary>
        public TiaTesteeProcess RunTestCase(string testName)
        {
            Input.WriteLine(testName);
            Assert.That(Output.ReadLine(), Is.EqualTo(testName));
            return this;
        }

        /// <summary>
        /// Sends a termination command to the process and waits for it to terminate.
        /// </summary>
        public void Stop()
        {
            Input.WriteLine();
            WaitForExit();
        }
    }
}
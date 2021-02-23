
using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using static Cqse.Teamscale.Profiler.Dotnet.ProfilerTestBase;

namespace Cqse.Teamscale.Profiler.Dotnet.Targets
{
    /// <summary>
    /// A process that can be run for testing the profiler.
    /// </summary>
    public abstract class Testee<P> where P : TesteeProcess
    {
        private FileInfo executable;

        public Testee(FileInfo exectable)
        {
            this.executable = exectable;
        }

        /// <summary>
        /// Starts a process running this executable and waits till the process terminates successfully.
        /// </summary>
        public void Run(string arguments = null, IProfiler profiler = null)
        {
            Start(arguments, profiler).WaitForExit();
        }

        /// <summary>
        /// Starts a process running this executable and returns it.
        /// </summary>
        public virtual P Start(string arguments = null, IProfiler profiler = null)
        {
            if (profiler == null)
            {
                profiler = new NoProfiler();
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(executable.FullName, arguments)
            {
                WorkingDirectory = executable.DirectoryName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            profiler.RegisterOn(startInfo);

            Process process = Process.Start(startInfo);
            return CreateProcess(process);
        }

        /// <summary>
        /// Creates a TesteeProcess wrapping a Process.
        /// </summary>
        protected abstract P CreateProcess(Process process);
    }

    /// <summary>
    /// A process resulting from running an exectable.
    /// </summary>
    public class TesteeProcess
    {
        private Process process;

        public StreamWriter Input => process.StandardInput;

        public StreamReader Output => process.StandardOutput;

        public bool HasExited => process.HasExited;

        public TesteeProcess(Process process)
        {
            this.process = process;
        }

        /// <summary>
        /// Wait until the executable terminates successfully.
        /// </summary>
        public void WaitForExit()
        {
            Output.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.Zero);
        }

        /// <summary>
        /// Asserts that the process terminated with exit code 0.
        /// </summary>
        public void AssertSuccess()
        {
            Assert.That(process.ExitCode, Is.EqualTo(0), "Program did not execute properly.");
        }
    }
}

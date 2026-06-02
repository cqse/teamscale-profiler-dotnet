using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    class Testee
    {
        private FileInfo executable;

        public Bitness bitness;

        public Testee(FileInfo executable, Bitness bitness = Bitness.x64)
        {
            this.bitness = bitness;
            this.executable = executable;
        }

        /// <summary>
        /// Starts a process running this executable and waits till the process terminates successfully.
        /// </summary>
        public void Run(IProfiler profiler, string arguments = null)
        {
            Start(profiler, arguments).WaitForExit();
        }

        /// <summary>
        /// Starts a process running this executable and returns it.
        /// </summary>
        public virtual TesteeProcess Start(IProfiler profiler, string arguments)
        {

            ProcessStartInfo startInfo = new ProcessStartInfo(executable.FullName, arguments)
            {
                WorkingDirectory = executable.DirectoryName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            profiler.RegisterOn(startInfo, bitness);

            Process process = Process.Start(startInfo);
            return new TesteeProcess(process);
        }
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
    }
}

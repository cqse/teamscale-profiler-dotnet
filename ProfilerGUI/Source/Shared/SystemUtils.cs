using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProfilerGUI.Source.Shared
{
    /// <summary>
    /// System utility functions.
    /// </summary>
    public static class SystemUtils
    {
        /// <summary>
        ///  Sets the given environment variable on the 'Process' level, unless it is already configured. I.e. this process and any of its children
        ///  will see the environment variable. Returns <code>true</code>
        ///  if changes were made, i.e. the variable was not already set to the target value.
        /// </summary>
        public static bool SetEnvironmentVariable(string variableName, string value)
        {
            if (!Equals(Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Process), value))
            {
                Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.Process);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Runs the given executable with the given command line arguments and waits for the process to terminate.
        /// </summary>
        public static ProcessResult RunProcessAndWait(string processPath, string args = "")
        {
            Process process = new Process
            {
                StartInfo = CreateStartInfo(processPath, args, false)
            };

            process.Start();

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();

            process.WaitForExit();

            return new ProcessResult(process.ExitCode, stdout, stderr);
        }

        /// <summary>
        /// Asynchronously runs the given executable with the given command line arguments, working dir and environment.
        /// </summary>
        public static Process RunNonBlocking(string processPath, string args, string workingDir, params Tuple<string, string>[] environmentVariables)
        {
            Process process = new Process
            {
                StartInfo = CreateStartInfo(processPath, args, true, workingDir, environmentVariables)
            };
            process.EnableRaisingEvents = true;
            process.Start();
            return process;
        }

        private static ProcessStartInfo CreateStartInfo(string processPath, string args, bool runAsIndependentApplication, string workingDir = "",
            params Tuple<string, string>[] environmentVariables)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = args
            };

            startInfo.UseShellExecute = false;

            foreach (Tuple<string, string> environmentVariableAndValue in environmentVariables)
            {
                string variableName = environmentVariableAndValue.Item1;
                startInfo.EnvironmentVariables.Remove(variableName);
                startInfo.EnvironmentVariables.Add(variableName, environmentVariableAndValue.Item2);
            }

            if (runAsIndependentApplication)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.WorkingDirectory = workingDir;
                startInfo.CreateNoWindow = false;
            }
            else
            {
                // TODO (FS) doesn't that mess with our output collection above?
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;
            }

            return startInfo;
        }
    }
}
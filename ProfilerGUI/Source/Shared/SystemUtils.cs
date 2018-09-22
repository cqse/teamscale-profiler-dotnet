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
        /// Asynchronously runs the given executable with the given command line arguments, working dir and environment.
        /// Does not enable process events.
        /// </summary>
        public static Process RunNonBlocking(string processPath, string args, string workingDir, List<(string, string)> environmentVariables)
        {
            Process process = new Process
            {
                StartInfo = CreateStartInfo(processPath, args, workingDir, environmentVariables)
            };
            process.Start();
            return process;
        }

        private static ProcessStartInfo CreateStartInfo(string processPath, string args, string workingDir, List<(string, string)> environmentVariables)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = args
            };

            startInfo.UseShellExecute = false;

            foreach ((string environmentVariable, string value) in environmentVariables)
            {
                // we must first remove the variable or otherwise the Add call will throw an exception
                startInfo.EnvironmentVariables.Remove(environmentVariable);
                startInfo.EnvironmentVariables.Add(environmentVariable, value);
            }

            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.WorkingDirectory = workingDir;
            startInfo.CreateNoWindow = false;

            return startInfo;
        }
    }
}
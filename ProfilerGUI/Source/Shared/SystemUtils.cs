using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ProfilerGUI.Source.Shared
{
    public static class SystemUtils
    {
        /// <summary>
        ///  Sets the given environment variable on the 'Machine' level, unless it is already configured. Returns <code>true</code> 
        ///  if changes were made, i.e. the variable was not already set to the target value.
        /// </summary>
        public static bool SetEnvironmentVariable(string variableName, string value)
        {
            if (!Equals(Environment.GetEnvironmentVariable(variableName, EnvironmentVariableTarget.Machine), value))
            {
                Environment.SetEnvironmentVariable(variableName, value, EnvironmentVariableTarget.Machine);
                return true;
            }
            return false;
        }

        public static ProcessOutput RunProcessAndWait(string processPath, string args = "")
        {
            Process process = new Process
            {
                StartInfo = CreateStartInfo(processPath, args, false)
            };

            process.Start();

            List<string> output = new List<string>();
            CollectOuput(process.StandardOutput, output);
            CollectOuput(process.StandardError, output);

            process.WaitForExit();

            return new ProcessOutput(process.ExitCode, output);
        }

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

        private static ProcessStartInfo CreateStartInfo(string processPath, string args, bool runAsIndependentApplication, string workingDir = "", params Tuple<string, string>[] environmentVariables)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = processPath,
                Arguments = args
            };

            startInfo.UseShellExecute = false;

            if (environmentVariables != null)
            {
                foreach (Tuple<string, string> environmentVariableAndValue in environmentVariables)
                {
                    string variableName = environmentVariableAndValue.Item1;
                    startInfo.EnvironmentVariables.Remove(variableName);
                    startInfo.EnvironmentVariables.Add(variableName, environmentVariableAndValue.Item2);
                }
            }

            if (runAsIndependentApplication)
            {
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.WorkingDirectory = workingDir;
                startInfo.CreateNoWindow = false;
            }
            else
            {
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;
            }

            return startInfo;
        }

        private static void CollectOuput(StreamReader outputStream, List<string> output)
        {
            while (!outputStream.EndOfStream)
            {
                output.Add(outputStream.ReadLine());
            }
        }
    }
}

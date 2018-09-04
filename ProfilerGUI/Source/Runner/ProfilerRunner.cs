using NLog;
using ProfilerGUI.Source.Configurator;
using ProfilerGUI.Source.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ProfilerGUI.Source.Runner
{
    /// <summary>
    /// Starts the target application with the profiler attached.
    /// </summary>
    public class ProfilerRunner
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ProfilerConfiguration configuration;

        public ProfilerRunner(ProfilerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Asynchronously runs the target process with the profiler attached.
        /// </summary>
        public void RunAsynchronously()
        {
            string profilerDllPath = LocateProfilerDll();
            if (!File.Exists(profilerDllPath))
            {
                logger.Error("The profiler DLL was not found at {profilerPath}. Cannot profile the application", profilerDllPath);
                return;
            }

            List<(string, string)> environmentVariables = new List<(string, string)>()
                {
                    (ProfilerConstants.ProfilerIdEnvironmentVariable, ProfilerConstants.ProfilerGuid),
                    (ProfilerConstants.ProfilerPathEnvironmentVariable, profilerDllPath),
                    (ProfilerConstants.EnableProfilingEnvironmentVariable, "1"),
                    (ProfilerConstants.TargetDirectoryEnvironmentVariable, configuration.TraceTargetFolder),
                    (ProfilerConstants.LightModeEnvironmentVariable, "1"),
                };

            logger.Info("Running {bitness} application {appPath} in working dir {workingDir} with arguments [{arguments}] and environment [{env}]",
                configuration.ApplicationType, configuration.TargetApplicationPath, configuration.WorkingDirectory, configuration.TargetApplicationArguments, environmentVariables);

            try
            {
                SystemUtils.RunNonBlocking(configuration.TargetApplicationPath, configuration.TargetApplicationArguments,
                    configuration.WorkingDirectory, environmentVariables);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to launch application {appPath}", configuration.TargetApplicationPath);
            }
        }

        private string LocateProfilerDll()
        {
            string profilerDll = ProfilerConstants.ProfilerDll64Bit;
            if (configuration.ApplicationType == EApplicationType.Type32Bit)
            {
                profilerDll = ProfilerConstants.ProfilerDll32Bit;
            }
            return Directory.GetParent(Environment.CurrentDirectory) + @"\" + profilerDll;
        }
    }
}
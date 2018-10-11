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
            List<(string, string)> environmentVariables;

            try
            {
                environmentVariables = DetermineEnvironmentVariables(configuration);
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return;
            }

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

        private static List<(string, string)> DetermineEnvironmentVariables(ProfilerConfiguration configuration)
        {
            List<(string, string)> environmentVariables;
            if (configuration.ApplicationType == EApplicationType.TypeCORE)
            {
                environmentVariables = new List<(string, string)>()
                {
                    (ProfilerConstants.CoreProfilerIdEnvironmentVariable, ProfilerConstants.ProfilerGuid),
                    (ProfilerConstants.CoreProfilerPath32EnvironmentVariable, GetProfilerDllPath(ProfilerConstants.ProfilerDll32Bit)),
                    (ProfilerConstants.CoreProfilerPath64EnvironmentVariable, GetProfilerDllPath(ProfilerConstants.ProfilerDll64Bit)),
                    (ProfilerConstants.CoreEnableProfilingEnvironmentVariable, "1"),
                };
            }
            else
            {
                string profilerDll = ProfilerConstants.ProfilerDll64Bit;
                if (configuration.ApplicationType == EApplicationType.Type32Bit)
                {
                    profilerDll = ProfilerConstants.ProfilerDll32Bit;
                }
                
                environmentVariables = new List<(string, string)>()
                {
                    (ProfilerConstants.ProfilerIdEnvironmentVariable, ProfilerConstants.ProfilerGuid),
                    (ProfilerConstants.ProfilerPathEnvironmentVariable, GetProfilerDllPath(profilerDll)),
                    (ProfilerConstants.EnableProfilingEnvironmentVariable, "1"),
                };
            }
            
            environmentVariables.AddRange(new List<(string, string)>()
            {
                (ProfilerConstants.TargetDirectoryEnvironmentVariable, configuration.TraceTargetFolder),
                (ProfilerConstants.LightModeEnvironmentVariable, "1")
            });

            return environmentVariables;
        }

        private static string GetProfilerDllPath(string profilerDll)
        {
            var path = Directory.GetParent(Environment.CurrentDirectory) + @"\" + profilerDll;
            if (!File.Exists(path))
            {
                throw new ArgumentException(string.Format("The profiler DLL was not found at \"{0}\". Cannot profile the application", path));
            }
            return path;
        }
    }
}
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
        private readonly ProfilerConfiguration configuration;

        /// <summary> Called if the profiling ended, i.e. all processes to profile were stopped. </summary>
        public event EventHandler<EventArgs> ProfilingEndHandler;

        public ProfilerRunner(ProfilerConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Asynchronously runs the target process with the profiler attached.
        /// </summary>
        public Task Run()
        {
            return Task.Factory.StartNew(() =>
            {
                Tuple<string, string> guidVariable = Tuple.Create(ProfilerConstants.ProfilerIdEnvironmentVariable, ProfilerConstants.ProfilerGuid);
                Tuple<string, string> dllVariable = CreateProfilerDllVariableTuple();
                Tuple<string, string> enableVariable = Tuple.Create(ProfilerConstants.EnableProfilingEnvironmentVariable, "1");
                Tuple<string, string> targetDirVariable = Tuple.Create(ProfilerConstants.TargetDirectoryEnvironmentVariable, configuration.TraceTargetFolder);
                // TODO (FS) make configurable? uploader!
                Tuple<string, string> lightModeVariable = Tuple.Create(ProfilerConstants.LightModeEnvironmentVariable, "1");

                Process process = SystemUtils.RunNonBlocking(configuration.TargetApplicationPath, configuration.TargetApplicationArguments,
                    configuration.WorkingDirectory, guidVariable, dllVariable, enableVariable, targetDirVariable, lightModeVariable);
                process.Exited += OnTargetProcessEnd;
            });
        }

        private Tuple<string, string> CreateProfilerDllVariableTuple()
        {
            string profilerDll = ProfilerConstants.ProfilerDll64Bit;
            if (configuration.ApplicationType == EApplicationType.Type32Bit)
            {
                profilerDll = ProfilerConstants.ProfilerDll32Bit;
            }
            return Tuple.Create(ProfilerConstants.ProfilerPathEnvironmentVariable, Directory.GetParent(Environment.CurrentDirectory) + @"\" + profilerDll);
        }

        private void OnTargetProcessEnd(object sender, EventArgs e)
        {
            ProfilingEndHandler?.Invoke(this, EventArgs.Empty);
        }
    }
}
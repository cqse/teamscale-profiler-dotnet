using NLog;
using ProfilerGUI.Source.Runner;
using ProfilerGUI.Source.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// The main view model to interact with the <see cref="MainWindow"/>.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary> <inheritDoc /> </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Model for the application to profile.
        /// </summary>
        public readonly TargetAppModel TargetApp;

        /// <summary>
        /// Whether to show the additional application fields.
        /// </summary>
        public bool ShowAdditionalApplicationFields { get => !string.IsNullOrEmpty(TargetApp.ApplicationPath); }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="launchTargetAppDirectly">If this is set, then the target application is started right away.
        /// A default config must exist for this to do anything.</param>
        public MainViewModel(bool launchTargetAppDirectly)
        {
            ProfilerConfiguration configuration = new ProfilerConfiguration();
            if (File.Exists(ProfilerConfiguration.ConfigFilePath))
            {
                configuration = ProfilerConfiguration.ReadFromFile();

                if (launchTargetAppDirectly)
                {
                    RunProfiledApplication();
                }
            }

            TargetApp = new TargetAppModel(configuration);
        }

        /// <summary>
        /// Resets the fields that describe the application that should be profiled.
        /// </summary>
        internal void ClearTargetAppFields()
        {
            TargetApp.Clear();
        }

        /// <summary>
        /// Saves the current configuration to a file.
        /// </summary>
        internal void SaveConfiguration()
        {
            try
            {
                TargetApp.Configuration.WriteToFile();
                logger.Info("Wrote config to {configPath}", ProfilerConfiguration.ConfigFilePath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Could not save configuration to {configPath}", ProfilerConfiguration.ConfigFilePath);
            }
        }

        /// <summary>
        /// Starts the profiled application with the profiler attached.
        /// </summary>
        internal async void RunProfiledApplication()
        {
            logger.Info("Profiling {targetAppPath}", TargetApp.Configuration.TargetApplicationPath);
            ProfilerRunner runner = new ProfilerRunner(TargetApp.Configuration);
            await runner.Run();
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
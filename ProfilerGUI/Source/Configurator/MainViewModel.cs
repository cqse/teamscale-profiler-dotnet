using Newtonsoft.Json;
using NLog;
using ProfilerGUI.Source.Runner;
using ProfilerGUI.Source.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Common;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// The main view model to interact with the <see cref="MainWindow"/>.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string UploadConfigFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "UploadDaemon", UploadConfig.ConfigFileName);

        /// <summary> <inheritDoc /> </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Model for the application to profile.
        /// </summary>
        public TargetAppModel TargetApp { get; private set; }

        /// <summary>
        /// Index of the selected bitness (32bit vs 64bit).
        /// </summary>
        public int SelectedBitnessIndex
        {
            get
            {
                return (int)TargetApp.ApplicationType;
            }
            set
            {
                TargetApp.ApplicationType = (EApplicationType)value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        private UploadConfig uploadConfig = null;

        /// <summary>
        /// Summary of the configured upload.
        /// </summary>
        public string UploadSummary
        {
            get
            {
                if (uploadConfig == null)
                {
                    return "No upload";
                }
                if (uploadConfig.Teamscale != null)
                {
                    return $"Upload to {uploadConfig.Teamscale}";
                }
                return $"Upload to directory {uploadConfig.Directory}";
            }
        }

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
                logger.Info("Loaded config from {configPath}", ProfilerConfiguration.ConfigFilePath);
                configuration = ProfilerConfiguration.ReadFromFile();

                if (launchTargetAppDirectly)
                {
                    RunProfiledApplication();
                }
            }

            TargetApp = new TargetAppModel(configuration);
            TargetApp.PropertyChanged += (sender, args) =>
            {
                UiUtils.ReRaise(PropertyChanged, this, nameof(TargetApp), args);

                if (args.PropertyName == nameof(TargetApp.ApplicationType))
                {
                    UiUtils.Raise(PropertyChanged, this, nameof(SelectedBitnessIndex));
                }
            };

            uploadConfig = ReadUploadConfigFromDisk();
        }

        /// <summary>
        /// Shows the dialog to configure the trace upload.
        /// </summary>
        internal void OpenUploadConfigDialog()
        {
            UploadConfigWindow dialog = new UploadConfigWindow(uploadConfig);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                uploadConfig = dialog.Config;
            }
            UiUtils.Raise(PropertyChanged, this, nameof(UploadSummary));
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
                SaveUploadConfig();
            }
            catch (Exception e)
            {
                logger.Error(e, "Could not save configuration to {configPath}", ProfilerConfiguration.ConfigFilePath);
            }
        }

        private UploadConfig ReadUploadConfigFromDisk()
        {
            if (!File.Exists(UploadConfigFilePath))
            {
                return null;
            }

            try
            {
                return UploadConfig.ReadFromFile(UploadConfigFilePath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to load upload deamon configuration from {uploadConfigPath}", UploadConfigFilePath);
                return null;
            }
        }

        /// <summary>
        /// Saves the config for the upload daemon.
        /// </summary>
        private void SaveUploadConfig()
        {
            try
            {
                File.WriteAllText(UploadConfigFilePath, JsonConvert.SerializeObject(uploadConfig));
                logger.Info("Wrote upload deamon config to {configPath}", UploadConfigFilePath);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to save upload deamon configuration to {uploadConfigPath}", UploadConfigFilePath);
            }
        }

        /// <summary>
        /// Starts the profiled application with the profiler attached.
        /// </summary>
        internal void RunProfiledApplication()
        {
            if (string.IsNullOrEmpty(TargetApp.ApplicationPath))
            {
                logger.Error("You did not configure an application to profile");
                return;
            }

            ProfilerRunner runner = new ProfilerRunner(TargetApp.Configuration);
            runner.RunAsynchronously();
        }
    }
}
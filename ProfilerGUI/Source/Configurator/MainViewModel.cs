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
        /// <summary> <inheritDoc /> </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The configuration.
        /// </summary>
        public readonly ProfilerConfiguration Configuration = new ProfilerConfiguration();

        /// <summary>
        /// Whether to show the additional application fields.
        /// </summary>
        public bool ShowAdditionalApplicationFields { get => !string.IsNullOrEmpty(TargetApplicationPath); }

        /// <summary>
        /// The directory into which to write the trace files.
        /// </summary>
        public string TargetTraceDirectory
        {
            get => Configuration.TraceTargetFolder;
            set
            {
                Configuration.TraceTargetFolder = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Index of the selected bitness (32bit vs 64bit).
        /// </summary>
        public int SelectedBitnessIndex
        {
            get
            {
                return (int)Configuration.ApplicationType;
            }
            set
            {
                Configuration.ApplicationType = (EApplicationType)value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Path to the application that should be started.
        /// </summary>
        public string TargetApplicationPath
        {
            get => Configuration.TargetApplicationPath;
            set
            {
                Configuration.TargetApplicationPath = value;
                if (Configuration.TargetApplicationPath?.EndsWith(WindowsShortcutReader.ShortcutExtension) == true)
                {
                    FillValuesFromLnkFile(Configuration.TargetApplicationPath);
                }
                else
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowAdditionalApplicationFields));
                    UpdateExecutableMachineType();
                }
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="launchTargetAppDirectly">If this is set, then the target application is started right away.
        /// A default config must exist for this to do anything.</param>
        public MainViewModel(bool launchTargetAppDirectly)
        {
            if (File.Exists(ProfilerConfiguration.ConfigFilePath))
            {
                Configuration = ProfilerConfiguration.ReadFromFile();

                if (launchTargetAppDirectly)
                {
                    RunProfiledApplication();
                }
            }
        }

        /// <summary>
        /// Resets the fields that describe the application that should be profiled.
        /// </summary>
        internal void ClearTargetAppFields()
        {
            TargetApplicationPath = null;
            TargetAppWorkingDirectory = null;
            TargetAppArguments = null;
        }

        private void UpdateExecutableMachineType()
        {
            if (string.IsNullOrEmpty(TargetApplicationPath))
            {
                return;
            }

            MachineTypeUtils.MachineType typeOfExecutable = MachineTypeUtils.GetExecutableType(TargetApplicationPath);
            if (typeOfExecutable == MachineTypeUtils.MachineType.Unknown)
            {
                LogWarn("Could not determine if target app is 32 or 64 bit, please set manually");
                return;
            }

            if (typeOfExecutable == MachineTypeUtils.MachineType.I386)
            {
                SelectedBitnessIndex = (int)EApplicationType.Type32Bit;
            }
            else
            {
                SelectedBitnessIndex = (int)EApplicationType.Type64Bit;
            }
        }

        private void FillValuesFromLnkFile(string targetApplicationPath)
        {
            try
            {
                WindowsShortcutReader.ShortcutInfo shortcutInfo = WindowsShortcutReader.ReadShortcutInfo(targetApplicationPath);
                TargetApplicationPath = shortcutInfo.TargetPath;
                TargetAppWorkingDirectory = shortcutInfo.WorkingDirectory;
                TargetAppArguments = shortcutInfo.Arguments;
            }
            catch
            {
                LogError("Could not read .lnk file. Please set values manually");
            }
        }

        private string InternalLogText = "";

        /// <summary>
        /// Stores the log's text.
        /// </summary>
        public string LogText
        {
            get => InternalLogText;
            set
            {
                InternalLogText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The command line arguments of the profiled application.
        /// </summary>
        public string TargetAppArguments
        {
            get => Configuration.TargetApplicationArguments;
            internal set
            {
                Configuration.TargetApplicationArguments = value;
                OnPropertyChanged();
            }
        }

        // TODO (FS) move properties to the top

        /// <summary>
        /// The working directory of the profiled application.
        /// </summary>
        public string TargetAppWorkingDirectory
        {
            get => Configuration.WorkingDirectory;
            set
            {
                Configuration.WorkingDirectory = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Saves the current configuration to a file.
        /// </summary>
        internal void SaveConfiguration()
        {
            ClearLog();

            try
            {
                Configuration.WriteToFile();
                LogInfo("Wrote config to " + ProfilerConfiguration.ConfigFilePath);
            }
            catch (Exception e)
            {
                LogError($"Could not save configuration to {ProfilerConfiguration.ConfigFilePath}: {e.Message}");
            }
        }

        /// <summary>
        /// Starts the profiled application with the profiler attached.
        /// </summary>
        internal async void RunProfiledApplication()
        {
            LogInfo("Profiling " + Configuration.TargetApplicationPath);
            ProfilerRunner runner = new ProfilerRunner(Configuration);
            await runner.Run();
        }

        private void LogError(string message)
        {
            LogText += $"ERROR: {message}\r\n";
        }

        private void LogWarn(string message)
        {
            LogText += $"WARN:  {message}\r\n";
        }

        private void LogInfo(string message)
        {
            LogText += $"INFO:  {message}\r\n";
        }

        private void ClearLog()
        {
            LogText = string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
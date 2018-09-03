
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

        // TODO (FS) please add  missing comments to public stuff
        public readonly ProfilerConfiguration Configuration = new ProfilerConfiguration();

        public bool ShowAdditionalApplicationFields { get => !string.IsNullOrEmpty(TargetApplicationPath); }

        public string TargetTraceDirectory
        {
            get => Configuration.TraceTargetFolder;
            set
            {
                Configuration.TraceTargetFolder = value;
                OnPropertyChanged();
            }
        }

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

        public string TargetFolderToCopyTo
        {
            get => Configuration.TraceFolderToCopyTo;
            set
            {
                Configuration.TraceFolderToCopyTo = value;
                OnPropertyChanged();
            }
        }

        public string ProcessNamesToWaitFor
        {
            get => string.Join(",", Configuration.ExecutablesToWaitFor);
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    Configuration.ExecutablesToWaitFor.Clear();
                }
                else
                {
                    Configuration.ExecutablesToWaitFor = value.Split(',').Select(name => name.Trim()).ToList();
                }
                OnPropertyChanged();
            }
        }

        public string TargetApplicationPath
        {
            get => Configuration.TargetApplicationPath;
            set
            {
                Configuration.TargetApplicationPath = value;
                if (Configuration.TargetApplicationPath?.EndsWith(WindowsShortcutReader.ShortcutExtension) == true)
                {
                    FillValuesFromInkFile(Configuration.TargetApplicationPath);
                }
                else
                {
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowAdditionalApplicationFields));
                    UpdateExecutableMachineType();
                }
            }
        }

        public MainViewModel(bool launchTargetAppDirectly)
        {
            if (File.Exists(ProfilerConstants.DefaultConfigFile))
            {
                Configuration = ProfilerConfiguration.ReadFromFile(ProfilerConstants.DefaultConfigFile);

                if (launchTargetAppDirectly)
                {
                    RunProfiledApplication();
                }
            }
        }

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
            MachineTypeExtractor.MachineType typeOfExecutable = MachineTypeExtractor.GetExecutableType(TargetApplicationPath);
            if (typeOfExecutable == MachineTypeExtractor.MachineType.Unknown)
            {
                LogLine("Warning: Could not determine if target app is 32 or 64 bit, please set manually.");
                return;
            }

            bool is32BitApp = typeOfExecutable == MachineTypeExtractor.MachineType.I386;
            if (is32BitApp)
            {
                SelectedBitnessIndex = (int)EApplicationType.Type32Bit;
            }
            else
            {
                SelectedBitnessIndex = (int)EApplicationType.Type64Bit;
            }
        }

        // TODO (FS) isn't it Lnk not Ink (like "l(i)nk")? If so, please fix this here and at other locations
        private void FillValuesFromInkFile(string targetApplicationPath)
        {
            try
            {
                WindowsShortcutReader.ShortcutInfo shortcutInfo = WindowsShortcutReader.ReadShortcutInfo(targetApplicationPath);
                TargetApplicationPath = shortcutInfo.TargetPath;
                TargetAppWorkingDirectory = shortcutInfo.WorkingDirectory;
                TargetAppArguments = shortcutInfo.Arguments;
            }
            catch (Exception)
            {
                LogLine("Could not read ink-file, please set values manually.");
            }
        }

        private string InternalLogText = "No log yet...";

        public string LogText
        {
            get => InternalLogText;
            set
            {
                InternalLogText = value;
                OnPropertyChanged();
            }
        }

        public string TargetAppArguments
        {
            get => Configuration.TargetApplicationArguments;
            internal set
            {
                Configuration.TargetApplicationArguments = value;
                OnPropertyChanged();
            }
        }

        public string TargetAppWorkingDirectory
        {
            get => Configuration.WorkingDirectory;
            set
            {
                Configuration.WorkingDirectory = value;
                OnPropertyChanged();
            }
        }

        internal void SaveConfiguration()
        {
            ClearLog();


            List<Tuple<string, string>> argumentValueTuples = new List<Tuple<string, string>>
                {
                    Tuple.Create(ProfilerConstants.ProfilerIdArgName, ProfilerConstants.ProfilerIdArgValue),
                    Tuple.Create(ProfilerConstants.TargetDirectoryForTracesArg, TargetTraceDirectory),
                    Tuple.Create(ProfilerConstants.ProfilerLightModeArg, "1"),
                };


            foreach (Tuple<string, string> argumentValueTuple in argumentValueTuples)
            {
                // TODO (FS) extract a method for the loop content?
                LogLine($"Setting {argumentValueTuple.Item1} = {argumentValueTuple.Item2} ... ");
                bool changesMade = SystemUtils.SetEnvironmentVariable(argumentValueTuple.Item1, argumentValueTuple.Item2);
                if (changesMade)
                {
                    Log("OK");
                }
                else
                {
                    // TODO (FS) why does this have a space at the beginning but the OK does not?
                    Log(" (already set)");
                }
            }

            LogLine("Storing config file as " + ProfilerConstants.DefaultConfigFile);

            try
            {
                Configuration.WriteToFile(ProfilerConstants.DefaultConfigFile);
                LogLine("-> Success");
                LogLine("--- Ready to run profiled app --- ");
            }
            catch (Exception e)
            {
                LogLine("ERROR: Could not save file: " + e.Message);
            }
        }

        internal async void RunProfiledApplication()
        {
            LogLine("Profiling " + Configuration.TargetApplicationPath);
            ProfilerConfiguration configuration = ProfilerConfiguration.ReadFromFile(ProfilerConstants.DefaultConfigFile);
            ProfilerRunner runner = new ProfilerRunner(configuration);
            runner.ProfilingEnd += delegate
            {
                if (configuration.QuitToolAfterProcessesEnded)
                {
                    Environment.Exit(0);
                }
            };
            runner.TraceFileCopy += (sender, args) =>
            {
                // TODO (FS) can we log the error messages if any exist? that might be helpful to the user to debug problems during the copy
                LogLine($"Copied {args.SuccessfulCopiedFiles.Count} trace files, errors: {args.FailedCopiedFiles.Count}");
            };
            await runner.Run();
        }

        private void LogLine(string line)
        {
            // TODO (FS) it's a bit weird that logline prepends the newline chars. I expected it to append them. can you either make this clear
            // from the method name or change the code so this can append the newline chars (like println vs print in java)
            Log("\r\n" + line);
        }

        private void Log(string text)
        {
            LogText += text;
        }

        private void ClearLog()
        {
            LogText = string.Empty;
        }

        private void ReportSuccessOrFailure(ProcessOutput output)
        {
            if (output.ReturnCode != 0)
            {
                LogLine("ERROR:\n" + string.Join("\t\n", output.Output));
                // TODO (FS) useless return. delete?
                return;
            }
            else
            {
                LogLine("OK");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

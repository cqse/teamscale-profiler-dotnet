using NLog;
using ProfilerGUI.Source.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProfilerGUI.Source.Configurator
{
    public class TargetAppModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary> <inheritDoc /> </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The configuration.
        /// </summary>
        public readonly ProfilerConfiguration Configuration;

        /// <summary>
        /// Constructor
        /// </summary>
        public TargetAppModel(ProfilerConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// The directory into which to write the trace files.
        /// </summary>
        public string TraceDirectory
        {
            get => Configuration.TraceTargetFolder;
            set
            {
                Configuration.TraceTargetFolder = value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// The selected bitness (32bit vs 64bit).
        /// </summary>
        public EApplicationType ApplicationType
        {
            get => Configuration.ApplicationType;
            set
            {
                Configuration.ApplicationType = value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// Path to the application that should be started.
        /// </summary>
        public string ApplicationPath
        {
            get => Configuration.TargetApplicationPath;
            set
            {
                Configuration.TargetApplicationPath = value;
                if (Configuration.TargetApplicationPath?.EndsWith(Shortcut.Extension) == true)
                {
                    FillValuesFromLnkFile(Configuration.TargetApplicationPath);
                }
                else
                {
                    UpdateExecutableMachineType();
                }
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// The command line arguments of the profiled application.
        /// </summary>
        public string Arguments
        {
            get => Configuration.TargetApplicationArguments;
            set
            {
                Configuration.TargetApplicationArguments = value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// The working directory of the profiled application.
        /// </summary>
        public string WorkingDirectory
        {
            get => Configuration.WorkingDirectory;
            set
            {
                Configuration.WorkingDirectory = value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// Clears all fields of this model.
        /// </summary>
        public void Clear()
        {
            ApplicationPath = null;
            WorkingDirectory = null;
            Arguments = null;
        }

        private void UpdateExecutableMachineType()
        {
            if (string.IsNullOrEmpty(ApplicationPath))
            {
                return;
            }

            if (IsDotNETCoreApplication(ApplicationPath))
            {
                ApplicationType = EApplicationType.DotNetCore;
            }
            else
            {
                switch (MachineTypeUtils.DetermineBitness(ApplicationPath))
                {
                    case MachineTypeUtils.Bitness.Bitness32:
                        ApplicationType = EApplicationType.DotNetFramework32Bit;
                        break;

                    case MachineTypeUtils.Bitness.Bitness64:
                        ApplicationType = EApplicationType.DotNetFramework64Bit;
                        break;

                    default:
                        logger.Warn("Could not determine if target app is 32 or 64 bit, please set manually");
                        break;
                }
            }
        }

        private static bool IsDotNETCoreApplication(string applicationPath)
        {
            try
            {
                // If the application is .NET, we can load it as an assembly.
                // We load into the reflection context here, to avoid loading dependencies
                // and prevent any code from executing.
                var assembly = Assembly.ReflectionOnlyLoadFrom(applicationPath);

                // If it is .NET Core it does not reference mscorlib.
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    if (reference.Name == "mscorlib")
                    {
                        return false;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void FillValuesFromLnkFile(string targetApplicationPath)
        {
            try
            {
                Shortcut shortcut = Shortcut.ReadShortcutFile(targetApplicationPath);
                ApplicationPath = shortcut.TargetPath;
                WorkingDirectory = shortcut.WorkingDirectory;
                Arguments = shortcut.Arguments;
            }
            catch
            {
                logger.Error("Could not read .lnk file. Please set values manually");
            }
        }
    }
}
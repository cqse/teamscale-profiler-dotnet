using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// View model for configuring the uploader daemon.
    /// </summary>
    internal class UploadViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string ConfigFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "UploadDaemon", UploadDaemon.Config.ConfigFileName);

        /// <summary>
        /// <inheritDoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The config for the daemon.
        /// </summary>
        public UploadDaemon.Config Config { get; private set; } = null;

        private readonly UploadDaemon.Config originalConfig = null;

        /// <summary>
        /// Whether the configuration UI for the Teamscale server should be shown.
        ///
        /// The three cases are modeled as follows:
        /// - no upload: Config == null
        /// - upload to Teamscale: Config != null && Config.Teamscale != null
        /// - upload to directory: Config != null && Config.Teamcsale == null
        /// </summary>
        public int UploadTypeIndex
        {
            get
            {
                if (Config == null)
                {
                    return 0;
                }
                else if (Config.Teamscale == null)
                {
                    return 1;
                }
                return 2;
            }
            set
            {
                switch (value)
                {
                    case 0:
                        Config = null;
                        break;

                    case 1:
                        if (Config == null)
                        {
                            Config = new UploadDaemon.Config();
                        }
                        Config.Teamscale = new UploadDaemon.TeamscaleServer();
                        break;

                    case 2:
                        if (Config == null)
                        {
                            Config = new UploadDaemon.Config();
                        }
                        Config.Teamscale = null;
                        break;
                }

                validationWasRun = false;

                // force all UI elements to be refreshed since this affects almost all of them
                PropertyChanged.Raise(this, null);
            }
        }

        /// <summary>
        /// Whether the configuration UI for the Teamscale server should be shown.
        /// </summary>
        public bool IsTeamscaleConfigVisible { get => Config != null && Config.Teamscale != null; }

        /// <summary>
        /// Whether the configuration UI for the directory to copy to should be shown.
        /// </summary>
        public bool IsDirectoryConfigVisible { get => Config != null && Config.Teamscale == null; }

        private string internalValidationErrorMessage = string.Empty;

        /// <summary>
        /// The general error message.
        /// </summary>
        public string ErrorMessage
        {
            get => internalValidationErrorMessage;
            set
            {
                internalValidationErrorMessage = value;
                PropertyChanged.Raise(this);
            }
        }

        private string connectionErrorMessage = null;
        private bool validationWasRun = false;

        /// <summary>
        /// The validation message to show next to the validate button.
        /// </summary>
        public String ValidationMessage
        {
            get
            {
                if (!validationWasRun)
                {
                    return string.Empty;
                }
                if (connectionErrorMessage == null)
                {
                    return "Connected successfully";
                }
                return $"Failed to connect: {connectionErrorMessage}";
            }
        }

        /// <summary>
        /// The color of the validation message.
        /// </summary>
        public Brush ValidationMessageColor
        {
            get
            {
                if (connectionErrorMessage == null)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                return new SolidColorBrush(Colors.Red);
            }
        }

        public UploadViewModel()
        {
            ReadConfigFromDisk();
        }

        private void ReadConfigFromDisk()
        {
            try
            {
                Config = JsonConvert.DeserializeObject<UploadDaemon.Config>(File.ReadAllText(ConfigFilePath));
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to load configuration from {uploadConfigPath}", ConfigFilePath);
                ErrorMessage = $"Failed to load configuration from {ConfigFilePath}:\n{e.GetType()}: {e.Message}";
            }
        }

        public async void ValidateTeamscale()
        {
            // TODO network request
            PropertyChanged.Raise(this, nameof(ValidationMessage));
            PropertyChanged.Raise(this, nameof(ValidationMessageColor));
        }

        public void RestoreOriginalConfig()
        {
            ReadConfigFromDisk();
        }

        public void SaveConfig()
        {
            try
            {
                File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(Config));
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to save configuration to {uploadConfigPath}", ConfigFilePath);
                ErrorMessage = $"Failed to save configuration to {ConfigFilePath}:\n{e.GetType()}: {e.Message}";
            }
        }
    }
}
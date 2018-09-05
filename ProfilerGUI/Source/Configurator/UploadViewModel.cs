using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        /// <summary>
        /// <inheritDoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The config for the daemon.
        /// </summary>
        public UploadDaemon.Config Config { get; private set; }

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

        public UploadViewModel(UploadDaemon.Config config)
        {
            this.Config = config;
        }

        public void ValidateTeamscale()
        {
            // TODO
            PropertyChanged.Raise(this, nameof(ValidationMessage));
        }
    }
}
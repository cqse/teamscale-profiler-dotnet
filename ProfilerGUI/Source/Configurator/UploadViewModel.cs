using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using UploadDaemon.Upload;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// View model for configuring the uploader daemon.
    /// </summary>
    internal class UploadViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly string ConfigFilePath = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, "UploadDaemon", UploadDaemon.Config.ConfigFileName);

        private readonly HttpClient client = new HttpClient();

        /// <summary>
        /// <inheritDoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The config for the daemon.
        /// </summary>
        public UploadDaemon.Config Config { get; private set; } = null;

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

        private ValidationResult internalValidationResult = null;

        /// <summary>
        /// The general error message.
        /// </summary>
        public ValidationResult ValidationResult
        {
            get => internalValidationResult;
            set
            {
                internalValidationResult = value;
                PropertyChanged.Raise(this);
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
                ShowErrorMessage($"Failed to load configuration from {ConfigFilePath}:\n{e.GetType()}: {e.Message}");
            }
        }

        private void ShowErrorMessage(String message)
        {
            ValidationResult = new ValidationResult(false, message);
        }

        private async Task<bool> Validate()
        {
            IEnumerable<string> errors = Config.Validate();
            if (errors.Any())
            {
                ShowErrorMessage(string.Join("\r\n", errors));
                return false;
            }

            return await CheckTeamscaleConnection();
        }

        /// <summary>
        /// Checks if the connection to Teamscale can be established.
        /// Show an error message in the UI if not.
        /// </summary>
        public async Task<bool> CheckTeamscaleConnection()
        {
            try
            {
                HttpClientUtils.SetUpBasicAuthentication(client, Config.Teamscale);

                string url = $"{Config.Teamscale.Url}/p/{Config.Teamscale.Project}/baselines";
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Failed to connect to {teamscale}. HTTP status code: {statusCode}\n{responseBody}",
                            Config.Teamscale, response.StatusCode, body);
                        ShowErrorMessage($"Failed to connect to {Config.Teamscale}. HTTP status code: {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to connect to {teamscale}", Config.Teamscale);
                ShowErrorMessage($"Failed to connect to {Config.Teamscale}. {e.GetType()} {e.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Rereads the config as it is on disk currently.
        /// </summary>
        public void RestoreOriginalConfig()
        {
            ReadConfigFromDisk();
        }

        /// <summary>
        /// Validates the config and saves it if it is valid.
        /// </summary>
        public async void SaveConfig()
        {
            bool isValid = await Validate();
            if (!isValid)
            {
                return;
            }

            try
            {
                File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(Config));
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to save configuration to {uploadConfigPath}", ConfigFilePath);
                ShowErrorMessage($"Failed to save configuration to {ConfigFilePath}:\n{e.GetType()}: {e.Message}");
            }
        }
    }
}
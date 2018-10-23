﻿using Common;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// View model for configuring the uploader daemon.
    /// </summary>
    internal class UploadViewModel : INotifyPropertyChanged
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly HttpClient client = new HttpClient();

        /// <summary>
        /// <inheritDoc />
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The config for the daemon.
        /// </summary>
        public UploadConfig Config { get; private set; } = null;

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
                if (Config.Teamscale != null)
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
                            Config = new UploadConfig();
                        }
                        Config.Teamscale = new TeamscaleServer();
                        Config.Directory = null;
                        break;

                    case 2:
                        if (Config == null)
                        {
                            Config = new UploadConfig();
                        }
                        Config.Teamscale = null;
                        Config.Directory = string.Empty;
                        break;
                }

                // force all UI elements to be refreshed since this affects almost all of them
                UiUtils.Raise(PropertyChanged, this, null);
            }
        }

        /// <summary>
        /// The directory to upload traces to.
        /// </summary>
        public string Directory
        {
            get => Config?.Directory;
            set
            {
                Config.Directory = value;
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        /// <summary>
        /// Whether the configuration UI for the Teamscale server should be shown.
        /// </summary>
        public bool IsTeamscaleConfigVisible => Config != null && Config.Teamscale != null;

        /// <summary>
        /// Whether the configuration UI for the directory to copy to should be shown.
        /// </summary>
        public bool IsDirectoryConfigVisible => Config != null && Config.Teamscale == null;

        /// <summary>
        /// Whether the configuration UI for the version assembly should be shown.
        /// </summary>
        public bool IsVersionAssemblyVisible => Config != null;

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
                UiUtils.Raise(PropertyChanged, this);
            }
        }

        public UploadViewModel(UploadConfig config)
        {
            Config = config;
        }

        private void ShowErrorMessage(String message)
        {
            ValidationResult = new ValidationResult(false, message);
        }

        /// <summary>
        /// Checks if current config is valid.
        /// Shows an error message in the UI if not.
        /// </summary>
        public async Task<bool> Validate()
        {
            IEnumerable<string> errors = Config?.Validate();
            if (errors != null && errors.Any())
            {
                ShowErrorMessage(string.Join("\r\n", errors));
                return false;
            }

            return await CheckTeamscaleConnection();
        }

        /// <summary>
        /// Checks if the connection to Teamscale can be established.
        /// Shows an error message in the UI if not.
        /// </summary>
        private async Task<bool> CheckTeamscaleConnection()
        {
            if (Config?.Teamscale == null)
            {
                return true;
            }

            try
            {
                HttpClientUtils.SetUpBasicAuthentication(client, Config.Teamscale);

                string url = $"{Config.Teamscale.Url}/p/{Config.Teamscale.Project}/baselines";
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        string body = await response.Content.ReadAsStringAsync();
                        logger.Error("Failed to connect to {teamscale}. HTTP status: {statusCode}\n{responseBody}",
                            Config.Teamscale, response.StatusCode, body);
                        ShowErrorMessage($"Failed to connect to {Config.Teamscale}. HTTP status: {response.StatusCode}");
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

            ValidationResult = new ValidationResult(true, "Connected successfully");
            return true;
        }
    }
}
using NLog;
using NLog.Config;
using NLog.Targets.Wrappers;
using ProfilerGUI.Source.Configurator;
using System;
using System.Windows;
using System.Windows.Forms;

namespace ProfilerGUI
{
    /// <summary>
    /// Interaction logic for UploadConfigWindow.xaml
    ///
    /// This window returns a DialogResult indicating whether the user wants to accept the
    /// changes to the upload config or not.
    /// </summary>
    public partial class UploadConfigWindow : Window
    {
        private readonly UploadViewModel ViewModel;

        /// <summary>
        /// The config the user edited.
        /// </summary>
        public UploadDaemon.Config Config { get => ViewModel.Config; }

        public UploadConfigWindow(UploadDaemon.Config config)
        {
            InitializeComponent();
            ViewModel = new UploadViewModel(config?.Clone());
            this.DataContext = ViewModel;
        }

        private void OnValidateClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.CheckTeamscaleConnection();
        }

        private async void OnOk(object sender, RoutedEventArgs e)
        {
            bool isValid = await ViewModel.Validate();
            if (!isValid)
            {
                return;
            }

            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnSelectDirectory(object sender, RoutedEventArgs e)
        {
            WpfUtils.OpenFolderChooser(path => ViewModel.Directory = path);
        }
    }
}
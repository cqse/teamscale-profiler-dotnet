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
    /// </summary>
    public partial class UploadConfigWindow : Window
    {
        private readonly UploadViewModel ViewModel;

        public UploadDaemon.Config Config { get => ViewModel.Config; }

        public UploadConfigWindow()
        {
            InitializeComponent();
            ViewModel = new UploadViewModel();
            this.DataContext = ViewModel;
        }

        private void OnValidateClicked(object sender, RoutedEventArgs e)
        {
            ViewModel.ValidateTeamscale();
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            ViewModel.SaveConfig();
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            ViewModel.RestoreOriginalConfig();
            Close();
        }
    }
}
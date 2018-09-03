using ProfilerGUI.Source.Configurator;
using System;
using System.Windows;
using System.Windows.Forms;

namespace ProfilerGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel ViewModel;

        public MainWindow(bool launchTargetAppDirectly)
        {
            InitializeComponent();
            ViewModel = new MainViewModel(launchTargetAppDirectly);
            this.DataContext = ViewModel;
        }

        private void OnSaveConfigurationButtonClick(object sender, EventArgs args)
        {
            ViewModel.SaveConfiguration();
        }

        private void OnChooseTargetDirButtonClick(object sender, EventArgs args)
        {
            OpenFolderChooser(selectedPath => ViewModel.TargetTraceDirectory = selectedPath);
        }

        private void OpenFolderChooser(Action<string> pathSelectedAction)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    pathSelectedAction.Invoke(dialog.SelectedPath);
                }
            }
        }

        private void OnChooseApplicationButtonClick(object sender, EventArgs args)
        {
            using (var dialog = new OpenFileDialog
            {
                // This lets the user select lnk-files (shortcuts), which can be helpful in our context
                DereferenceLinks = false,
                Filter = "Applications or shortcuts|*.lnk;*.exe;*.dll)|All files|*.*"
            })
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.ClearTargetAppFields();
                    ViewModel.TargetApplicationPath = dialog.FileName;
                }
            }
        }

        private void OnRunProfiledAppButtonClick(object sender, EventArgs args)
        {
            ViewModel.RunProfiledApplication();
        }

        private void OnTargetAppArgumentsChange(object sender, EventArgs args)
        {
            ViewModel.TargetAppArguments = (sender as System.Windows.Controls.TextBox).Text;
        }

        private void OnTargetAppStartInChange(object sender, EventArgs args)
        {
            ViewModel.TargetAppWorkingDirectory = (sender as System.Windows.Controls.TextBox).Text;
        }
    }
}
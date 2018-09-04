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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel ViewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="launchTargetAppDirectly">If this is true, the target application will be started right away</param>
        public MainWindow(bool launchTargetAppDirectly)
        {
            InitializeComponent();
            ViewModel = new MainViewModel(launchTargetAppDirectly);
            this.DataContext = ViewModel;
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (true)
            {
                return;
            }
            Dispatcher.Invoke(() =>
            {
                var target = new Helper.WpfRichTextBoxTarget
                {
                    Name = "RichText",
                    Layout =
                        "[${longdate:useUTC=false}] :: [${level:uppercase=true}] :: ${logger}:${callsite} :: ${message} ${exception:innerFormat=tostring:maxInnerExceptionLevel=10:separator=,:format=tostring}",
                    ControlName = LogBox.Name,
                    FormName = GetType().Name,
                    AutoScroll = true,
                    MaxLines = 100000,
                    UseDefaultRowColoringRules = true,
                };
                var asyncWrapper = new AsyncTargetWrapper { Name = "RichTextAsync", WrappedTarget = target };

                LogManager.Configuration.AddTarget(asyncWrapper.Name, asyncWrapper);
                LogManager.Configuration.LoggingRules.Insert(0, new LoggingRule("*", LogLevel.Info, asyncWrapper));
                LogManager.ReconfigExistingLoggers();
            });
        }

        private void OnSaveConfigurationButtonClick(object sender, EventArgs args)
        {
            ViewModel.SaveConfiguration();
        }

        private void OnChooseTargetDirButtonClick(object sender, EventArgs args)
        {
            OpenFolderChooser(selectedPath => ViewModel.TargetApp.TraceDirectory = selectedPath);
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
                Filter = "Applications or shortcuts|*.lnk;*.exe;*.dll|All files|*.*"
            })
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    ViewModel.ClearTargetAppFields();
                    ViewModel.TargetApp.ApplicationPath = dialog.FileName;
                }
            }
        }

        private void OnRunProfiledAppButtonClick(object sender, EventArgs args)
        {
            ViewModel.RunProfiledApplication();
        }
    }
}
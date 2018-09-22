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
        private MainViewModel ViewModel => DataContext as MainViewModel;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="launchTargetAppDirectly">If this is true, the target application will be started right away</param>
        public MainWindow(bool launchTargetAppDirectly)
        {
            InitializeComponent();
            DataContext = new MainViewModel(launchTargetAppDirectly);
        }

        private void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(ConfigureLogBox);
        }

        private void ConfigureLogBox()
        {
            var target = new WpfRichTextTarget(LogBox)
            {
                Name = "RichText",
                Layout = "[${level:uppercase=true}] ${message} (${logger})${onexception:inner=${newline}${exception:format=ShortType,Message:innerFormat=ShortType,Message:maxInnerExceptionLevel=10}}${newline}",
            };
            var asyncWrapper = new AsyncTargetWrapper { Name = "RichTextAsync", WrappedTarget = target };

            LogManager.Configuration.AddTarget(asyncWrapper.Name, asyncWrapper);
            LogManager.Configuration.LoggingRules.Insert(0, new LoggingRule("*", LogLevel.Info, asyncWrapper));
            LogManager.ReconfigExistingLoggers();
        }

        private void OnSaveConfigurationButtonClick(object sender, EventArgs args)
        {
            ViewModel.SaveConfiguration();
        }

        private void OnChooseTargetDirButtonClick(object sender, EventArgs args)
        {
            WpfUtils.OpenFolderChooser(selectedPath => ViewModel.TargetApp.TraceDirectory = selectedPath);
        }

        private void OnChooseApplicationButtonClick(object sender, EventArgs args)
        {
            using (OpenFileDialog dialog = new OpenFileDialog
            {
                // This lets the user select lnk-files (shortcuts) instead of resolving them to their target exe files
                // this allows us the parse the contents of the link files and extract working dir, arguments, ...
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

        private void OnChangeUpload(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenUploadConfigDialog();
        }
    }
}
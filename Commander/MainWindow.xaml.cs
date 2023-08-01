using Cqse.Teamscale.Profiler.Commons.Ipc;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Windows;

namespace Cqse.Teamscale.Profiler.Commander
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowVM viewModel = new MainWindowVM();
        private readonly App app;
        private long startTimestamp = 0;

        public MainWindow()
        {
            app = Application.Current as App;
            DataContext = viewModel;
            viewModel.ButtonText = "Start Test";
            viewModel.IsStopped = true;
            InitializeComponent();
            string pattern = ConfigurationManager.AppSettings["testNamePattern"];
            if (pattern != null)
            {
                viewModel.TestNamePattern = new Regex(pattern);
                TestName.ToolTip = $"Test name must match: {pattern}";
            }
        }

        private void OnStartClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = false;
            app.StartTest(viewModel.TestName);
            startTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1000;
        }

        private void OnPassedClicked(object sender, RoutedEventArgs e)
        {
            ShowDurationDialog(TestExecutionResult.Passed);
        }

        private void OnFailureClicked(object sender, RoutedEventArgs e)
        {
            ShowDurationDialog(TestExecutionResult.Failure);
        }

        private void OnSkippedClicked(object sender, RoutedEventArgs e)
        {
            ShowDurationDialog(TestExecutionResult.Skipped);
        }

        private void ShowDurationDialog(TestExecutionResult result)
        {
            long endTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() * 1000;
            long duration = endTimestamp - startTimestamp;
            if (new TestDurationDialog(duration, result).ShowDialog() == true)
            {
                viewModel.IsStopped = true;
            }
        }
    }
}

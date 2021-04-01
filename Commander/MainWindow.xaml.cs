using Cqse.Teamscale.Profiler.Commons.Ipc;
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
        private MainWindowVM viewModel = new MainWindowVM();
        private App app;

        public MainWindow()
        {
            app = Application.Current as App;
            this.DataContext = viewModel;
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
        }

        private void OnPassedClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = true;
            viewModel.TestName = "";
            app.EndTest(ETestExecutionResult.PASSED);
        }

        private void OnFailureClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = true;
            app.EndTest(ETestExecutionResult.FAILURE);
        }

        private void OnSkippedClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = true;
            app.EndTest(ETestExecutionResult.SKIPPED);
        }
    }
}

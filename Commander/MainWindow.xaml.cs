using Cqse.Teamscale.Profiler.Commons.Ipc;
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
        }

        private void OnStartClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = false;
            app.StartTest(viewModel.TestName);
        }

        private void OnPassedClicked(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = true;
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

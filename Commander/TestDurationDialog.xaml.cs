using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Windows;

namespace Cqse.Teamscale.Profiler.Commander
{
    /// <summary>
    /// Interaction logic for TestDurationDialog.xaml
    /// </summary>
    public partial class TestDurationDialog : Window
    {

        private readonly App app;
        private readonly TestDurationDialogVM viewModel;
        private readonly TestExecutionResult result;

        public TestDurationDialog(long durationMs, TestExecutionResult result)
        {
            this.result = result;
            app = Application.Current as App;
            viewModel = new TestDurationDialogVM(durationMs);
            DataContext = viewModel;
            InitializeComponent();
        }

        private void SaveClicked(object sender, RoutedEventArgs e)
        {
            long? duration = viewModel.DurationMs;
            if (duration != null)
            {
                app.EndTest(result, "", duration.Value);
                DialogResult = true;
                Close();
            }
        }
    }
}

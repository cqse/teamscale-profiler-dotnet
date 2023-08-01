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

        public TestDurationDialog(long durationMs)
        {
            app = Application.Current as App;
            viewModel = new TestDurationDialogVM(durationMs);
            DataContext = viewModel;
            InitializeComponent();
        }
    }
}

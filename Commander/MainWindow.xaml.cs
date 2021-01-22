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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            viewModel.IsStopped = !viewModel.IsStopped;

            if (viewModel.IsStopped)
            {
                app.StopProfiling();
                viewModel.ButtonText = "Start Test";
            }
            else
            {
                app.StartProfiling(viewModel.TestName);
                viewModel.ButtonText = "Stop Test";
            }
        }
    }
}

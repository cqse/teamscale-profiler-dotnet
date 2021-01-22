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
            viewModel.StatusText = "Collecting coverage";
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!viewModel.EnableProfiling)
            {
                app.StopProfiling();
                viewModel.StatusText = "Stopped collecting coverage";
            }
            else if (!viewModel.TestWiseProfiling)
            {
                app.StartProfiling();
                viewModel.StatusText = "Collecting coverage";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(viewModel.TestName))
                {
                    viewModel.StatusText = "Please specify a test name";
                }
                else
                {
                    app.StartProfiling(viewModel.TestName);
                    viewModel.StatusText = "Collecting coverage for test " + viewModel.TestName;
                }
            }

            viewModel.NeedsUpdate = false;
        }
    }
}

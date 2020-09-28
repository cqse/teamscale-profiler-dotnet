using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProfilerGui
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

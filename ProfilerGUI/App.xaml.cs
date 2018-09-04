using System.Linq;
using System.Windows;

namespace ProfilerGUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string LaunchAppDirectlyArg = "--launchTargetApp";

        private void OnApplicationStart(object sender, StartupEventArgs e)
        {
            bool launchTargetApp = e.Args != null && e.Args.Any(argument => argument == LaunchAppDirectlyArg);
            new MainWindow(launchTargetApp).Show();
        }
    }
}
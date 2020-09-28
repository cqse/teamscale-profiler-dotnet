using System.Windows;
using ZeroMQ;

namespace ProfilerGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string Endpoint = "tcp://127.0.0.1:5555";
        private ZContext context;
        private ZSocket socket;

        protected override void OnStartup(StartupEventArgs e)
        {
            context = new ZContext();
            socket = new ZSocket(context, ZSocketType.PUB);
            socket.Bind(Endpoint);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            socket.Dispose();
            context.Dispose();
        }

        internal void StopProfiling()
        {
            using (var frame = new ZFrame("profilerStop"))
            {
                socket.Send(frame);
            }
        }

        internal void StartProfiling(string testName = null)
        {
            string message = "profilerStart";
            if (!string.IsNullOrWhiteSpace(testName))
            {
                message += " " + testName;
            }

            using (var frame = new ZFrame(message))
            {
                socket.Send(frame);
            }
        }
    }
}

using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Windows;

namespace Cqse.Teamscale.Profiler.Commander
{
    /// <summary>
    /// Profiler GUI application.
    /// </summary>
    public partial class App : Application
    {
        private ProfilerIpc profilerIpc;

        protected override void OnStartup(StartupEventArgs e)
        {
            profilerIpc = new ProfilerIpc(new IpcConfig());
        }

        protected override void OnExit(ExitEventArgs e)
        {
            profilerIpc.Dispose();
        }

        internal void StopProfiling()
        {
            profilerIpc.TestName = null;
            //using (var frame = new ZFrame("profilerStop"))
            //{
            //    socket.Send(frame);
            //}
        }

        internal void StartProfiling(string testName = null)
        {
            profilerIpc.TestName = testName;
            //string message = "profilerStart";
            //if (!string.IsNullOrWhiteSpace(testName))
            //{
            //    message += " " + testName;
            //}

            //using (var frame = new ZFrame(message))
            //{
            //    socket.Send(frame);
            //}
        }
    }
}

using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Configuration;
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
            string publishSocket = ConfigurationManager.AppSettings["publishSocket"];
            string requestSocket = ConfigurationManager.AppSettings["requestSocket"];
            profilerIpc = new ProfilerIpc(new IpcConfig(publishSocket, requestSocket));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            profilerIpc.Dispose();
        }

        internal void EndTest(ETestExecutionResult result)
        {
            profilerIpc.EndTest(result);
        }

        internal void StartTest(string testName = null)
        {
            profilerIpc.StartTest(testName);
        }
    }
}

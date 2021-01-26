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

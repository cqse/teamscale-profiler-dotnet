using System;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ProfilerIpc : IDisposable
    {
        private readonly IpcServer ipcServer;

        private string testName = string.Empty;
        public string TestName => testName;

        public IpcConfig Config { get; }

        public ProfilerIpc(IpcConfig config)
        {
            Config = config;
            this.ipcServer = CreateIpcServer(config);
        }

        protected virtual IpcServer CreateIpcServer(IpcConfig config)
        {
            return new ZmqIpcServer(config, this.HandleRequest);
        }

        protected virtual string HandleRequest(string message)
        {
            switch (message)
            {
                case "get_testname":
                    return testName;

                default:
                    return string.Empty;
            }
        }

        public void StartTest(string testName)
        {
            this.testName = testName;
            ipcServer.Publish("test:start", testName);
        }

        public void EndTest(ETestExecutionResult result, string message = "")
        {
            this.testName = string.Empty;
            ipcServer.Publish("test:end", Enum.GetName(typeof(ETestExecutionResult), result), message);
        }

        public void Dispose()
        {
            this.ipcServer.Dispose();
        }
    }
}

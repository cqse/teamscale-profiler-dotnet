using System;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ProfilerIpc : IDisposable
    {
        private readonly IpcServer ipcServer;
        private string testName = string.Empty;

        public IpcConfig Config { get; }

        public string TestName
        {
            get => testName;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    testName = string.Empty;
                }
                else
                {
                    testName = value;
                }
            }
        }

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

        public void Dispose()
        {
            this.ipcServer.Dispose();
        }
    }
}

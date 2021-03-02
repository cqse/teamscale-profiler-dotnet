using NLog;
using System;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ProfilerIpc : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
            logger.Info("Starting IPC server (PUB={pub}, REQ={req})", config.PublishSocket, config.RequestSocket);
            return new ZmqIpcServer(config, this.HandleRequest);
        }

        protected virtual string HandleRequest(string message)
        {
            switch (message)
            {
                case "get_testname":
                    logger.Info("Received request get_testname. Response {testName}", testName);
                    return testName;
                default:
                    logger.Info("Received request: {request}", message);
                    return string.Empty;
            }
        }

        public void StartTest(string testName)
        {
            logger.Info("Broadcasting start of test {testName}", testName);
            this.testName = testName;
            ipcServer.Publish("test:start", testName);
        }

        public void EndTest(ETestExecutionResult result, string message = "")
        {
            logger.Info("Broadcasting end of test {testName} with result {result}", testName, result);
            this.testName = string.Empty;
            ipcServer.Publish("test:end", Enum.GetName(typeof(ETestExecutionResult), result), message);
        }

        public void Dispose()
        {
            logger.Info("Shutting down IPC server");
            this.ipcServer.Dispose();
        }
    }
}

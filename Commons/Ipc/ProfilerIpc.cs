using NLog;
using System;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ProfilerIpc : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IpcServer ipcServer;

        // TODO (MP) We need to define a contract for test names. Is empty allowed? May it be null?
        public string TestName { get; private set; } = string.Empty;

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
                    logger.Info("Received request get_testname. Response {testName}", TestName);
                    return TestName;
                default:
                    logger.Info("Received request: {request}", message);
                    return string.Empty;
            }
        }

        public void StartTest(string testName)
        {
            logger.Info("Broadcasting start of test {testName}", testName);
            this.TestName = testName;
            ipcServer.Publish("test:start", testName);
        }

        public void EndTest(TestExecutionResult result, string message = "")
        {
            logger.Info("Broadcasting end of test {testName} with result {result}", TestName, result);
            this.TestName = string.Empty;
            ipcServer.Publish("test:end", Enum.GetName(typeof(TestExecutionResult), result).ToUpper(), message);
        }

        public void Dispose()
        {
            logger.Info("Shutting down IPC server");
            this.ipcServer.Dispose();
        }
    }
}

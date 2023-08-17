using NLog;
using System;
using System.Text.RegularExpressions;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ProfilerIpc : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ZmqIpcServer ipcServer;

        /// <summary>
        /// The name of the current test, empty string if no test is running.
        /// </summary>
        public string TestName { get; private set; } = String.Empty;

        public IpcConfig Config { get; }

        public ProfilerIpc(IpcConfig config)
        {
            Config = config;
            this.ipcServer = CreateIpcServer(config);
        }

        protected virtual ZmqIpcServer CreateIpcServer(IpcConfig config)
        {
            logger.Info("Starting IPC server (PUB={pub}, REQ={req})", config.PublishSocket, config.RequestSocket);
            return new ZmqIpcServer(config, this.HandleRequest);
        }

        protected virtual string HandleRequest(string message)
        {
            switch (message)
            {
                case "testname":
                    logger.Info("Received request get_testname. Response {testName}", TestName);
                    if (TestName == string.Empty)
                    {
                        return string.Empty;
                    }
                    return $"start:{TestName}";
                case string profiler when new Regex("r:").IsMatch(profiler):
                    logger.Info("Registered");
                    return "registered";
                default:
                    logger.Info("Received request: {request}.", message);
                    return string.Empty;
            }
        }

        public void StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new ArgumentException("Test name must not be empty or null");
            }

            logger.Info("Broadcasting start of test {testName}", testName);
            this.TestName = testName;
            ipcServer.SendTestEvent($"start:{testName}");
        }

        public void EndTest(TestExecutionResult result)
        {
            logger.Info("Broadcasting end of test {testName} with result {result}", TestName, result);
            this.TestName = string.Empty;
            ipcServer.SendTestEvent($"end:{Enum.GetName(typeof(TestExecutionResult), result).ToUpper()}");
        }

        public void Dispose()
        {
            logger.Info("Shutting down IPC server");
            this.ipcServer.Dispose();
        }
    }
}

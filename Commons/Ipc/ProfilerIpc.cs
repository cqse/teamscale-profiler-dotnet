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
                    logger.Info("Received request get_testname. Response {testName}", this.TestName);
                    if (this.TestName == string.Empty)
                    {
                        return string.Empty;
                    }
                    return $"start:{this.TestName}";
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
            if (this.TestName != string.Empty)
            {
                logger.Info("Starting a new test while a test is still active. Ending active Test with result Skipped since actual result is unknown.");
                this.EndTest(TestExecutionResult.Skipped);
            }
            String cleanedTestName = testName.Replace("\n", " ").Replace("\r", " "); ;
            logger.Info("Broadcasting start of test {testName}", cleanedTestName);
            this.TestName = cleanedTestName;
            ipcServer.SendTestEvent($"start:{cleanedTestName}");
        }

        public void EndTest(TestExecutionResult result)
        {
            if (TestName == string.Empty)
            {
                logger.Info("Testname is empty. Result {result} cannot be associated with a testname and is not broadcasted.", TestName, result);
                return;
            }
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

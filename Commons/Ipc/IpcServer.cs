using NLog;
using System;
using System.Threading;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public abstract class IpcServer : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate string RequestHandler(string message);

        protected readonly IpcConfig config;
        protected readonly RequestHandler requestHandler;
        private readonly Thread requestHandlerThread;

        public IpcServer(IpcConfig config, RequestHandler requestHandler, bool createThread = false)
        {
            this.config = config;
            this.requestHandler = requestHandler;

            if (createThread)
            {
                logger.Debug("Starting request handler on background thread");
                this.requestHandlerThread = this.StartRequestHandlerThread();
            }
            else
            {
                logger.Debug("Starting request handler on this thread");
                this.StartRequestHandler();
            }

            StartPublisher();
        }

        private Thread StartRequestHandlerThread()
        {
            var thread = new Thread(StartRequestHandler)
            {
                IsBackground = true
            };
            thread.Start();

            return thread;
        }

        protected abstract void StartRequestHandler();

        protected abstract void StartPublisher();

        public abstract void Publish(params string[] frames);

        public virtual void Dispose()
        {
            if (requestHandlerThread?.IsAlive == true)
            {
                requestHandlerThread.Abort();
            }
        }
    }
}

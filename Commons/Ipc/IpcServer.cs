using System;
using System.Threading;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public abstract class IpcServer : IDisposable
    {
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
                this.requestHandlerThread = this.StartRequestHandlerThread();
            }
            else
            {
                this.StartRequestHandler();
            }         

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

        public virtual void Dispose()
        {
            if (requestHandlerThread?.IsAlive == true)
            {
                requestHandlerThread.Abort();
            }
        }
    }
}

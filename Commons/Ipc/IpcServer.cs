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

        public IpcServer(IpcConfig config, RequestHandler requestHandler)
        {
            this.config = config;
            this.requestHandler = requestHandler;
            this.requestHandlerThread = new Thread(StartRequestHandler)
            {
                IsBackground = true
            };
            this.requestHandlerThread.Start();
        }

        protected abstract void StartRequestHandler();

        public void Dispose()
        {
            requestHandlerThread.Abort();
        }
    }
}

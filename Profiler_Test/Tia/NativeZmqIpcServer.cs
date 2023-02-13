using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Linq;
using System.Threading;
using ZeroMQ;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    public class NativeZmqIpcServer : IpcServer
    {
        private readonly ZContext context = new ZContext();
        private ZSocket publishSocket;
        private bool disposed = false;

        public NativeZmqIpcServer(IpcConfig config, RequestHandler requestHandler) : base(config, requestHandler, true)
        {
            // delegate to base class
        }

        override protected void StartRequestHandler()
        {
            using (var responseSocket = new ZSocket(this.context, ZSocketType.REP))
            {
                responseSocket.Bind(this.config.RequestSocket);
                while (!disposed)
                {
                    using (ZFrame request = responseSocket.ReceiveFrame())
                    {
                        string response = this.requestHandler(request.ReadString());
                        responseSocket.Send(new ZFrame(response));
                    }
                }
            }
        }

        override protected void StartPublisher()
        {
            publishSocket = new ZSocket(this.context, ZSocketType.PUB);
            publishSocket.Bind(this.config.PublishSocket);
        }

        public override void Publish(params string[] frames)
        {
            using (var msg = new ZMessage(frames.Select(frame => new ZFrame(frame))))
            {
                publishSocket.Send(msg);
            }
        }

        public override void Dispose()
        {
            disposed = true;
            publishSocket.Dispose();
            base.Dispose();
            this.context.Dispose();
        }
    }
}

using NetMQ;
using NetMQ.Sockets;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ZmqIpcServer : IpcServer
    {
        private NetMQPoller poller;
        private PublisherSocket publishSocket;
        private ResponseSocket responseSocket;

        public ZmqIpcServer(IpcConfig config, RequestHandler requestHandler) : base(config, requestHandler, false)
        {
            // delegate to base class
        }

        override protected void StartRequestHandler()
        {
            this.responseSocket = new ResponseSocket();
            this.responseSocket.Bind(this.config.RequestSocket);
            this.responseSocket.ReceiveReady += (s, e) =>
            {
                string message = responseSocket.ReceiveFrameString();
                string response = this.requestHandler(message);
                responseSocket.SendFrame(response);
            };

            this.poller = new NetMQPoller { responseSocket };
            poller.RunAsync("Profiler IPC", true);
        }

        override protected void StartPublisher()
        {
            this.publishSocket = new PublisherSocket();
            this.publishSocket.Bind(this.config.PublishSocket);
        }

        public override void Publish(params string[] frames)
        {
            this.publishSocket.SendMultipartMessage(new NetMQMessage(frames.Select(frame => new NetMQFrame(frame))));
        }

        public override void Dispose()
        {
            this.poller?.Dispose();
            this.responseSocket?.Dispose();
            this.publishSocket?.Dispose();
            NetMQConfig.Cleanup();
            base.Dispose();
        }
    }
}

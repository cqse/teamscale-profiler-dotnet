using NetMQ;
using NetMQ.Sockets;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ZmqIpcServer : IpcServer
    {
        private NetMQPoller poller;
        private PublisherSocket publishSocket;

        public ZmqIpcServer(IpcConfig config, RequestHandler requestHandler) : base(config, requestHandler, true)
        {
            // delegate to base class
        }

        override protected void StartRequestHandler()
        {
            using (var responseSocket = new ResponseSocket())
            {
                responseSocket.Bind(this.config.RequestSocket);
                using (poller = new NetMQPoller { responseSocket })
                {
                    responseSocket.ReceiveReady += (s, e) =>
                    {
                        string message = responseSocket.ReceiveFrameString();
                        string response = this.requestHandler(message);
                        responseSocket.SendFrame(response);
                    };

                    poller.Run();
                }
            }
        }

        override protected void StartPublisher()
        {
            publishSocket = new PublisherSocket();
            publishSocket.Bind(this.config.PublishSocket);
        }

        public override void Publish(string testName)
        {
            publishSocket.SendFrame(testName);
        }

        public override void Dispose()
        {
            publishSocket.Dispose();
            poller.Stop();
            NetMQConfig.Cleanup();
            base.Dispose();
        }
    }
}

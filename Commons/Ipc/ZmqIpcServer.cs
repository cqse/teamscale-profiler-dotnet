using NetMQ;
using NetMQ.Sockets;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ZmqIpcServer : IpcServer
    {
        public ZmqIpcServer(IpcConfig config, RequestHandler requestHandler) : base(config, requestHandler)
        {
            // delegate to base class
        }

        override protected void StartRequestHandler()
        {
            using (var responseSocket = new ResponseSocket())
            {
                responseSocket.Bind(this.config.RequestSocket);
                while (true)
                {
                    string message = responseSocket.ReceiveFrameString();
                    string response = this.requestHandler(message);
                    responseSocket.SendFrame(response);
                }
            }
        }
    }
}

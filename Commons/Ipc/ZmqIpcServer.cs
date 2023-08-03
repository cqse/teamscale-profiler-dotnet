using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    public class ZmqIpcServer : IDisposable
    {
        private const string REGISTER_CLIENT = "register";
        private NetMQPoller? poller;
        private ResponseSocket? responseSocket;

        private Dictionary<string, RequestSocket> clients = new Dictionary<string, RequestSocket>();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate string RequestHandler(string message);

        protected readonly IpcConfig config;
        protected readonly RequestHandler requestHandler;

        private int portOffset = 0;

        public ZmqIpcServer(IpcConfig config, RequestHandler requestHandler)
        {
            this.config = config;
            this.requestHandler = requestHandler;

            StartRequestHandler();
        }

        protected void StartRequestHandler()
        {
            this.responseSocket = new ResponseSocket();
            this.responseSocket.Bind(this.config.RequestSocket + ":" + config.StartPortNumber);
            this.responseSocket.ReceiveReady += (s, e) =>
            {
                string message = responseSocket.ReceiveFrameString();
                if (message.StartsWith(REGISTER_CLIENT))
                {
                    portOffset++;
                    RequestSocket clientRequestSocket = new RequestSocket();
                    string clientAddress = config.RequestSocket + ":" + (config.StartPortNumber + portOffset);
                    clientRequestSocket.Connect(clientAddress);
                    lock(clients)
                    {
                        clients.Add(clientAddress, clientRequestSocket);
                    }
                    responseSocket.SendFrame(clientAddress);
                    return;
                }
                string response = this.requestHandler(message);
                responseSocket.SendFrame(response);
            };

            this.poller = new NetMQPoller { responseSocket };
            poller.RunAsync("Profiler IPC", true);
        }

        public void SendTestEvent(string testEvent)
        {
            HashSet<string> clientsToRemove = new HashSet<string>();
            System.Threading.Tasks.Parallel.ForEach(clients, entry =>
            {
                entry.Value.SendFrame(Encoding.UTF8.GetBytes(testEvent));
                if (entry.Value.TryReceiveFrameString(TimeSpan.FromSeconds(5.0), out string? response))
                {
                    Console.WriteLine(response);
                } else
                {
                    lock(clientsToRemove)
                    {
                        clientsToRemove.Add(entry.Key);
                    }
                    logger.Error($"Got no response from Profiler with Socket {entry.Key}");
                }
            });
            lock (clients)
            {
                foreach (var client in clientsToRemove)
                {
                    if (!clients.ContainsKey(client)) {
                        continue;
                    }
                    clients[client].Close();
                    clients.Remove(client);
                }
            }
        }

        public void Dispose()
        {
            this.poller?.Dispose();
            this.responseSocket?.Dispose();
            foreach (var client in clients)
            {
                client.Value.Dispose();
            }
            NetMQConfig.Cleanup();
        }
    }
}

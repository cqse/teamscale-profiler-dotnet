using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    /// <summary>
    /// .Net Profiler instances can connect to this server to receive test events for testwise coverage.
    /// </summary>
    public class ZmqIpcServer : IDisposable
    {
        private const string REGISTER_CLIENT = "register";
        private NetMQPoller? poller;
        private ResponseSocket? responseSocket;

        private Dictionary<int, Tuple<string, RequestSocket>> clients = new Dictionary<int, Tuple<string, RequestSocket>>();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate string RequestHandler(string message);

        private readonly IpcConfig config;
        private readonly RequestHandler requestHandler;

        private int portOffset = 0;

        public ZmqIpcServer(IpcConfig config, RequestHandler requestHandler)
        {
            this.config = config;
            this.requestHandler = requestHandler;

            StartRequestHandler();
        }

        /// <summary>
        /// Starts the zeromq request and response handler
        /// </summary>
        protected void StartRequestHandler()
        {
            this.responseSocket = new ResponseSocket();
            this.responseSocket.Bind(this.config.PublishSocket);
            this.responseSocket.ReceiveReady += (s, e) =>
            {
                string message = responseSocket.ReceiveFrameString();
                if (message.StartsWith(REGISTER_CLIENT))
                {
                    RegisterClient(message);
                    return;
                }
                string response = this.requestHandler(message);
                responseSocket.SendFrame(response);
            };

            this.poller = new NetMQPoller { responseSocket };
            poller.RunAsync("Profiler IPC", true);
        }

        private  void RegisterClient(string message)
        {
            int pid = Int32.Parse(message.Split(':')[1]);
            lock (clients)
            {
                string clientAddress;
                if (clients.ContainsKey(pid))
                {
                    clientAddress = clients[pid].Item1;
                    responseSocket.SendFrame(clientAddress);
                    return;
                }
                portOffset++;
                RequestSocket clientRequestSocket = new RequestSocket();
                clientAddress = config.RequestSocket + ":" + ((config.StartPortNumber + portOffset) % 65535);
                clientRequestSocket.Connect(clientAddress);

                clients.Add(pid, Tuple.Create(clientAddress, clientRequestSocket));
                responseSocket.SendFrame(clientAddress);
                logger.Info($"Registered profiler on address {clientAddress}");
            }
        }

        /// <summary>
        /// Sends the given test event to all connected profiler instances.
        /// </summary>
        /// <param name="testEvent"></param>
        public void SendTestEvent(string testEvent)
        {
            HashSet<int> clientsToRemove = new HashSet<int>();
            System.Threading.Tasks.Parallel.ForEach(clients, entry =>
            {
                entry.Value.Item2.SendFrame(Encoding.UTF8.GetBytes(testEvent));
                if (entry.Value.Item2.TryReceiveFrameString(TimeSpan.FromSeconds(3.0), out string? response))
                {
                    logger.Info($"Got Response from {entry.Value.Item1}: {response}");
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
                    clients[client].Item2.Close();
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
                client.Value.Item2.Dispose();
            }
            NetMQConfig.Cleanup();
        }
    }
}

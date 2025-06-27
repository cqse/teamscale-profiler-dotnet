using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private Dictionary<string, ProfilerClient> idToClient = new Dictionary<string, ProfilerClient>();

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public delegate string RequestHandler(string message);

        private readonly IpcConfig config;
        private readonly RequestHandler requestHandler;

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

        private void RegisterClient(string message)
        {
            string[] splitMessage = message.Split(':');
            if (splitMessage.Length != 5)
            {
                logger.Error("Couldn't register client with message " + message + ", please check the format of the client address (tcp://1.2.3.4:1234).");
                return;
            }
            int pid = Int32.Parse(splitMessage[1]);
            string clientAddress = splitMessage[2] + ":" + splitMessage[3] + ":" + splitMessage[4];

            string clientId = pid + ":" + clientAddress;
            lock (idToClient)
            {
                if (idToClient.ContainsKey(clientId))
                {
                    clientAddress = idToClient[clientId].ClientAddress;
                    responseSocket.SendFrame(clientAddress);
                    return;
                }
                RequestSocket clientRequestSocket = new RequestSocket();
                clientRequestSocket.Connect(clientAddress);

                idToClient.Add(clientId, new ProfilerClient(clientAddress, clientRequestSocket));
                responseSocket.SendFrame(clientAddress);
                logger.Info($"Registered profiler on address {clientAddress}");
            }
        }

        /// <summary>
        /// Sends the given test event to all connected profiler instances.
        /// </summary>
        public void SendTestEvent(string testEvent)
        {
            HashSet<string> clientsToRemove = new HashSet<string>();
            System.Threading.Tasks.Parallel.ForEach(idToClient, entry =>
            {
                entry.Value.Socket.SendFrame(Encoding.UTF8.GetBytes(testEvent));
                if (entry.Value.Socket.TryReceiveFrameString(TimeSpan.FromSeconds(10.0), out string? response))
                {
                    logger.Info($"Got Response from {entry.Value.ClientAddress}: {response}");
                } else
                {
                    lock(clientsToRemove)
                    {
                        clientsToRemove.Add(entry.Key);
                    }
                    logger.Error($"Got no response from Profiler with PID {entry.Key} with address {entry.Value.ClientAddress}, removing from clients");
                }
            });
            lock (idToClient)
            {
                foreach (var client in clientsToRemove)
                {
                    if (!idToClient.ContainsKey(client)) {
                        continue;
                    }
                    idToClient[client].Socket.Close();
                    idToClient.Remove(client);
                }
            }
        }

        public void Dispose()
        {
            this.poller?.Dispose();
            this.responseSocket?.Dispose();
            foreach (var client in idToClient)
            {
                client.Value.Socket.Dispose();
            }
            NetMQConfig.Cleanup(false);
        }
    }
}

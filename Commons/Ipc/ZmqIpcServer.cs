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

        private Dictionary<int, ProfilerClient> pidToClient = new Dictionary<int, ProfilerClient>();

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

        private void RegisterClient(string message)
        {
            int pid = Int32.Parse(message.Split(':')[1]);
            lock (pidToClient)
            {
                string clientAddress;
                if (pidToClient.ContainsKey(pid))
                {
                    clientAddress = pidToClient[pid].ClientAddress;
                    responseSocket.SendFrame(clientAddress);
                    return;
                }
                RequestSocket clientRequestSocket = new RequestSocket();
                clientAddress = config.RequestSocket + ":" + ((config.StartPortNumber + portOffset) % 65535);
                portOffset++;
                clientRequestSocket.Connect(clientAddress);

                pidToClient.Add(pid, new ProfilerClient(clientAddress, clientRequestSocket));
                responseSocket.SendFrame(clientAddress);
                logger.Info($"Registered profiler on address {clientAddress}");
            }
        }

        /// <summary>
        /// Sends the given test event to all connected profiler instances.
        /// </summary>
        public void SendTestEvent(string testEvent)
        {
            HashSet<int> clientsToRemove = new HashSet<int>();
            System.Threading.Tasks.Parallel.ForEach(pidToClient, entry =>
            {
                entry.Value.Socket.SendFrame(Encoding.UTF8.GetBytes(testEvent));
                if (entry.Value.Socket.TryReceiveFrameString(TimeSpan.FromSeconds(3.0), out string? response))
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
            lock (pidToClient)
            {
                foreach (var client in clientsToRemove)
                {
                    if (!pidToClient.ContainsKey(client)) {
                        continue;
                    }
                    pidToClient[client].Socket.Close();
                    pidToClient.Remove(client);
                }
            }
        }

        public void Dispose()
        {
            this.poller?.Dispose();
            this.responseSocket?.Dispose();
            foreach (var client in pidToClient)
            {
                client.Value.Socket.Dispose();
            }
            NetMQConfig.Cleanup(false);
        }
    }
}

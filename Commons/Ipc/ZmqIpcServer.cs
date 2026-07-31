using NetMQ;
using NetMQ.Sockets;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        /// How long to wait for a profiler to acknowledge a broadcast message.
        /// </summary>
        public static readonly TimeSpan DefaultResponseTimeout = TimeSpan.FromSeconds(3.0);

        private NetMQPoller? poller;
        private ResponseSocket? responseSocket;

        private Dictionary<int, ProfilerClient> pidToClient = new Dictionary<int, ProfilerClient>();

        /// <summary>
        /// Serializes broadcasts so that no two threads use the same client socket at the same time.
        /// </summary>
        private readonly object broadcastLock = new object();

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
            Broadcast(testEvent, DefaultResponseTimeout);
        }

        /// <summary>
        /// Sends the given message to all connected profiler instances and waits for their acknowledgement.
        /// Profilers that don't answer within the given timeout are considered gone and are removed.
        /// </summary>
        public void Broadcast(string message, TimeSpan responseTimeout)
        {
            RemoveClientsOfDeadProcesses();

            KeyValuePair<int, ProfilerClient>[] clients;
            lock (pidToClient)
            {
                // take a snapshot so a profiler that registers while we are sending doesn't
                // invalidate the enumeration
                clients = pidToClient.ToArray();
            }

            HashSet<int> clientsToRemove = new HashSet<int>();
            // each client has its own socket, but a socket must not be used by two threads at once,
            // so only one broadcast may be in flight at a time
            lock (broadcastLock)
            {
                System.Threading.Tasks.Parallel.ForEach(clients, entry =>
                {
                    entry.Value.Socket.SendFrame(Encoding.UTF8.GetBytes(message));
                    if (entry.Value.Socket.TryReceiveFrameString(responseTimeout, out string? response))
                    {
                        logger.Info($"Got Response from {entry.Value.ClientAddress}: {response}");
                    }
                    else
                    {
                        lock (clientsToRemove)
                        {
                            clientsToRemove.Add(entry.Key);
                        }
                        logger.Error($"Got no response from Profiler with PID {entry.Key} with address {entry.Value.ClientAddress}, removing from clients");
                    }
                });
            }

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

        /// <summary>
        /// Drops all clients whose profiled process has ended. Profiled applications are often killed
        /// instead of being shut down gracefully, in which case they never tell us that they are gone.
        /// Without this, every broadcast would have to wait for the response timeout of each of them.
        /// </summary>
        private void RemoveClientsOfDeadProcesses()
        {
            lock (pidToClient)
            {
                foreach (int pid in pidToClient.Keys.ToList())
                {
                    if (IsProcessRunning(pid))
                    {
                        continue;
                    }
                    logger.Info($"Profiled process with PID {pid} has ended, removing from clients");
                    pidToClient[pid].Socket.Close();
                    pidToClient.Remove(pid);
                }
            }
        }

        private static bool IsProcessRunning(int pid)
        {
            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    return !process.HasExited;
                }
            }
            catch (ArgumentException)
            {
                // thrown if no process with that id is running
                return false;
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

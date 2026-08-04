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
        /// Message a profiler is asked to answer before it is sent an actual request
        /// </summary>
        private const string ALIVE_CHECK = "alive";

        /// <summary>
        /// How long to wait for a profiler to answer the alive check. Answering it requires no work
        /// from the profiler, so this stays short no matter how expensive the request that follows is.
        /// </summary>
        public static readonly TimeSpan AliveCheckTimeout = TimeSpan.FromSeconds(3.0);

        /// <summary>
        /// How long to wait for a profiler to acknowledge a broadcast message. This is only ever waited
        /// for by a profiler that just answered the alive check, i.e. one that we know is there and
        /// responsive, so it can be generous enough for the most expensive request we send. A profiler
        /// that is gone is already sorted out by the alive check.
        /// </summary>
        public static readonly TimeSpan ResponseTimeout = TimeSpan.FromMinutes(1.0);

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
            Broadcast(testEvent);
        }

        /// <summary>
        /// Sends the given message to all connected profiler instances and waits for their acknowledgement.
        /// Profilers that don't answer are considered gone and are removed.
        /// Each profiler is first asked to answer an alive check.
        /// </summary>
        public void Broadcast(string message)
        {
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
                    void Remove()
                    {
                        lock (clientsToRemove)
                        {
                            clientsToRemove.Add(entry.Key);
                        }
                    }

                    if (Request(entry.Value, ALIVE_CHECK, AliveCheckTimeout) == null)
                    {
                        Remove();
                        logger.Error($"Profiler with PID {entry.Key} with address {entry.Value.ClientAddress} did not answer the alive check within {AliveCheckTimeout}, removing from clients without sending '{message}'");
                        return;
                    }

                    string? response = Request(entry.Value, message, ResponseTimeout);
                    if (response == null)
                    {
                        Remove();
                        logger.Error($"Profiler with PID {entry.Key} with address {entry.Value.ClientAddress} answered the alive check but did not acknowledge '{message}' within {ResponseTimeout}, removing from clients");
                        return;
                    }
                    logger.Info($"Got Response from {entry.Value.ClientAddress}: {response}");
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
        /// Sends the given message to the given profiler and returns its answer, or null if it did not
        /// answer within the given timeout. In the latter case the socket must not be used again: a
        /// request socket that is still waiting for an answer cannot send the next message.
        /// </summary>
        private static string? Request(ProfilerClient client, string message, TimeSpan timeout)
        {
            client.Socket.SendFrame(Encoding.UTF8.GetBytes(message));
            if (client.Socket.TryReceiveFrameString(timeout, out string? response))
            {
                return response;
            }
            return null;
        }

        private static bool IsProcessRunning(int pid)
        {
            try
            {
                using Process process = Process.GetProcessById(pid);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                // if no process with that id is running
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

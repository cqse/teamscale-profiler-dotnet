using System;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    /// <summary>
    /// Configuration for ZeroMQ IPC.
    /// </summary>
    public class IpcConfig
    {
        /// <summary>
        /// The ZeroMQ socket to publish messages to the subscribed profilers via PUB-SUB pattern.
        /// Default ist localhost TCP port 7145 (leet for TIA-Socket)
        /// </summary>
        public string PublishSocket { get; } = "tcp://127.0.0.1:7145";

        /// <summary>
        /// The ZeroMQ socket to receive and answer requests from subscribed profilers via REQ-REP pattern.
        /// Default ist localhost TCP port 7146 (leet for TIA-Socket + 1 = 7145 + 1)
        /// </summary>
        public string RequestSocket { get; } = "tcp://127.0.0.1";

        public int StartPortNumber { get; } = 7146;

        public IpcConfig()
        {
            // defaults
        }

        public IpcConfig(string publishSocket, string requestSocket)
        {
            PublishSocket = publishSocket;
            RequestSocket = requestSocket.Substring(0, requestSocket.LastIndexOf(':')) ;
            StartPortNumber = Int32.Parse(RequestSocket.Split(':').Last());
        }
    }
}

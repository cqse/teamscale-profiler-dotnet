using Cqse.Teamscale.Profiler.Commons.Ipc;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    public class RecordingProfilerIpc : ProfilerIpc
    {
        private readonly ConcurrentBag<string> receivedRequests = new ConcurrentBag<string>();

        public IEnumerable<string> ReceivedRequests => receivedRequests;

        public RecordingProfilerIpc() : this(CreateIpcConfigWithRandomPorts())
        {
            // empty, just delegate
        }

        public RecordingProfilerIpc(IpcConfig config) : base(config)
        {
            // empty, just delegate
        }

        protected override string HandleRequest(string message)
        {
            receivedRequests.Add(message);
            return base.HandleRequest(message);
        }

        private static IpcConfig CreateIpcConfigWithRandomPorts()
            => new IpcConfig("tcp://127.0.0.1:" + GetAvailablePort(), "tcp://127.0.0.1:" + GetAvailablePort());

        private static int GetAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}

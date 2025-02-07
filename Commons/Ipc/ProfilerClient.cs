using NetMQ.Sockets;

namespace Cqse.Teamscale.Profiler.Commons.Ipc
{
    internal class ProfilerClient
    {
        internal ProfilerClient(string clientAddress, RequestSocket socket)
        {
            ClientAddress = clientAddress;
            Socket = socket;
        }

        public string ClientAddress { get; }
        public RequestSocket Socket { get; }
    }
}

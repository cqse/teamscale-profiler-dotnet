using NUnit.Framework;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ZeroMQ;

namespace Cqse.Teamscale.Profiler.Dotnet
{
    /// <summary>
    /// Test case for coverage profiler.
    /// </summary>
    [TestFixture]
    public class TiaTest : ProfilerTestBase
    {
        private static readonly string PublishSocketAddress = "tcp://127.0.0.1:" + GetAvailablePort();
        private static readonly string ReplySocketAddress = "tcp://127.0.0.1:" + GetAvailablePort();
        private static readonly ZContext Context = new ZContext();
        private static readonly ZSocket PublishSocket = new ZSocket(Context, ZSocketType.REP);
        private static readonly Thread RequestHandlerThread = new Thread(StartRequestHandler);
        private static readonly ConcurrentBag<string> ReceivedRequests = new ConcurrentBag<string>();

        [OneTimeSetUp]
        public static void StartZmq()
        {
            PublishSocket.Bind(PublishSocketAddress);
            RequestHandlerThread.Start();
        }

        private static void StartRequestHandler()
        {
            using (ZSocket replySocket = new ZSocket(Context, ZSocketType.REP))
            {
                replySocket.Bind(ReplySocketAddress);
                while (true)
                {
                    using (ZFrame request = replySocket.ReceiveFrame())
                    {
                        ReceivedRequests.Add(request.ReadString());
                        using (ZFrame reply = new ZFrame("sample test"))
                        {
                            replySocket.Send(reply);
                        }
                    }
                }
            }
        }

        [OneTimeTearDown]
        public static void StopZmq()
        {
            RequestHandlerThread.Abort();
            PublishSocket.Dispose();
            Context.Dispose();
        }

        /// <summary>
        /// Runs the profiler with command line argument and asserts its content is logged into the trace.
        /// </summary>
        [Test]
        public void TestRequestTestNameOnStart()
        {
            var environment = new Dictionary<string, string>()
            {
                ["COR_PROFILER_TIA"] = "true",
                ["COR_PROFILER_TIA_SUBSCRIBE_SOCKET"] = PublishSocketAddress, // PUB-SUB
                ["COR_PROFILER_TIA_REQUEST_SOCKET"] = ReplySocketAddress, // REQ-REP
            };

            FileInfo actualTrace = AssertSingleTrace(RunProfiler("ProfilerTestee.exe", arguments: "all", lightMode: true, bitness: Bitness.x86, environment: environment));
            string[] lines = File.ReadAllLines(actualTrace.FullName);
            Assert.That(lines, Has.One.StartsWith($"Info=TIA enabled. SUB: {PublishSocketAddress} REQ: {ReplySocketAddress}"));
            Assert.That(lines, Has.One.StartsWith("Stopped="));
            Assert.That(lines, Has.One.StartsWith("Test=sample test"));
            Assert.That(ReceivedRequests, Is.EquivalentTo(new[] { "profiler_connected", "get_testname", "profiler_disconnected" }));
        }

        public static int GetAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}

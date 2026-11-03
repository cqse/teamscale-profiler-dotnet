using NUnit.Framework;
using System.IO.Pipes;

namespace UploadDaemon
{
    [TestFixture]
    public class UploadDaemonControlPipeTest
    {
        /// <summary>
        /// The daemon must stop instead of crashing if it cannot create its control pipe (TS-21163).
        /// </summary>
        [Test]
        [Timeout(10000)]
        public void TestStopsIfControlPipeCannotBeCreated()
        {
            // A single-instance server that holds the pipe name makes the daemon's pipe creation fail with an IOException.
            using (NamedPipeServerStream blockingServer = new NamedPipeServerStream(UploadDaemon.DaemonControlPipeName, PipeDirection.In, 1))
            {
                UploadDaemon daemon = new UploadDaemon();

                Assert.DoesNotThrow(() => daemon.WaitForNotifications());
            }
        }
    }
}

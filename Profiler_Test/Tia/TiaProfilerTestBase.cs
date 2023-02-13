using Cqse.Teamscale.Profiler.Commons.Ipc;
using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    public abstract class TiaProfilerTestBase : ProfilerTestBase
    {
        protected readonly IpcImplementation ipcImplementation;
        protected RecordingProfilerIpc profilerIpc;
        protected TiaProfiler profilerUnderTest;

        public enum IpcImplementation
        {
            /// <summary>
            /// The default NetMQ based IPC server implementation
            /// </summary>
            NetMQ,

            /// <summary>
            /// Alternate native libzmq based IPC implementation
            /// </summary>
            Native,
        }

        public TiaProfilerTestBase(IpcImplementation ipcImplementation)
        {
            this.ipcImplementation = ipcImplementation;
        }

        [SetUp]
        public void SetUpProfilerUnderTest()
        {
            profilerIpc = CreateProfilerIpc();
            profilerUnderTest = new TiaProfiler(basePath: SolutionRoot, targetDir: TestTraceDirectory, profilerIpc.Config);
        }

        protected virtual RecordingProfilerIpc CreateProfilerIpc(IpcConfig config = null)
        {
            if (this.ipcImplementation == IpcImplementation.Native)
            {
                return new NativeRecordingProfilerIpc(config);
            }

            return new RecordingProfilerIpc(config);
        }

        [TearDown]
        public void StopZmq()
        {
            profilerIpc?.Dispose();
        }
    }
}

using Cqse.Teamscale.Profiler.Commons.Ipc;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    internal class NativeRecordingProfilerIpc : RecordingProfilerIpc
    {
        public NativeRecordingProfilerIpc() : base()
        {
            // empty, just delegate
        }

        public NativeRecordingProfilerIpc(IpcConfig config) : base(config)
        {
            // empty, just delegate
        }

        protected override IpcServer CreateIpcServer(IpcConfig config)
        {
            return new NativeZmqIpcServer(config, this.HandleRequest);
        }
    }
}

using NUnit.Framework;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    /// <summary>
    /// Test case for coverage profiler.
    /// </summary>
    [TestFixture]
    public class TiaProfilerTestWithNativeZmq : TiaProfilerTest
    {
        protected override RecordingProfilerIpc CreateProfilerIpc()
        {
            return new NativeRecordingProfilerIpc();
        }
    }
}

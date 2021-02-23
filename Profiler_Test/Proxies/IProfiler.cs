using System.Diagnostics;
using static Cqse.Teamscale.Profiler.Dotnet.ProfilerTestBase;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    public interface IProfiler
    {
        void RegisterOn(ProcessStartInfo processInfo);

    }
}
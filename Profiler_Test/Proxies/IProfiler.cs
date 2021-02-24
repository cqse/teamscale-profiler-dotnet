using System.Diagnostics;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    public interface IProfiler
    {
        void RegisterOn(ProcessStartInfo processInfo, Bitness? bitness = null);
    }
}

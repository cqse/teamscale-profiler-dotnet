using System.Diagnostics;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    /// <summary>
    /// A profiler that may register itself to a process.
    /// </summary>
    public interface IProfiler
    {
        /// <summary>
        /// Registers this profiler on the given process.
        /// </summary>
        void RegisterOn(ProcessStartInfo processInfo, Bitness? bitness = null);
    }
}

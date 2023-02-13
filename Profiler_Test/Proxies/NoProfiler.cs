
using System.Diagnostics;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    /// <summary>
    /// A NOP profiler that does not register anything on the target process.
    /// </summary>
    public class NoProfiler : IProfiler
    {    
        /// <inheritDoc/>
        public void RegisterOn(ProcessStartInfo processInfo, Bitness? bitness = null)
        {
            Profiler.ClearProfilerRegistration(processInfo);
        }
    }
}

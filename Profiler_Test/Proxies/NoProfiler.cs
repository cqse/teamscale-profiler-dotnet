
using System.Diagnostics;

namespace Cqse.Teamscale.Profiler.Dotnet.Proxies
{
    public class NoProfiler : IProfiler
    {    
        public void RegisterOn(ProcessStartInfo processInfo)
        {
            Profiler.ClearProfilerRegistration(processInfo);
        }
    }
}

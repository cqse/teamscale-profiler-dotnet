using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;

namespace Cqse.Teamscale.Profiler.Commander.Server
{
    [Route("profiler")]
    [ApiController]
    public class ProfilerController : ControllerBase
    {
        private readonly ProfilerIpc profilerIpc;
        private readonly ILogger logger;

        public ProfilerController(ProfilerIpc profilerIpc, ILogger<ProfilerController> logger)
        {
            this.profilerIpc = profilerIpc;
            this.logger = logger;
        }

        /// <summary>
        /// Asks all connected profilers to write the coverage they collected so far to disk. This only
        /// returns once they have done so, i.e. the profiled applications may be killed afterwards
        /// without losing their coverage.
        /// </summary>
        [HttpPost("dump")]
        public IActionResult DumpCoverage()
        {
            logger.LogInformation("Dumping the coverage of all connected profilers");
            profilerIpc.DumpCoverage();
            return NoContent();
        }
    }
}

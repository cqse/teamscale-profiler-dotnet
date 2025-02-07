using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Web;

namespace Cqse.Teamscale.Profiler.Commander.Server
{
    [Route("test")]
    [ApiController]
    public class ProfilerTestController : ControllerBase
    {
        private readonly ProfilerIpc profilerIpc;
        private readonly ProfilerTestControllerState state;
        private readonly ILogger logger;

        public ProfilerTestController(ProfilerIpc profilerIpc, ProfilerTestControllerState state, ILogger<ProfilerTestController> logger)
        {
            this.profilerIpc = profilerIpc;
            this.state = state;
            this.logger = logger;
        }

        [HttpGet]
        public string GetCurrent()
        {
            return profilerIpc.TestName;
        }

        public long GetStart()
        {
            return state.TestStartMS;
        }

        [HttpPost("start/{testName}")]
        public HttpStatusCode StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            logger.LogInformation("Starting test: {}", testName);
            state.TestStartMS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
            return HttpStatusCode.NoContent;
        }

        [HttpPost("stop/{result}")]
        public HttpStatusCode StopTest(TestExecutionResult result)
        {
            long testEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            logger.LogInformation("Stopping test: {}; Result: {}; Duration: {}", GetCurrent(), result, testEnd - GetStart());
            profilerIpc.EndTest(result, durationMs: testEnd - GetStart());
            return HttpStatusCode.NoContent;
        }
    }
}

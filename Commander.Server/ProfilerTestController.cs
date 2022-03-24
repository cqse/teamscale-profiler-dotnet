using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;
using System.Web;

namespace Cqse.Teamscale.Profiler.Commander.Server
{
    [Route("test")]
    [ApiController]
    public class ProfilerTestController : ControllerBase
    {
        private readonly ProfilerIpc profilerIpc;

        public ProfilerTestController(ProfilerIpc profilerIpc)
        {
            this.profilerIpc = profilerIpc;
        }

        [HttpGet]
        public string GetCurrent()
        {
            return profilerIpc.TestName;
        }

        [HttpPost("start/{testName}")]
        public void startTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
        }

        [HttpPost("stop/{result}")]
        public void stopTest(TestExecutionResult result)
        {
            profilerIpc.EndTest(result);
        }
    }
}

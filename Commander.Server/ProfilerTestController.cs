using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
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
        public void StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
        }

        [HttpPost("stop/{result}")]
        public void StopTest(TestExecutionResult result)
        {
            profilerIpc.EndTest(result);
        }

        /// <summary>
        /// Legacy end test to match the JaCoCo API.
        /// </summary>
        [HttpPost("end/{name}")]
        public void EndTest(string name, [FromBody] TestResultDto result)
        {
            profilerIpc.EndTest(result.Result);
        }

        public class TestResultDto
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TestExecutionResult Result { get; set; }
            public string? Message { get; set; }
        }
    }
}

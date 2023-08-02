using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json.Serialization;
using System.Web;

namespace Cqse.Teamscale.Profiler.Commander.Server
{
    [Route("test")]
    [ApiController]
    public class ProfilerTestController : ControllerBase
    {
        private readonly ProfilerIpc profilerIpc;
        private readonly ILogger logger;

        public ProfilerTestController(ProfilerIpc profilerIpc, ILogger<ProfilerTestController> logger)
        {
            this.profilerIpc = profilerIpc;
            this.logger = logger;
        }

        [HttpGet]
        public string GetCurrent()
        {
            return profilerIpc.TestName;
        }

        [HttpPost("start/{testName}")]
        public HttpStatusCode StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            logger.LogInformation("Starting test: {}", testName);
            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
            return HttpStatusCode.NoContent;
        }

        [HttpPost("stop/{result}")]
        public HttpStatusCode StopTest(TestExecutionResult result)
        {
            logger.LogInformation("Stopping test: {}; Result: {}", GetCurrent(), result);
            profilerIpc.EndTest(result);

            return HttpStatusCode.NoContent;

        }

        /// <summary>
        /// Legacy end test to match the JaCoCo API.
        /// </summary>
        [HttpPost("end/{name}")]
        public HttpStatusCode EndTest(string name, [FromBody] TestResultDto result)
        {
            logger.LogInformation("Stopping test (JaCoCo endpoint): {}; Result: {}", name, result.Result);
            profilerIpc.EndTest(result.Result);

            return HttpStatusCode.NoContent;
        }

        public class TestResultDto
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TestExecutionResult Result { get; set; }
            public string? Message { get; set; }
        }
    }
}

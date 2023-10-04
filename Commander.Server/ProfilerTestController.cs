using Cqse.Teamscale.Profiler.Commons.Ipc;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;
using System.Web;
using System.Xml.Linq;

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

        public long GetStart()
        {
            return profilerIpc.TestStartMS;
        }

        [HttpPost("start/{testName}")]
        public void StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            logger.LogInformation("Starting test: {}", testName);
            profilerIpc.TestStartMS = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
        }

        [HttpPost("stop/{result}")]
        public void StopTest(TestExecutionResult result)
        {
            long testEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            logger.LogInformation("Stopping test (JaCoCo endpoint): {}; Result: {}; duration: {}", GetCurrent(), result, testEnd - GetStart());
            profilerIpc.EndTest(result, durationMs: testEnd - GetStart());
        }

        /// <summary>
        /// Legacy end test to match the JaCoCo API.
        /// </summary>
        [HttpPost("end/{name}")]
        public void EndTest(string name, [FromBody] TestResultDto result)
        {
            long testEnd = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            logger.LogInformation("Stopping test (JaCoCo endpoint): {}; Result: {}; duration: {}", name, result.Result, testEnd - GetStart());
            profilerIpc.EndTest(result.Result, durationMs: testEnd - GetStart());
        }

        public class TestResultDto
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TestExecutionResult Result { get; set; }
            public string? Message { get; set; }
        }
    }
}

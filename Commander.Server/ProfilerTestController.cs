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
        public void StartTest(string testName)
        {
            if (string.IsNullOrEmpty(testName))
            {
                throw new BadHttpRequestException("Test name may not be empty");
            }

            logger.LogInformation("Starting test: {}", testName);
            profilerIpc.StartTest(HttpUtility.UrlDecode(testName));
        }

        /// <summary>
        /// Azure Devops requires an update endpoint for manual testing.
        /// </summary>
        [HttpPost("update/{testName}")]
        public void UpdateTest(string testName, [FromQuery(Name = "result")] TestExecutionResult result, [FromQuery(Name = "extended-name")] string extendedName)
        {
            logger.LogInformation("Updating test: {}; Result: {}", testName, result);
            profilerIpc.TestResults[testName] = result;
        }

        [HttpPost("stop/{result}")]
        public void StopTest(TestExecutionResult result)
        {
            logger.LogInformation("Stopping test: {}; Result: {}", GetCurrent(), result);
            profilerIpc.EndTest(result);
        }

        /// <summary>
        /// Legacy end test to match the JaCoCo API.
        /// </summary>
        [HttpPost("end/{testName}")]
        public void EndTest(string testName, [FromBody] TestResultDto result)
        {
            TestExecutionResult testResult = result.Result;
            if (result == null || profilerIpc.TestResults.ContainsKey(testName))
            {
                logger.LogInformation("Stopping test (JaCoCo endpoint): {}; Result: {}", testName, profilerIpc.TestResults[testName]);
                profilerIpc.EndTest(profilerIpc.TestResults[testName]);
                profilerIpc.TestResults.Remove(testName);
                return;
            }
            logger.LogInformation("Stopping test (JaCoCo endpoint): {}; Result: {}", testName, testResult);
            profilerIpc.EndTest(testResult);
        }

        public class TestResultDto
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TestExecutionResult Result { get; set; }
            public string? Message { get; set; }
        }
    }
}

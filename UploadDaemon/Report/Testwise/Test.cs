using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// A test in a <see cref="TestwiseCoverageReport"/>.
    /// </summary>
    public class Test
    {
        [JsonProperty(PropertyName = "uniformPath")]
        public string UniformPath;

        [JsonProperty(PropertyName = "duration")]
        public double Duration;

        [JsonProperty(PropertyName = "result")]
        public string Result;

        [JsonProperty(PropertyName = "message")]
        public string Message;

        [JsonProperty(PropertyName = "content")]
        public string Content;

        [JsonProperty(PropertyName = "paths")]
        public IList<CoverageForPath> CoverageByPath;

        public Test(string uniformPath, params File[] coverageByFile)
        {
            UniformPath = uniformPath;
            CoverageByPath = new List<CoverageForPath>()
            {
                new CoverageForPath(coverageByFile)
            };
        }

        public Test(string uniformPath, SimpleCoverageReport report) : this(uniformPath, report.FileNames.Select(fileName => new File(fileName, report[fileName])).ToArray()) { }
    }
}

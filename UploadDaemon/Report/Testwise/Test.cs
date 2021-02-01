using Newtonsoft.Json;
using System.Collections.Generic;

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
    }
}

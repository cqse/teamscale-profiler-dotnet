using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report
{
    /// <summary>
    /// A Teamscale testwise coverage report.
    /// </summary>
    public class TestwiseCoverageReport : ICoverageReport
    {
        public class Test
        {
            public class CoverageForPath
            {
                public static CoverageForPath From(LineCoverageReport report)
                {
                    return new CoverageForPath()
                    {
                        Files = report.FileNames.Select(fileName => new Dictionary<string, string>() {
                            { "fileName", fileName },
                            { "coveredLines", report[fileName].CoveredLineRanges.Select((range) =>  $"{range.Item1}-{range.Item2}").Aggregate((a, b) => $"{a},{b}") }
                        }).ToList<IDictionary<string, string>>()
                    };
                }

                [JsonProperty(PropertyName = "path")]
                public readonly string Path = "";

                [JsonProperty(PropertyName = "files")]
                public IList<IDictionary<string, string>> Files { get; set; }
            }

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

        [JsonProperty(PropertyName = "tests")]
        public IList<Test> Tests = new List<Test>();
    }
}

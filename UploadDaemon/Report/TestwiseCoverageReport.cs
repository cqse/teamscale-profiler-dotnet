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
                        Files = report.FileNames.Select(fileName => new File() {
                            FileName = fileName,
                            CoveredLines = report[fileName].CoveredLineRanges.Select((range) =>  $"{range.Item1}-{range.Item2}").Aggregate((a, b) => $"{a},{b}")
                        }).ToList()
                    };
                }

                [JsonProperty(PropertyName = "path")]
                public readonly string Path = "";

                [JsonProperty(PropertyName = "files")]
                public IList<File> Files { get; set; }

                public class File
                {
                    [JsonProperty(PropertyName = "fileName")]
                    public string FileName;

                    [JsonProperty(PropertyName = "coveredLines")]
                    public string CoveredLines;
                }
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

        public bool IsEmpty => Tests.All(test => test.CoverageByPath.All(coverage => coverage.Files.Count == 0));

        public string FileExtension => "testwise";

        public ICoverageReport UnionWith(ICoverageReport coverageReport)
        {
            throw new System.NotImplementedException();
        }


        /// <summary>
        /// Converts this report into a TESTWISE format report for Teamscale.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

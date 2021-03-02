using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Report.Simple;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// A test in a <see cref="TestwiseCoverageReport"/>.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Test
    {
        [JsonProperty(PropertyName = "uniformPath")]
        public string UniformPath;

        public DateTime Start;

        public DateTime End;

        [JsonProperty(PropertyName = "duration")]
        public double Duration
        {
            get => End.Subtract(Start).TotalSeconds;
            set
            {
                // We cannot reconstruct the timestamps from the report format
                End = DateTime.Today;
                Start = End.Subtract(TimeSpan.FromSeconds(value));
            }
        }

        [JsonProperty(PropertyName = "result")]
        public string Result = "SKIPPED";

        [JsonProperty(PropertyName = "content")]
        public string Content;

        [JsonProperty(PropertyName = "message")]
        public string Message;

        [JsonProperty(PropertyName = "paths")]
        public IList<CoverageForPath> CoverageByPath;

        public Test(string uniformPath, params File[] coverageByFile)
        {
            UniformPath = uniformPath;
            if (coverageByFile.Length > 0)
            {
                CoverageByPath = new List<CoverageForPath>()
                {
                    new CoverageForPath(coverageByFile)
                };
            }
            else
            {
                CoverageByPath = new List<CoverageForPath>();
            }
        }

        public Test(string uniformPath, SimpleCoverageReport report) : this(uniformPath, report.FileNames.Select(fileName => new File(fileName, report[fileName])).ToArray()) { }

        public Test Union(Test other)
        {
            return new Test(UniformPath, (SimpleCoverageReport)ToSimpleCoverageReport().Union(other.ToSimpleCoverageReport()))
            {
                Start = new DateTime(Math.Min(Start.Ticks, other.Start.Ticks)),
                End = new DateTime(Math.Max(End.Ticks, other.End.Ticks)),
                Result = Result.Equals("SKIPPED") ? other.Result : Result,
                Message = Message,
                Content = Content,
            };
        }

        public SimpleCoverageReport ToSimpleCoverageReport()
        {
            return new SimpleCoverageReport(CoverageByPath.First().Files.ToDictionary(file => file.FileName, file => (FileCoverage)file));
        }
    }
}

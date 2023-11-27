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

        /// <summary>
        /// Duration of the test case in milliseconds
        /// </summary>
        public long DurationMillis;

        [JsonProperty(PropertyName = "duration")]
        public double Duration
        {

            get {
                if (DurationMillis != 0)
                {
                    return Convert.ToDouble(DurationMillis) / 1000;
                }
                return End.Subtract(Start).TotalSeconds;
            }
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
            long thisTimeInterval = End.Ticks - Start.Ticks;
            long otherTimeInterval = other.End.Ticks - other.Start.Ticks;

            // When merging coverage from different test cases, we use the longest time for a testcase.
            // Using the earliest start and latest end has issues with repeated execution.
            DateTime newStart;
            DateTime newEnd;
            if (thisTimeInterval > otherTimeInterval)
            {
                newStart = Start;
                newEnd = End;
            } else
            {
                newStart = other.Start;
                newEnd = other.End;
            }

            return new Test(UniformPath, (SimpleCoverageReport)ToSimpleCoverageReport().Union(other.ToSimpleCoverageReport()))
            {
                Start = newStart,
                End = newEnd,
                Result = Result.Equals("SKIPPED") ? other.Result : Result,
                Message = Message,
                Content = Content,
            };
        }

        public SimpleCoverageReport ToSimpleCoverageReport()
        {
            IDictionary<string, FileCoverage> lineCoverageByPath = new Dictionary<string, FileCoverage>();
            if (CoverageByPath.Any())
            {
                lineCoverageByPath = CoverageByPath.First().Files.ToDictionary(file => file.FileName, file => (FileCoverage)file);
            }
            return new SimpleCoverageReport(lineCoverageByPath);
        }
    }
}

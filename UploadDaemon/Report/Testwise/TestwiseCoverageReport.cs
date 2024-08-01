using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// A Teamscale testwise coverage report.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TestwiseCoverageReport : ICoverageReport
    {
        private const int MAX_REPORT_STRING_SIZE = 536_870_912;

        [JsonProperty("partial")]
        public bool Partial { get; }

        [JsonProperty("tests")]
        public Test[] Tests { get; }


        public TestwiseCoverageReport(Test[] tests) : this(false, tests) { }

        public TestwiseCoverageReport(bool partial, Test[] tests)
        {
            Partial = partial;
            Tests = tests;
        }

        /// <inheritDoc/>
        public bool IsEmpty => Tests.All(test => test.CoverageByPath.All(coverage => coverage.Files.Count == 0));

        /// <inheritDoc/>
        public string FileExtension => "testwise";

        /// <inheritDoc/>
        public string UploadFormat => "TESTWISE_COVERAGE";


        /// <inheritDoc/>
        public ICoverageReport Union(ICoverageReport coverageReport)
        {
            if (!(coverageReport is TestwiseCoverageReport other))
            {
                throw new NotSupportedException();
            }

            IDictionary<string, Test> mergedCoverage = new Dictionary<string, Test>();
            foreach (Test test in this.Tests.Concat(other.Tests))
            {
                if (mergedCoverage.ContainsKey(test.UniformPath))
                {
                    mergedCoverage[test.UniformPath] = mergedCoverage[test.UniformPath].Union(test);
                }
                else
                {
                    mergedCoverage[test.UniformPath] = test;
                }
            }

            return new TestwiseCoverageReport(this.Partial || other.Partial, mergedCoverage.Values.ToArray());
        }

        /// <summary>
        /// Converts this report into a TESTWISE format report for Teamscale.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            
            sb.Append("{");
            if (Partial)
            {
                sb.Append("\"partial\":true,");
            }
            sb.Append("\"tests\":[");
            JsonSerializerSettings settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore };
            for (int i = 0; i < Tests.Length; i++)
            {
                sb.Append(JsonConvert.SerializeObject(Tests[i], settings));
                if (i < Tests.Length - 1) {
                    sb.Append(",");
                }
                Tests[i] = null;
            }
            sb.Append("]}");

            return sb.ToString();
        }

        /// <summary>
        /// Converts this report into a list of TESTWISE format reports for Teamscale.
        /// Reports are split to avoid too large strings (around 1GB) which cause OutOfMemoryErrors.
        /// </summary>
        public List<string> ToStringList()
        {
            List<string> result = new List<string>();
            StringBuilder sb = new StringBuilder();
            JsonSerializerSettings settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore };

            bool newReport = true;
            for (int i = 0; i < Tests.Length; i++)
            {
                if (newReport)
                {
                    AddReportStart(sb);
                    newReport = false;
                }
                else
                {
                    sb.Append(',');
                }
                
                sb.Append(JsonConvert.SerializeObject(Tests[i], settings));
                Tests[i] = null;

                if (sb.Length > MAX_REPORT_STRING_SIZE || i == Tests.Length - 1)
                {
                    sb.Append("]}");
                    result.Add(sb.ToString());
                    sb = new StringBuilder();
                    newReport = true;
                }
            }

            return result;
        }

        private void AddReportStart(StringBuilder sb)
        {
            sb.Append("{");
            if (Partial)
            {
                sb.Append("\"partial\":true,");
            }
            sb.Append("\"tests\":[");
        }
    }
}

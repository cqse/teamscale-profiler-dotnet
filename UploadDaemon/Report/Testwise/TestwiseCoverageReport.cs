using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// We build the JSON envelope manually and serialize the tests one by one (freeing each via
        /// <c>Tests[i] = null</c>) instead of calling <c>JsonConvert.SerializeObject(this)</c>. This keeps
        /// peak memory low for very large reports (around 1GB) which would otherwise cause OutOfMemoryErrors,
        /// since serializing the whole object graph at once holds both the graph and the full string in memory.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            AddReportStart(sb);
            JsonSerializerSettings settings = new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore };
            for (int i = 0; i < Tests.Length; i++)
            {
                sb.Append(JsonConvert.SerializeObject(Tests[i], settings));
                if (i < Tests.Length - 1) {
                    sb.Append(",");
                }
                Tests[i] = null;
            }
            AddReportEnd(sb);

            return sb.ToString();
        }

        /// <summary>
        /// Converts this report into a list of TESTWISE format reports for Teamscale.
        /// Reports are split to avoid too large strings (around 1GB) which cause OutOfMemoryErrors.
        /// </summary>
        public List<string> ToStringList()
        {
            return ToStringList(MAX_REPORT_STRING_SIZE);
        }

        /// <summary>
        /// <inheritdoc cref="ToStringList()"/>
        /// The <paramref name="maxReportStringSize"/> overload exposes the split threshold so tests can
        /// exercise the splitting logic with a small size instead of building gigabyte-sized reports.
        /// </summary>
        public List<string> ToStringList(int maxReportStringSize)
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

                if (sb.Length > maxReportStringSize || i == Tests.Length - 1)
                {
                    AddReportEnd(sb);
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

        private void AddReportEnd(StringBuilder sb)
        {
            sb.Append("]}");
        }
    }
}

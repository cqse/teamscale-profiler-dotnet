using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// A Teamscale testwise coverage report.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TestwiseCoverageReport : ICoverageReport
    {
        [JsonProperty("partial")]
        public bool Partial { get; }

        [JsonProperty("tests")]
        public List<Test> Tests { get; }

        public TestwiseCoverageReport(params Test[] tests) : this(false, tests) {}

        public TestwiseCoverageReport(bool partial, params Test[] tests)
        {
            Partial = partial;
            Tests = tests.ToList();
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
            foreach(Test test in new[] { Tests, other.Tests }.SelectMany(tests => tests))
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
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { DefaultValueHandling = DefaultValueHandling.Ignore });
        }
    }
}

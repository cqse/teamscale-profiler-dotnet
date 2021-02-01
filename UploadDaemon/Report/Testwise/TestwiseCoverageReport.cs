using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// A Teamscale testwise coverage report.
    /// </summary>
    public class TestwiseCoverageReport : ICoverageReport
    {
        [JsonProperty(PropertyName = "tests")]
        public IList<Test> Tests = new List<Test>();

        public TestwiseCoverageReport(params Test[] tests)
        {
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

            return new TestwiseCoverageReport(mergedCoverage.Values.ToArray());
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

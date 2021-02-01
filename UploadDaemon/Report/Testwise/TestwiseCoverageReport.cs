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

        public bool IsEmpty => Tests.All(test => test.CoverageByPath.All(coverage => coverage.Files.Count == 0));

        public string FileExtension => "testwise";

        /// <inheritDoc/>
        public ICoverageReport Union(ICoverageReport coverageReport)
        {
            if (!(coverageReport is TestwiseCoverageReport other))
            {
                throw new NotSupportedException();
            }

            return new TestwiseCoverageReport(new[] { Tests, other.Tests }.SelectMany(tests => tests).ToArray());
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

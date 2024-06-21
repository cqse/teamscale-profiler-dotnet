using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

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
        public Test[] Tests { get; }

        public List<(string project, RevisionOrTimestamp revisionOrTimestamp)> EmbeddedUploadTargets { get; }

        public TestwiseCoverageReport(Test[] tests, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets) : this(false, tests, embeddedUploadTargets) { }

        public TestwiseCoverageReport(bool partial, Test[] tests, List<(string project, RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets)
        {
            Partial = partial;
            Tests = tests;
            EmbeddedUploadTargets = embeddedUploadTargets;
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

            return new TestwiseCoverageReport(this.Partial || other.Partial, mergedCoverage.Values.ToArray(), this.EmbeddedUploadTargets);
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

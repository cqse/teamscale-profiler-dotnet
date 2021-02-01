using NUnit.Framework;
using System.Collections.Generic;
using UploadDaemon.Report;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemons.Report
{
    [TestFixture]
    public class SimpleCoverageReportTest
    {
        [Test]
        public void ConvertsToReportFormat()
        {
            SimpleCoverageReport report = new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                { "file1.cs", new FileCoverage((1,5),(7,10))},
                { "file2.cs", new FileCoverage((3, 20)) }
            });

            Assert.That(report.ToString(), Is.EqualTo(@"# isMethodAccurate=true
file1.cs
1-5
7-10
file2.cs
3-20
"));
        }
    }
}

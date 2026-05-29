using NUnit.Framework;
using System.Collections.Generic;
using UploadDaemon.Report.Simple;
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

        [Test]
        public void UnionMergesLineRangesForSameFileAndKeepsDifferentFiles()
        {
            SimpleCoverageReport report1 = new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                { "file1.cs", new FileCoverage((1, 5)) },
                { "file2.cs", new FileCoverage((3, 7)) }
            });
            SimpleCoverageReport report2 = new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                { "file1.cs", new FileCoverage((10, 20)) }
            });

            SimpleCoverageReport merged = report1.Union(report2) as SimpleCoverageReport;

            Assert.That(merged.FileNames, Is.EquivalentTo(new[] { "file1.cs", "file2.cs" }));
            Assert.That(merged["file1.cs"], Is.EqualTo(new FileCoverage((1, 5), (10, 20))));
            Assert.That(merged["file2.cs"], Is.EqualTo(new FileCoverage((3, 7))));
        }
    }
}

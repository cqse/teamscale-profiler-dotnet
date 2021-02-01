using NUnit.Framework;
using UploadDaemon.Report.Testwise;

namespace UploadDaemon.Report
{
    [TestFixture]
    public class TestwiseCoverageReportTest
    {
        [Test]
        public void MergesReportsWithDifferentTests()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10,20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test2", new File("file1.cs", (10,20))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Count.EqualTo(2));
            Assert.That(mergedReport.Tests[0].UniformPath, Is.EqualTo("Test1"));
            Assert.That(mergedReport.Tests[1].UniformPath, Is.EqualTo("Test2"));
        }

        [Test]
        public void MergesReportsWithSameTestsButDifferentFiles()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file2.cs", (10, 20))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Count.EqualTo(1));
            Assert.That(mergedReport.Tests[0].UniformPath, Is.EqualTo("Test1"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files, Has.Count.EqualTo(2));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[0].FileName, Is.EqualTo("file1.cs"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[1].FileName, Is.EqualTo("file2.cs"));
        }

        [Test]
        public void MergesReportsWithSameTestsAndSameFiles()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (30, 40))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Count.EqualTo(1));
            Assert.That(mergedReport.Tests[0].UniformPath, Is.EqualTo("Test1"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files, Has.Count.EqualTo(1));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[0].FileName, Is.EqualTo("file1.cs"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[0].CoveredLineRanges, Has.Count.EqualTo(2));
        }
    }
}

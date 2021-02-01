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

        [Test]
        public void ExportsToReportFormat()
        {
            TestwiseCoverageReport report = new TestwiseCoverageReport(
                new Test("Test/NotExecuted()"),
                new Test("Test/SomeTest()",
                    new File("Test/Code/File1.cs", (20,24), (26,29)),
                    new File("Test/Code/Other/File2.cs", (26,28)))
                {
                    Duration = 0.025,
                    Result = "PASSED",
                    Content = "43d743a8ef4389bc5",
                    Message = "Awesome!"
                }
            );

            Assert.That(report.ToString(), Is.EqualTo(@"{
  ""tests"": [
    {
                ""uniformPath"": ""Test/NotExecuted()"",
                ""paths"": []
    },
    {
                ""uniformPath"": ""Test/SomeTest()"",
      ""duration"": 0.025,
      ""result"": ""PASSED"",
      ""content"": ""43d743a8ef4389bc5"",
      ""message"": ""Awesome!"",
      ""paths"": [
        {
          ""path"": """",
          ""files"": [
            {
              ""fileName"": ""Test/Code/File1.cs"",
              ""coveredLines"": ""20-24,26-29""
            },
            {
              ""fileName"": ""Test/Code/Other/File2.cs"",
              ""coveredLines"": ""26-28""
            }
          ]
        }
      ]
    }
  ]
}".Replace(" ", "").Replace("\r\n", "")));
        }
    }
}

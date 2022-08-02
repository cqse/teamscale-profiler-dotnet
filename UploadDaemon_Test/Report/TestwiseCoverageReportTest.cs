using NUnit.Framework;
using System;
using UploadDaemon.Report.Testwise;

namespace UploadDaemon.Report
{
    [TestFixture]
    public class TestwiseCoverageReportTest
    {
        [Test]
        public void MergesDifferentTests()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10,20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test2", new File("file1.cs", (10,20))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(2));
            Assert.That(mergedReport.Tests[0].UniformPath, Is.EqualTo("Test1"));
            Assert.That(mergedReport.Tests[1].UniformPath, Is.EqualTo("Test2"));
        }

        [Test]
        public void MergesSameTestsRuntime()
        {
            DateTime now = DateTime.Now;
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))) { Start = now.Subtract(TimeSpan.FromSeconds(10)), End = now.Subtract(TimeSpan.FromSeconds(5)) });
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file2.cs", (10, 20))) { Start = now.Subtract(TimeSpan.FromSeconds(7)), End = now.Subtract(TimeSpan.FromSeconds(1)) });

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
            Assert.That(mergedReport.Tests[0].Duration, Is.EqualTo(9));

            mergedReport = report2.Union(report1) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
            Assert.That(mergedReport.Tests[0].Duration, Is.EqualTo(9));
        }

        [Test]
        public void MergesSameTestsResult()
        {
            DateTime now = DateTime.Now;
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))) { Result = "PASSED" });
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file2.cs", (10, 20))) { Result = "SKIPPED" });

            TestwiseCoverageReport mergedReport = report2.Union(report1) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
            Assert.That(mergedReport.Tests[0].Result, Is.EqualTo("PASSED"));

            mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
            Assert.That(mergedReport.Tests[0].Result, Is.EqualTo("PASSED"));
        }

        [Test]
        public void MergesSameTestsCoverageInDifferentFiles()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file2.cs", (10, 20))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
            Assert.That(mergedReport.Tests[0].UniformPath, Is.EqualTo("Test1"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files, Has.Count.EqualTo(2));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[0].FileName, Is.EqualTo("file1.cs"));
            Assert.That(mergedReport.Tests[0].CoverageByPath[0].Files[1].FileName, Is.EqualTo("file2.cs"));
        }

        [Test]
        public void MergesSameTestsCoverageInSameFiles()
        {
            TestwiseCoverageReport report1 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (10, 20))));
            TestwiseCoverageReport report2 = new TestwiseCoverageReport(new Test("Test1", new File("file1.cs", (30, 40))));

            TestwiseCoverageReport mergedReport = report1.Union(report2) as TestwiseCoverageReport;

            Assert.That(mergedReport.Tests, Has.Length.EqualTo(1));
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
                    Duration = 5,
                    Result = "PASSED",
                    Content = "43d743a8ef4389bc5",
                    Message = "Awesome!"
                }
            );

            Assert.That(report.ToString(), Is.EqualTo(@"
            {
                ""tests"": [
                    {
                        ""uniformPath"": ""Test/NotExecuted()"",
                        ""result"": ""SKIPPED"",
                        ""paths"": []
                    },
                    {
                        ""uniformPath"": ""Test/SomeTest()"",
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
                        ],
                        ""duration"": 5.0
                    }
                ]
            }".Replace(" ", "").Replace("\r\n", "")));
        }

        [Test]
        public void PartialReport()
        {
            TestwiseCoverageReport report = new TestwiseCoverageReport(true, new Test("Test/Method()"));
            Assert.That(report.ToString(), Is.EqualTo(@"
            {
                ""partial"": true,
                ""tests"": [
                    {
                        ""uniformPath"": ""Test/Method()"",
                        ""result"": ""SKIPPED"",
                        ""paths"": []
                    }
                ]
            }".Replace(" ", "").Replace("\r\n", "")));
        }
    }
}

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;
using UploadDaemon.Report.Testwise;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Scanning
{
    [TestFixture]
    public class TraceFileTest
    {
        [Test]
        public void DetectsEmptyFile()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            });

            Assert.That(traceFile.IsEmpty, Is.True);
        }

        [Test]
        public void DetectsNonEmptyFile()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:12345",
            });

            Assert.That(traceFile.IsEmpty, Is.False);
        }

        [Test]
        public void SupportsNewStyleMethodReferences()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:12345",
            });
            Trace trace = null;

            traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(1));
            Assert.That(trace.CoveredMethods[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(trace.CoveredMethods[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void SupportsInlinedJittedAndCalledEvents()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:123",
                "Jitted=2:456",
                "Called=2:789",
            });
            Trace trace = null;

            traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(3));
            Assert.That(trace.CoveredMethods.Select(m => m.Item2), Is.EquivalentTo(new[] { 123, 456, 789 }));
        }

        [Test]
        public void IgnoresMethodReferenceFromUnknownAssembly()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Inlined=1:12345",
            });
            Trace trace = null;

            traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(trace.CoveredMethods, Is.Empty);
        }

        [Test]
        public void ConvertsToAggregatedCoverageReport()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:12345",
            });
            Trace trace = null;

            ICoverageReport report = traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(report, Is.InstanceOf<SimpleCoverageReport>());
            Assert.That(trace.CoveredMethods, Is.EquivalentTo(new[] { ("ProfilerGUI", 12345) }));
        }

        [Test]
        public void ConvertsToTestwiseCoverageReport()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Info=TIA enabled. SUB: tcp://127.0.0.1:7145 REQ: tcp://127.0.0.1:7146",
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Test=Start:20200131_1109420123:TestCase1",
                "Inlined=2:12345",
                "Test=End:20200131_1109430456:PASSED",
                "Stopped=20200131_1109440000"
            });

            ICoverageReport report = traceFile.ToReport((Trace t) => new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                { "file1.cs", new FileCoverage((10,20)) }
            }, new List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)>()));

            Assert.That(report, Is.InstanceOf<TestwiseCoverageReport>());
            TestwiseCoverageReport testwiseReport = (TestwiseCoverageReport)report;
            Assert.That(testwiseReport.Tests, Has.Length.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().UniformPath, Is.EqualTo("TestCase1"));
            Assert.That(testwiseReport.Tests.First().Duration, Is.EqualTo(1.333d));
            Assert.That(testwiseReport.Tests.First().Result, Is.EqualTo("PASSED"));
            Assert.That(testwiseReport.Tests.First().CoverageByPath, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Path, Is.EqualTo(string.Empty));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files.First().FileName, Is.EqualTo("file1.cs"));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files.First().CoveredLines, Is.EqualTo("10-20"));
        }

        [Test]
        public void ConvertsToTestwiseCoverageReportMultipleTests()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Info=TIA enabled. SUB: tcp://127.0.0.1:7145 REQ: tcp://127.0.0.1:7146",
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Test=Start:20200131_1109420000:TestCase1",
                "Inlined=2:12345",
                "Test=End:20200131_1109430000:PASSED",
                "Test=Start:20200131_1109440000:TestCase2",
                "Inlined=2:67890",
                "Test=End:20200131_1109460000:FAILURE",
                "Stopped=20200131_1109440000"
            });

            ICoverageReport report = traceFile.ToReport((Trace t) => {
                if (t.CoveredMethods.Contains(("ProfilerGUI", 12345)) && t.CoveredMethods.Count == 1)
                {
                    return new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                        { "file1.cs", new FileCoverage((1,2)) }
                    }, new List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)>());
                }
                else if (t.CoveredMethods.Contains(("ProfilerGUI", 67890)) && t.CoveredMethods.Count == 1)
                {
                    return new SimpleCoverageReport(new Dictionary<string, FileCoverage>() {
                        { "file2.cs", new FileCoverage((1,2)) }
                    }, new List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)>());
                }

                throw new ArgumentException();
            });

            Assert.That(report, Is.InstanceOf<TestwiseCoverageReport>());
            TestwiseCoverageReport testwiseReport = (TestwiseCoverageReport)report;
            Assert.That(testwiseReport.Tests, Has.Length.EqualTo(2));
            Assert.That(testwiseReport.Tests.First().UniformPath, Is.EqualTo("TestCase1"));
            Assert.That(testwiseReport.Tests.First().Duration, Is.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().Result, Is.EqualTo("PASSED"));
            Assert.That(testwiseReport.Tests.First().CoverageByPath, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Path, Is.EqualTo(string.Empty));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files.First().FileName, Is.EqualTo("file1.cs"));
            Assert.That(testwiseReport.Tests[1].UniformPath, Is.EqualTo("TestCase2"));
            Assert.That(testwiseReport.Tests[1].Duration, Is.EqualTo(2));
            Assert.That(testwiseReport.Tests[1].Result, Is.EqualTo("FAILURE"));
            Assert.That(testwiseReport.Tests[1].CoverageByPath, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests[1].CoverageByPath.First().Path, Is.EqualTo(string.Empty));
            Assert.That(testwiseReport.Tests[1].CoverageByPath.First().Files, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests[1].CoverageByPath.First().Files.First().FileName, Is.EqualTo("file2.cs"));
        }

        [Test]
        public void ThrowsOnTestWithoutStart()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Info=TIA enabled. SUB: tcp://127.0.0.1:7145 REQ: tcp://127.0.0.1:7146",
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:12345",
                "Test=End:20200131_1109430456:SUCCESS",
                "Stopped=20200131_1109440000"
            });

            Exception exception = Assert.Throws<InvalidTraceFileException>(() =>
            {
                traceFile.ToReport((Trace t) => SomeSimpleCoverageReport());
            });

            Assert.That(exception.Message, Contains.Substring("encountered end of test that did not start"));
        }

        [Test]
        public void ConsidersTestWithoutEnd()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Info=TIA enabled. SUB: tcp://127.0.0.1:7145 REQ: tcp://127.0.0.1:7146",
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Test=Start:20200131_1109420000:TestCase1",
                "Inlined=2:12345",
                "Stopped=20200131_1109440000"
            });
            Trace trace = null;

            ICoverageReport report = traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(report, Is.InstanceOf<TestwiseCoverageReport>());
            TestwiseCoverageReport testwiseReport = (TestwiseCoverageReport)report;
            Assert.That(testwiseReport.Tests, Has.Length.EqualTo(1));
            Assert.That(testwiseReport.Tests[0].UniformPath, Is.EqualTo("TestCase1"));
            Assert.That(testwiseReport.Tests[0].Result, Is.EqualTo("SKIPPED"));
            Assert.That(testwiseReport.Tests[0].Duration, Is.EqualTo(2));
            Assert.That(trace.CoveredMethods, Contains.Item(("ProfilerGUI", 12345)));
        }

        [Test]
        public void ConsidersNoTestTrace()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Started=20200131_1109400000",
                "Info=TIA enabled. SUB: tcp://127.0.0.1:7145 REQ: tcp://127.0.0.1:7146",
                "Assembly=A:2 Version:1.0.0.0",
                "Inlined=2:123",
                "Test=Start:20200131_1109420000:TestCase1",
                "Inlined=2:12345",
                "Test=End:20200131_1109430000:PASSED",
                "Inlined=2:456",
                "Test=Start:20200131_1109440000:TestCase2",
                "Inlined=2:12345",
                "Test=End:20200131_1109450000:PASSED",
                "Inlined=2:789",
                "Stopped=20200131_1109460000"
            });
            Trace trace = null;

            ICoverageReport report = traceFile.ToReport((Trace t) => { trace = t; return SomeSimpleCoverageReport(); });

            Assert.That(report, Is.InstanceOf<TestwiseCoverageReport>());
            TestwiseCoverageReport testwiseReport = (TestwiseCoverageReport)report;
            Assert.That(testwiseReport.Tests, Has.Length.EqualTo(3));
            Assert.That(testwiseReport.Tests[2].UniformPath, Is.EqualTo("No Test"));
            Assert.That(testwiseReport.Tests[2].Result, Is.EqualTo("SKIPPED"));
            Assert.That(testwiseReport.Tests[2].Duration, Is.EqualTo(6));
            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(3));
            Assert.That(trace.CoveredMethods, Contains.Item(("A", 123)));
            Assert.That(trace.CoveredMethods, Contains.Item(("A", 456)));
            Assert.That(trace.CoveredMethods, Contains.Item(("A", 789)));
        }

        private SimpleCoverageReport SomeSimpleCoverageReport()
        {
            return new SimpleCoverageReport(new Dictionary<string, FileCoverage>(), new List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)>());
        }
    }
}

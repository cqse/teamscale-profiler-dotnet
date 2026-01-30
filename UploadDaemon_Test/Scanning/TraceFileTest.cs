using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Report;
using UploadDaemon.Report.Simple;
using UploadDaemon.Report.Testwise;

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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(3));
            Assert.That(trace.CoveredMethods.Select(m => m.Item2), Is.EquivalentTo(new[] { 123, 456, 789 }));
        }

        [Test]
        public void IgnoresMethodReferenceFromUnknownAssembly()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Inlined=1:12345",
            });

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

            Assert.That(trace.CoveredMethods, Is.EquivalentTo(new[] { ("ProfilerGUI", 12345) }));
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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            Exception exception = Assert.Throws<InvalidTraceFileException>(() =>
            {
                TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
                new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            ICoverageReport report = new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

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
            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            TraceCollectingLineCoverageSynthesizer traceCollector = new TraceCollectingLineCoverageSynthesizer();
            ICoverageReport report = new TraceFileParser(traceFile, extractor.Assemblies, traceCollector).ParseTraceFile();
            Trace trace = traceCollector.LastTrace;

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
            return new SimpleCoverageReport(new Dictionary<string, FileCoverage>());
        }
    }
}

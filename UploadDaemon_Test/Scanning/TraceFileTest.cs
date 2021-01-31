﻿using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Report;
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

            Assert.That(traceFile.FindCoveredMethods(), Has.Count.EqualTo(1));
            Assert.That(traceFile.FindCoveredMethods()[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(traceFile.FindCoveredMethods()[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void SupportsOldStyleMethodReferences()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:9876:12345",
            });

            Assert.That(traceFile.FindCoveredMethods(), Has.Count.EqualTo(1));
            Assert.That(traceFile.FindCoveredMethods()[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(traceFile.FindCoveredMethods()[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void IgnoresMethodReferenceFromUnknownAssembly()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[] {
                "Inlined=1:12345",
            });

            Assert.That(traceFile.FindCoveredMethods(), Is.Empty);
        }

        [Test]
        public void ConvertsToAggregatedCoverageReport()
        {
            TraceFile traceFile = new TraceFile(":path:", new string[]
            {
                "Assembly=ProfilerGUI:2 Version:1.0.0.0",
                "Inlined=2:9876:12345",
            });
            Trace trace = null;

            ICoverageReport report = traceFile.ToReport((Trace t) => { trace = t; return SomeLineCoverageReport(); });

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
                "Inlined=2:9876:12345",
                "Test=End:20200131_1109430456:SUCCESS",
            });

            ICoverageReport report = traceFile.ToReport((Trace t) => new LineCoverageReport(new Dictionary<string, FileCoverage>() {
                { "file1.cs", new FileCoverage((10,20)) }
            }));

            Assert.That(report, Is.InstanceOf<TestwiseCoverageReport>());
            TestwiseCoverageReport testwiseReport = (TestwiseCoverageReport)report;
            Assert.That(testwiseReport.Tests, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().UniformPath, Is.EqualTo("TestCase1"));
            Assert.That(testwiseReport.Tests.First().Duration, Is.EqualTo(1.333d));
            Assert.That(testwiseReport.Tests.First().Result, Is.EqualTo("SUCCESS"));
            Assert.That(testwiseReport.Tests.First().CoverageByPath, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Path, Is.EqualTo(string.Empty));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files, Has.Count.EqualTo(1));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files.First().FileName, Is.EqualTo("file1.cs"));
            Assert.That(testwiseReport.Tests.First().CoverageByPath.First().Files.First().CoveredLines, Is.EqualTo("10-20"));
        }

        private LineCoverageReport SomeLineCoverageReport()
        {
            return new LineCoverageReport(new Dictionary<string, FileCoverage>());
        }
    }
}

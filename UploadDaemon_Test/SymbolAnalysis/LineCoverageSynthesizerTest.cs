﻿using Cqse.ConQAT.Dotnet.Bummer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Configuration;
using UploadDaemon.Report.Simple;
using UploadDaemon.Scanning;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class LineCoverageSynthesizerTest
    {
        // 100663427 corresponds to MainViewModel#get_SelectedBitnessIndex in ProfilerGUI.pdb
        // obtained with cvdump.exe
        private const uint ExistingMethodToken = 100663427;

        [Test]
        public void TestSynthesizing()
        {
            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=nomatch:2 Version:1.0.0.0 Path:C:\bla\nomatch.dll",
                @"Inlined=2:{ExistingMethodToken}",
            });
            Trace trace = new Trace() { CoveredMethods = new[] { ("ProfilerGUI", ExistingMethodToken) }.ToList() };

            SimpleCoverageReport report = Convert(trace, traceFile, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            string sourceFilePath = @"\\VBOXSVR\proj\teamscale-profiler-dotnet\ProfilerGUI\Source\Configurator\MainViewModel.cs";
            Assert.That(report.FileNames, Is.EquivalentTo(new[] { sourceFilePath }));
            Assert.That(report[sourceFilePath], Is.EqualTo(new FileCoverage((37, 39))));
        }

        [Test]
        public void TracesWithoutCoverageShouldResultInEmptyReport()
        {
            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=nomatch:2 Version:1.0.0.0 Path:C:\bla\nomatch.dll",
                @"Inlined=2:{ExistingMethodToken}",
            });
            Trace trace = new Trace() { CoveredMethods = new List<(string, uint)>() };

            SimpleCoverageReport report = new LineCoverageSynthesizer().ConvertToLineCoverage(trace, traceFile, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }), new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>());
            Assert.That(report.IsEmpty, Is.True);
        }

        [Test]
        public void ExcludingAllPdbsShouldResultInException()
        {
            TraceFile traceFile = new TraceFile("coverage_1_1.txt", new[] {
                @"Assembly=nomatch:2 Version:1.0.0.0 Path:C:\bla\nomatch.dll",
                @"Inlined=2:{ExistingMethodToken}",
            });
            Trace trace = new Trace() { CoveredMethods = new[] { ("ProfilerGUI", ExistingMethodToken) }.ToList() };

            Exception exception = Assert.Throws<LineCoverageSynthesizer.LineCoverageConversionFailedException>(() =>
            {
                Convert(trace, traceFile, TestUtils.TestDataDirectory, new GlobPatternList(new List<string> { "xx" }, new List<string> { "*" }));
            });

            Assert.That(exception.Message, Contains.Substring("no symbols"));
        }

        [Test]
        public void CompilerHiddenLinesShouldBeIgnored()
        {
            Trace trace = new Trace() { CoveredMethods = new[] { ("Test", (uint) 1234) }.ToList() };

            AssemblyMethodMappings mappings = new AssemblyMethodMappings
            {
                AssemblyName = "Test",
                SymbolFileName = "Test.pdb",
            };
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = 1234,
                SourceFile = "",
                StartLine = 16707566,
                EndLine = 16707566,
            });
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = 1234,
                SourceFile = @"c:\some\file.cs",
                StartLine = 16707566,
                EndLine = 16707566,
            });

            SymbolCollection symbolCollection = new SymbolCollection(new List<AssemblyMethodMappings>() { mappings });

            SimpleCoverageReport coverage = LineCoverageSynthesizer.ConvertToLineCoverage(trace, symbolCollection, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }), new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>());

            Assert.That(coverage.IsEmpty, Is.True);
        }

        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static SimpleCoverageReport Convert(Trace trace, TraceFile traceFile, string symbolDirectory, GlobPatternList assemlyPatterns)
        {
            return new LineCoverageSynthesizer().ConvertToLineCoverage(trace, traceFile, symbolDirectory, assemlyPatterns, new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>());
        }
    }
}
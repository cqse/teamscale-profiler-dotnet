﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using Cqse.ConQAT.Dotnet.Bummer;
using UploadDaemon.Configuration;
using UploadDaemon.Scanning;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class LineCoverageSynthesizerTest
    {
        // 100663427 corresponds to MainViewModel#get_SelectedBitnessIndex in ProfilerGUI.pdb
        // obtained with cvdump.exe
        private static readonly uint ExistingMethodToken = 100663427;

        [Test]
        public void TestSynthesizing()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:{ExistingMethodToken}",
        });
            string coverageReport = Convert(traceFile, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(NormalizeNewLines(coverageReport.Trim()), Is.EqualTo(NormalizeNewLines(@"# isMethodAccurate=true
\\VBOXSVR\proj\teamscale-profiler-dotnet\ProfilerGUI\Source\Configurator\MainViewModel.cs
37-39")));
        }

        [Test]
        public void TracesWithoutCoverageShouldResultInEmptyTrace()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
        });

            LineCoverageReport report = new LineCoverageSynthesizer().ConvertToLineCoverage(traceFile, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));
            Assert.That(report.IsEmpty, Is.True);
        }

        [Test]
        public void ExcludingAllPdbsShouldResultInException()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:{ExistingMethodToken}",
        });

            Exception exception = Assert.Throws<LineCoverageSynthesizer.LineCoverageConversionFailedException>(() =>
            {
                LineCoverageSynthesizerTest.Convert(traceFile, TestUtils.TestDataDirectory, new GlobPatternList(new List<string> { "xx" }, new List<string> { "*" }));
            });

            Assert.That(exception.Message, Contains.Substring("no symbols"));
        }

        [Test]
        public void CompilerHiddenLinesShouldBeIgnored()
        {
            TraceFile traceFile = new TraceFile("coverage_12345_1234.txt", new string[] {
            "Assembly=Test:2 Version:1.0.0.0",
            "Inlined=2:1234",
        });

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

            LineCoverageReport coverage = LineCoverageSynthesizer.ConvertToLineCoverage(traceFile, symbolCollection, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(coverage.IsEmpty, Is.True);
        }

        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string Convert(TraceFile traceFile, string symbolDirectory, GlobPatternList assemlyPatterns)
        {
            return new LineCoverageSynthesizer().ConvertToLineCoverage(traceFile, symbolDirectory, assemlyPatterns).ToReportString();
        }
    }
}
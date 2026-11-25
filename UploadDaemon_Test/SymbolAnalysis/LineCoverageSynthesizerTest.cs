using Cqse.ConQAT.Dotnet.Bummer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UploadDaemon.Configuration;
using UploadDaemon.Report.Simple;
using UploadDaemon.Scanning;

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
            Trace trace = new Trace() { CoveredMethods = new[] { ("ProfilerGUI", ExistingMethodToken) }.ToList() };

            string sourceFilePath = @"\\VBOXSVR\proj\teamscale-profiler-dotnet\ProfilerGUI\Source\Configurator\MainViewModel.cs";


            AssemblyMethodMappings mappings = new AssemblyMethodMappings
            {
                AssemblyName = "ProfilerGUI",
                SymbolFileName = "Test.pdb",
            };
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = ExistingMethodToken,
                SourceFileMappings = { new SourceFileMapping
                {
                    SourceFile = sourceFilePath,
                    LineRanges = { new LineRange { StartLine = 37, EndLine = 39 } },
                } },
            });

            SimpleCoverageReport report = Convert(trace, new SymbolCollection(new List<AssemblyMethodMappings> { mappings }), TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(report.FileNames, Is.EquivalentTo(new[] { sourceFilePath }));
            Assert.That(report[sourceFilePath], Is.EqualTo(new FileCoverage((37, 39))));
        }

        [Test]
        public void MethodsWithMultipleLineRangesInMultipleSourceFilesShouldCoverEachRange()
        {
            Trace trace = new Trace() { CoveredMethods = new[] { ("Test", (uint) 1234) }.ToList() };

            AssemblyMethodMappings mappings = new AssemblyMethodMappings
            {
                AssemblyName = "Test",
                SymbolFileName = "Test.pdb",
            };
            // e.g. a constructor (lines 17-18) with an auto-property initializer (lines 37-38) in the same file and a field
            // initializer (line 10) in another file of a partial class
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = 1234,
                SourceFileMappings =
                {
                    new SourceFileMapping
                    {
                        SourceFile = "File.Fields.cs",
                        LineRanges = { new LineRange { StartLine = 10, EndLine = 10 } },
                    },
                    new SourceFileMapping
                    {
                        SourceFile = "File.cs",
                        LineRanges =
                        {
                            new LineRange { StartLine = 17, EndLine = 18 },
                            new LineRange { StartLine = 37, EndLine = 38 },
                        },
                    },
                },
            });

            SimpleCoverageReport report = Convert(trace, new SymbolCollection(new List<AssemblyMethodMappings> { mappings }), TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(report.FileNames, Is.EquivalentTo(new[] { "File.Fields.cs", "File.cs" }));
            Assert.That(report["File.Fields.cs"], Is.EqualTo(new FileCoverage((10, 10))));
            Assert.That(report["File.cs"], Is.EqualTo(new FileCoverage((17, 18), (37, 38))));
        }

        [Test]
        public void TracesWithoutCoverageShouldResultInEmptyReport()
        {
            Trace trace = new Trace() { CoveredMethods = new List<(string, uint)>() };

            // mapping content is irrelevant as only the empty trace file matters
            AssemblyMethodMappings mappings = new AssemblyMethodMappings
            {
                AssemblyName = "ProfilerGUI",
                SymbolFileName = "Test.pdb",
            };
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = ExistingMethodToken,
                SourceFileMappings = { new SourceFileMapping
                {
                    SourceFile = "File.cs",
                    LineRanges = { new LineRange { StartLine = 37, EndLine = 39 } },
                } },
            });

            SimpleCoverageReport report = Convert(trace, new SymbolCollection(new List<AssemblyMethodMappings> { mappings }), TestUtils.TestDataDirectory,
                            new GlobPatternList(new List<string> { "*" }, new List<string> { }));
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

            AssemblyExtractor extractor = new AssemblyExtractor();
            extractor.ExtractAssemblies(traceFile.Lines);

            Exception exception = Assert.Throws<LineCoverageSynthesizer.LineCoverageConversionFailedException>(() =>
            {
                Convert(trace, new SymbolCollection(new List<AssemblyMethodMappings>()), TestUtils.TestDataDirectory, new GlobPatternList(new List<string> { "xx" }, new List<string> { "*" }));
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
            // the method mappers provide no line ranges if all lines of a method are compiler hidden
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = 1234,
                SourceFileMappings = { new SourceFileMapping { SourceFile = "" } },
            });
            mappings.MethodMappings.Add(new MethodMapping
            {
                MethodToken = 1234,
                SourceFileMappings = { new SourceFileMapping { SourceFile = @"c:\some\file.cs" } },
            });

            SymbolCollection symbolCollection = new SymbolCollection(new List<AssemblyMethodMappings>() { mappings });

            SimpleCoverageReport coverage = LineCoverageSynthesizer.ConvertToLineCoverage(trace, symbolCollection, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(coverage.IsEmpty, Is.True);
        }

        private static SimpleCoverageReport Convert(Trace trace, SymbolCollection symbolCollection, string symbolDirectory, GlobPatternList assemlyPatterns)
        {
            return LineCoverageSynthesizer.ConvertToLineCoverage(trace, symbolCollection, symbolDirectory, assemlyPatterns);
        }
    }
}
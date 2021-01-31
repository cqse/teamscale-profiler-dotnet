using NUnit.Framework;
using System;
using System.Collections.Generic;
using Cqse.ConQAT.Dotnet.Bummer;
using UploadDaemon.Configuration;
using UploadDaemon.Scanning;
using System.Linq;

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

            string coverageReport = Convert(trace, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(NormalizeNewLines(coverageReport.Trim()), Is.EqualTo(NormalizeNewLines(@"# isMethodAccurate=true
\\VBOXSVR\proj\teamscale-profiler-dotnet\ProfilerGUI\Source\Configurator\MainViewModel.cs
37-39")));
        }

        [Test]
        public void TracesWithoutCoverageShouldResultInEmptyReport()
        {
            Trace trace = new Trace() { CoveredMethods = new List<(string, uint)>() };

            LineCoverageReport report = new LineCoverageSynthesizer().ConvertToLineCoverage(trace, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));
            Assert.That(report.IsEmpty, Is.True);
        }

        [Test]
        public void ExcludingAllPdbsShouldResultInException()
        {
            Trace trace = new Trace() { CoveredMethods = new[] { ("ProfilerGUI", ExistingMethodToken) }.ToList() };

            Exception exception = Assert.Throws<LineCoverageSynthesizer.LineCoverageConversionFailedException>(() =>
            {
                Convert(trace, TestUtils.TestDataDirectory, new GlobPatternList(new List<string> { "xx" }, new List<string> { "*" }));
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

            LineCoverageReport coverage = LineCoverageSynthesizer.ConvertToLineCoverage(trace, symbolCollection, TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "*" }, new List<string> { }));

            Assert.That(coverage.IsEmpty, Is.True);
        }

        private static string NormalizeNewLines(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string Convert(Trace trace, string symbolDirectory, GlobPatternList assemlyPatterns)
        {
            return new LineCoverageSynthesizer().ConvertToLineCoverage(trace, symbolDirectory, assemlyPatterns).ToReportString();
        }
    }
}
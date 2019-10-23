using NUnit.Framework;
using System.Collections.Generic;
using UploadDaemon.Configuration;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class SymbolCollectionTest
    {
        // 100663427 corresponds to MainViewModel#get_SelectedBitnessIndex in ProfilerGUI.pdb
        // obtained with cvdump.exe
        private static readonly uint ExistingMethodToken = 100663427;

        [Test]
        public void TestPdbParsing()
        {
            SymbolCollection collection = SymbolCollection.CreateFromPdbFiles(TestUtils.TestDataDirectory,
                new GlobPatternList(new List<string> { "ProfilerGUI" }, new List<string> { }));

            SymbolCollection.SourceLocation existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod, Is.Not.Null);
                Assert.That(collection.Resolve("does-not-exist", 123), Is.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.StartLine, Is.EqualTo(37));
                Assert.That(existingMethod.EndLine, Is.EqualTo(39));
            });
        }

        [Test]
        public void OneInvalidPdbShouldNotPreventParsingOthers()
        {
            SymbolCollection collection = SymbolCollection.CreateFromPdbFiles(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "Invalid", "ProfilerGUI" }, new List<string> { }));

            SymbolCollection.SourceLocation existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.That(existingMethod, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.StartLine, Is.EqualTo(37));
                Assert.That(existingMethod.EndLine, Is.EqualTo(39));
            });
        }

        [Test]
        public void DuplicatePdbsShouldNotThrowExceptions()
        {
            SymbolCollection collection = SymbolCollection.CreateFromPdbFiles(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "ProfilerGUI", "ProfilerGUICopy" }, new List<string> { }));

            SymbolCollection.SourceLocation existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.That(existingMethod, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.StartLine, Is.EqualTo(37));
                Assert.That(existingMethod.EndLine, Is.EqualTo(39));
            });
        }

        [Test]
        public void RespectsGlobPatterns()
        {
            SymbolCollection collection = SymbolCollection.CreateFromPdbFiles(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "*" }, new List<string> { "Profiler*" }));

            SymbolCollection.SourceLocation existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.That(existingMethod, Is.Null);
        }

        [Test]
        public void SearchesSubdirectories()
        {
            SymbolCollection collection = SymbolCollection.CreateFromPdbFiles(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "Sub" }, new List<string> { }));

            SymbolCollection.SourceLocation existingMethod = collection.Resolve("Sub", ExistingMethodToken);
            Assert.That(existingMethod, Is.Not.Null);
        }
    }
}
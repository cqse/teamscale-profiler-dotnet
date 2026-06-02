using NUnit.Framework;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class SymbolCollectionTest
    {
        private static readonly string TestPdbPath = $"{TestUtils.TestDataDirectory}\\ProfilerGUI.pdb";
        private static readonly string TestPDBCopyPath = $"{TestUtils.TestDataDirectory}\\ProfilerGUICopy.pdb";

        // 100663427 corresponds to MainViewModel#get_SelectedBitnessIndex in ProfilerGUI.pdb
        // obtained with cvdump.exe
        private const uint ExistingMethodToken = 100663427;

        [Test]
        public void TestPdbParsing()
        {
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { TestPdbPath });

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
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { $"{TestUtils.TestDataDirectory}\\Invalid.pdb", TestPdbPath });

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
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { TestPdbPath, TestPDBCopyPath });

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
        public void CollectsSymbolFilePaths()
        {
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { TestPdbPath });

            Assert.That(collection.SymbolFilePaths, Is.EqualTo(new[] { TestPdbPath }));
        }
    }
}
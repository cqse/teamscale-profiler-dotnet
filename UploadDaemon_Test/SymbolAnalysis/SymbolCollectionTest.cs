using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

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

            IReadOnlyList<SymbolCollection.SourceLocation> existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod, Is.Not.Null);
                Assert.That(collection.Resolve("profilergui", ExistingMethodToken), Is.SameAs(existingMethod));
                Assert.That(collection.Resolve("does-not-exist", 123), Is.Null);
            });

            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.Single().SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.Single().LineRanges, Is.EqualTo(new[] { (37u, 39u) }));
            });
        }

        [Test]
        public void ConstructorWithInitializerShouldBeMappedToMultipleLineRanges()
        {
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { TestPdbPath });

            // 100663307 corresponds to the constructor of Config in ProfilerGUI.pdb, which contains an auto-property initializer in line 161
            IReadOnlyList<SymbolCollection.SourceLocation> constructor = collection.Resolve("ProfilerGUI", 100663307);
            Assert.That(constructor, Is.Not.Null);
            Assert.That(constructor.Single().LineRanges, Is.EqualTo(new[] { (161u, 161u), (171u, 175u) }));
        }

        [Test]
        public void OneInvalidPdbShouldNotPreventParsingOthers()
        {
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { $"{TestUtils.TestDataDirectory}\\Invalid.pdb", TestPdbPath });

            IReadOnlyList<SymbolCollection.SourceLocation> existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.That(existingMethod, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.Single().SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.Single().LineRanges, Is.EqualTo(new[] { (37u, 39u) }));
            });
        }

        [Test]
        public void DuplicatePdbsShouldNotThrowExceptions()
        {
            SymbolCollection collection = SymbolCollection.CreateFromFiles(new[] { TestPdbPath, TestPDBCopyPath });

            IReadOnlyList<SymbolCollection.SourceLocation> existingMethod = collection.Resolve("ProfilerGUI", ExistingMethodToken);
            Assert.That(existingMethod, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(existingMethod.Single().SourceFile, Contains.Substring("Configurator\\MainViewModel.cs"));
                Assert.That(existingMethod.Single().LineRanges, Is.EqualTo(new[] { (37u, 39u) }));
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
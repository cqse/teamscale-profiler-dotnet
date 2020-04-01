using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UploadDaemon.Configuration;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    class SymbolCollectionResolverTest
    {
        private readonly GlobPatternList someAssemblyPatterns = new GlobPatternList(new List<string> { "*" }, new List<string> { });

        private SymbolCollectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new SymbolCollectionResolver();
        }

        [Test]
        public void CollectsSymbolFileNames()
        {
            SymbolCollection collection = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            Assert.That(Normalized(collection.SymbolFileNames), Is.EqualTo(new[] { "\\test-data\\UploadDaemon.SymbolAnalysis.SymbolCollectionResolverTest\\Some.pdb" }));
        }

        [Test]
        public void CachesCollection()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);
            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            Assert.That(collection1, Is.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileChanges()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            SimulateSymbolFileChange(collection1.SymbolFileNames.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void CachesCollectionAgainAfterInvalidation()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            SimulateSymbolFileChange(collection1.SymbolFileNames.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);
            SymbolCollection collection3 = resolver.ResolveFrom(TestUtils.TestDataDirectory, someAssemblyPatterns);

            Assert.That(collection2, Is.SameAs(collection3));
        }

        /// <summary>
        /// Removes user-specific path prefix from PDB file names in test-data.
        /// </summary>
        private IEnumerable<string> Normalized(ICollection<string> symbolFileNames)
        {
            return symbolFileNames.Select(symbolFileName => symbolFileName.Substring(symbolFileName.IndexOf("\\test-data\\")));
        }

        private void SimulateSymbolFileChange(string symbolFileName)
        {
            DateTime originalLastWriteDate = File.GetLastAccessTime(symbolFileName);
            DateTime newLastWriteDate = originalLastWriteDate.AddSeconds(1);
            File.SetLastWriteTime(symbolFileName, newLastWriteDate);
        }
    }
}

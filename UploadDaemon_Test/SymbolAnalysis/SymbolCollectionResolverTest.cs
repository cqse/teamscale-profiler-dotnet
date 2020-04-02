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
        private readonly GlobPatternList includeAllAssembliesPattern = new GlobPatternList(new List<string> { "*" }, new List<string> { });
        private readonly string testSymbolFileName = $"{TestUtils.TestDataDirectory}\\Some.pdb";

        private SymbolCollectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new SymbolCollectionResolver();
            File.SetLastWriteTime($"{TestUtils.TestDataDirectory}\\Some.pdb", new DateTime(2019, 1, 1));
        }

        [Test]
        public void CollectsSymbolFileNames()
        {
            SymbolCollection collection = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection.SymbolFilePaths, Is.EqualTo(new[] { testSymbolFileName }));
        }

        [Test]
        public void CachesCollection()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);
            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileChanges()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void CachesCollectionAgainAfterInvalidation()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);
            SymbolCollection collection3 = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection2, Is.SameAs(collection3));
        }

        private void SimulateSymbolFileChange(string symbolFileName)
        {
            DateTime originalLastWriteDate = File.GetLastAccessTime(symbolFileName);
            DateTime newLastWriteDate = originalLastWriteDate.AddDays(1);
            File.SetLastWriteTime(symbolFileName, newLastWriteDate);
        }
    }
}

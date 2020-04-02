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
        private static readonly string TestDataDirectory = $"{TestUtils.TestDataDirectory}\\Tmp";
        private static readonly string TestSymbolFileName = $"{TestDataDirectory}\\Some.pdb";

        private readonly GlobPatternList includeAllAssembliesPattern = new GlobPatternList(new List<string> { "*" }, new List<string> { });

        private SymbolCollectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new SymbolCollectionResolver();

            Directory.CreateDirectory(TestDataDirectory);
            File.Copy($"{TestUtils.TestDataDirectory}\\Some.pdb", TestSymbolFileName);
            File.SetLastWriteTime(TestSymbolFileName, new DateTime(2019, 1, 1));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TestDataDirectory, true);
        }

        [Test]
        public void CollectsSymbolFileNames()
        {
            SymbolCollection collection = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection.SymbolFilePaths, Is.EqualTo(new[] { TestSymbolFileName }));
        }

        [Test]
        public void CachesCollection()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);
            SymbolCollection collection2 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileIsAdded()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            File.Copy(TestSymbolFileName, $"{TestDataDirectory}\\SomeNew.pdb");

            SymbolCollection collection2 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileChanges()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileIsDeleted()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            File.Delete(TestSymbolFileName);

            SymbolCollection collection2 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void CachesCollectionAgainAfterInvalidation()
        {
            SymbolCollection collection1 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);
            SymbolCollection collection3 = resolver.ResolveFrom(TestDataDirectory, includeAllAssembliesPattern);

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

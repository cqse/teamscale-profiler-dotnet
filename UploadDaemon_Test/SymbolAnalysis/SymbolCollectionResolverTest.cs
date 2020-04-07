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
        private static readonly string TestSymbolFilePath = $"{TestDataDirectory}\\Some.pdb";
        private static readonly GlobPatternList includeAllAssembliesPattern = new GlobPatternList(new List<string> { "*" }, new List<string> { });

        private SymbolCollectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new SymbolCollectionResolver();

            Directory.CreateDirectory(TestDataDirectory);
            File.Copy($"{TestUtils.TestDataDirectory}\\Some.pdb", TestSymbolFilePath);
            File.SetLastWriteTime(TestSymbolFilePath, new DateTime(2019, 1, 1));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TestDataDirectory, true);
        }

        [Test]
        public void ConsidersSymbolFileIncludes()
        {
            SymbolCollection collection = resolver.ResolveFrom(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "DoesNotExist*" }, new List<string> {}));

            Assert.That(collection.IsEmpty, Is.True);
        }

        [Test]
        public void ConsidersSymbolFileExcludes()
        {
            SymbolCollection collection = resolver.ResolveFrom(TestUtils.TestDataDirectory,
                   new GlobPatternList(new List<string> { "*" }, new List<string> { "So*" }));

            Assert.That(collection.IsEmpty, Is.True);
        }

        [Test]
        public void IncludesSymbolFilesFromSubDirectories()
        {
            var testDataSubDirectory = $"{TestDataDirectory}\\Sub";
            Directory.CreateDirectory(testDataSubDirectory);
            File.Move(TestSymbolFilePath, $"{testDataSubDirectory}\\Some.pdb");

            SymbolCollection collection = resolver.ResolveFrom(TestUtils.TestDataDirectory, includeAllAssembliesPattern);

            Assert.That(collection.IsEmpty, Is.False);
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

            File.Copy(TestSymbolFilePath, $"{TestDataDirectory}\\SomeNew.pdb");

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

            File.Delete(TestSymbolFilePath);

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

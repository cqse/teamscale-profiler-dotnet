﻿using NUnit.Framework;
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
        private static readonly string TestSymbolDirectory = Path.Combine(Path.GetTempPath(), TestUtils.SolutionRoot.FullName, TestUtils.GetSanitizedTestClassName());
        private static readonly string TestSymbolFilePath = $"{TestSymbolDirectory}\\Some.pdb";
        private static readonly GlobPatternList includeAllAssembliesPattern = new GlobPatternList(new List<string> { "*" }, new List<string> { });

        private SymbolCollectionResolver resolver;

        [SetUp]
        public void SetUp()
        {
            resolver = new SymbolCollectionResolver();

            Directory.CreateDirectory(TestSymbolDirectory);
            File.Copy($"{TestUtils.TestDataDirectory}\\Some.pdb", TestSymbolFilePath);
            File.SetLastWriteTime(TestSymbolFilePath, new DateTime(2019, 1, 1));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(TestSymbolDirectory, true);
        }

        [Test]
        public void ConsidersSymbolFileIncludes()
        {
            SymbolCollection collection = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory,
                   new GlobPatternList(new List<string> { "DoesNotExist*" }, new List<string> {}));

            Assert.That(collection.IsEmpty, Is.True);
        }

        [Test]
        public void ConsidersSymbolFileExcludes()
        {
            SymbolCollection collection = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory,
                   new GlobPatternList(new List<string> { "*" }, new List<string> { "So*" }));

            Assert.That(collection.IsEmpty, Is.True);
        }

        [Test]
        public void IncludesSymbolFilesFromSubDirectories()
        {
            var testSubSymbolDirectory = $"{TestSymbolDirectory}\\Sub";
            Directory.CreateDirectory(testSubSymbolDirectory);
            File.Move(TestSymbolFilePath, $"{testSubSymbolDirectory}\\Some.pdb");

            SymbolCollection collection = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            Assert.That(collection.IsEmpty, Is.False);
        }

        [Test]
        public void CachesCollection()
        {
            SymbolCollection collection1 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);
            SymbolCollection collection2 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileIsAdded()
        {
            SymbolCollection collection1 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            File.Copy(TestSymbolFilePath, $"{TestSymbolDirectory}\\SomeNew.pdb");

            SymbolCollection collection2 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileChanges()
        {
            SymbolCollection collection1 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void InvalidatesCacheIfSymbolFileIsDeleted()
        {
            SymbolCollection collection1 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            File.Delete(TestSymbolFilePath);

            SymbolCollection collection2 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            Assert.That(collection1, Is.Not.SameAs(collection2));
        }

        [Test]
        public void CachesCollectionAgainAfterInvalidation()
        {
            SymbolCollection collection1 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

            SimulateSymbolFileChange(collection1.SymbolFilePaths.First());

            SymbolCollection collection2 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);
            SymbolCollection collection3 = resolver.ResolveFromSymbolDirectory(TestSymbolDirectory, includeAllAssembliesPattern);

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

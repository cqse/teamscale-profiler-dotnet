using System;
using System.Collections.Generic;
using UploadDaemon.Configuration;
using System.Linq;
using System.IO;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// This resolver loads symbol collections (PDB files) into <see cref="SymbolCollection"/>s.
    /// It uses a cache to avoid unnecessary disk I/O from reading the same PDB files repeatedly.
    /// The cache is invalidated when the last modified date of one of the PDBs considered for a
    /// collection changes.
    /// </summary>
    public class SymbolCollectionResolver
    {
        private readonly IDictionary<(string, GlobPatternList), SymbolCollection> symbolCollectionsCache =
            new Dictionary<(string, GlobPatternList), SymbolCollection>();

        private readonly IDictionary<string, DateTime> symbolFileLastWriteDates =
            new Dictionary<string, DateTime>();

        /// <summary>
        /// Resolves the <see cref="SymbolCollection"/> from the given path filtered by the given assembly patterns.
        /// The result is cached, to reduce disk I/O. Caching is invalidated when the last modified date of one of the
        /// PDBs changed.
        /// </summary>
        public SymbolCollection ResolveFrom(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            (string, GlobPatternList) key = GetCacheKey(symbolDirectory, assemblyPatterns);
            if (!symbolCollectionsCache.TryGetValue(key, out SymbolCollection collection) || !IsValid(collection))
            {
                symbolCollectionsCache[key] = collection = SymbolCollection.CreateFromPdbFiles(symbolDirectory, assemblyPatterns);
                UpdateValidationInfo(collection);
            }
            return collection;
        }

        private static (string, GlobPatternList) GetCacheKey(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            return (symbolDirectory, assemblyPatterns);
        }

        private bool IsValid(SymbolCollection collection)
        {
            return collection.SymbolFileNames.All(symbolFileName => ReadLastWriteDate(symbolFileName).Equals(symbolFileLastWriteDates[symbolFileName]));
        }

        private void UpdateValidationInfo(SymbolCollection collection)
        {
            foreach (string symbolFileName in collection.SymbolFileNames)
            {
                symbolFileLastWriteDates[symbolFileName] = ReadLastWriteDate(symbolFileName);
            }
        }

        private static DateTime ReadLastWriteDate(string symbolFileName) => File.GetLastWriteTimeUtc(symbolFileName);
    }
}

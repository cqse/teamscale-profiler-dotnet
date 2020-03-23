using System;
using System.Collections.Generic;
using UploadDaemon.Configuration;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// This resolver loads symbol collections (PDB files) into <see cref="SymbolCollection"/>s.
    /// It uses a cache to avoid unnecessary I/O from reading the same PDB files repeatedly.
    /// </summary>
    public class SymbolCollectionResolver
    {
        private readonly IDictionary<Tuple<string, GlobPatternList>, SymbolCollection> symbolCollectionsCache =
            new Dictionary<Tuple<string, GlobPatternList>, SymbolCollection>();

        public SymbolCollection ResolveFrom(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            Tuple<string, GlobPatternList> key = GetCacheKey(symbolDirectory, assemblyPatterns);
            if (!symbolCollectionsCache.ContainsKey(key))
            {
                symbolCollectionsCache[key] = SymbolCollection.CreateFromPdbFiles(symbolDirectory, assemblyPatterns);
            }
            return symbolCollectionsCache[key];
        }

        private static Tuple<string, GlobPatternList> GetCacheKey(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            return (symbolDirectory, assemblyPatterns).ToTuple();
        }
    }
}

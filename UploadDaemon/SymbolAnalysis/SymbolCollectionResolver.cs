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
    /// The cache is invalidated when a new symbol file is added, the last write date of a symbol
    /// file changes, or a symbol file is deleted.
    /// </summary>
    public class SymbolCollectionResolver
    {
        private class SymbolFileInfo
        {
            internal string Path;
            internal DateTime LastWriteTime;
        }

        private readonly IDictionary<(string, GlobPatternList), SymbolCollection> symbolCollectionsCache =
            new Dictionary<(string, GlobPatternList), SymbolCollection>();

        private readonly IDictionary<string, DateTime> symbolFileLastWriteDates =
            new Dictionary<string, DateTime>();

        /// <summary>
        /// Resolves a <see cref="SymbolCollection"/> from the PDB files in the given symbol directory whose file names
        /// without extension match the given pattern list.
        /// 
        /// The result is cached, to reduce disk I/O. Caching is invalidated when a new symbol file is added, the last
        /// write date of a symbol file changes, or a symbol file is deleted.
        ///
        /// May throw exceptions if, e.g., the symbol directory cannot be read.
        /// </summary>
        public SymbolCollection ResolveFrom(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            (string, GlobPatternList) key = GetCacheKey(symbolDirectory, assemblyPatterns);
            ICollection<SymbolFileInfo> relevantSymbolFiles = FindRelevantSymbolFiles(symbolDirectory, assemblyPatterns);
            if (!symbolCollectionsCache.TryGetValue(key, out SymbolCollection collection) || !IsValid(collection, relevantSymbolFiles))
            {
                symbolCollectionsCache[key] = collection = SymbolCollection.CreateFromFiles(relevantSymbolFiles.Select(info => info.Path));
                UpdateValidationInfo(relevantSymbolFiles);
            }
            return collection;
        }

        private static (string, GlobPatternList) GetCacheKey(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            return (symbolDirectory, assemblyPatterns);
        }

        private bool IsValid(SymbolCollection collection, ICollection<SymbolFileInfo> relevantSymbolFiles)
        {
            return ContainsExactly(collection, relevantSymbolFiles)
                && relevantSymbolFiles.All(symbolFile => symbolFile.LastWriteTime.Equals(symbolFileLastWriteDates[symbolFile.Path]));
        }

        private static bool ContainsExactly(SymbolCollection collection, ICollection<SymbolFileInfo> relevantSymbolFilePaths)
        {
            return collection.SymbolFilePaths.SetEquals(relevantSymbolFilePaths.Select(info => info.Path));
        }

        private void UpdateValidationInfo(ICollection<SymbolFileInfo> relevantSymbolFiles)
        {
            foreach (SymbolFileInfo symbolFile in relevantSymbolFiles)
            {
                symbolFileLastWriteDates[symbolFile.Path] = symbolFile.LastWriteTime;
            }
        }

        private static ICollection<SymbolFileInfo> FindRelevantSymbolFiles(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            return new HashSet<SymbolFileInfo>(Directory.EnumerateFiles(symbolDirectory, "*.pdb", SearchOption.AllDirectories)
                .Where(file => assemblyPatterns.Matches(Path.GetFileNameWithoutExtension(file)))
                .Select(file => new SymbolFileInfo { Path = file, LastWriteTime = File.GetLastWriteTimeUtc(file) }));
        }
    }
}

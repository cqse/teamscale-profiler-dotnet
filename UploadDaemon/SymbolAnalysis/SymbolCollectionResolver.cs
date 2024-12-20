﻿using System;
using System.Collections.Generic;
using UploadDaemon.Configuration;
using System.Linq;
using System.IO;
using NLog;

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
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

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
        /// Resolves the symbol collection either from PDBs stored in the passed symbol directory or the assembly directory.
        /// </summary>
        internal SymbolCollection Resolve(ParsedTraceFile traceFile, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            if (Config.IsAssemblyRelativePath(symbolDirectory))
            {
                logger.Debug("Resolve PDBs from assembly directory");
                return ResolveFromTraceFile(traceFile, symbolDirectory, assemblyPatterns);
            }

            logger.Debug($"Resolve PDBs from {symbolDirectory}");
            return ResolveFromSymbolDirectory(symbolDirectory, assemblyPatterns);
        }

        /// <summary>
        /// Resolves a <see cref="SymbolCollection"/> from the PDB files in the given symbol directory whose file names
        /// without extension match the given pattern list.
        /// 
        /// The result is cached, to reduce disk I/O. Caching is invalidated when a new symbol file is added, the last
        /// write date of a symbol file changes, or a symbol file is deleted.
        ///
        /// May throw exceptions if, e.g., the symbol directory cannot be read.
        /// </summary>
        public SymbolCollection ResolveFromSymbolDirectory(string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            (string, GlobPatternList) cacheKey = GetCacheKey(symbolDirectory, assemblyPatterns);
            ICollection<SymbolFileInfo> relevantSymbolFiles = FindRelevantSymbolFiles(symbolDirectory, assemblyPatterns);
            if (!symbolCollectionsCache.TryGetValue(cacheKey, out SymbolCollection collection) || !IsValid(collection, relevantSymbolFiles))
            {
                symbolCollectionsCache[cacheKey] = collection = SymbolCollection.CreateFromFiles(relevantSymbolFiles.Select(info => info.Path));
                UpdateValidationInfo(relevantSymbolFiles);
            }
            return collection;
        }

        /// <summary>
        /// Resolves the symbol collection from PDBs loaded relative to all loaded assemblies. The result is cached per assembly.
        /// </summary>
        public SymbolCollection ResolveFromTraceFile(ParsedTraceFile traceFile, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            List<SymbolCollection> collectionOfAllAssemblies = new List<SymbolCollection>();
            foreach ((string name, string path) assembly in traceFile.LoadedAssemblies.Where(assembly => MatchesAssemblyPattern(assemblyPatterns, assembly.name)))
            {
                string pdbPath = Path.Combine(Config.ResolveAssemblyRelativePath(symbolDirectory, assembly.path), Path.GetFileNameWithoutExtension(assembly.path) + ".pdb");
                if (!File.Exists(pdbPath))
                {
                    logger.Debug("No PDB found for assembly {assembly}", assembly.path);
                    continue;
                }

                (string, GlobPatternList) cacheKey = GetCacheKey(pdbPath, null); // assembly patterns are irrelevant for single assemblies
                SymbolFileInfo[] symbolFile = new[] { new SymbolFileInfo { Path = pdbPath, LastWriteTime = File.GetLastWriteTimeUtc(pdbPath) } };
                if (!symbolCollectionsCache.TryGetValue(cacheKey, out SymbolCollection collection) || !IsValid(collection, symbolFile))
                {
                    symbolCollectionsCache[cacheKey] = collection = SymbolCollection.CreateFromFiles(new[] { pdbPath });
                    UpdateValidationInfo(symbolFile);
                }
                collectionOfAllAssemblies.Add(collection);
            }

            return SymbolCollection.Merge(collectionOfAllAssemblies);
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
                .Where(file => MatchesAssemblyPattern(assemblyPatterns, Path.GetFileNameWithoutExtension(file)))
                .Select(file => new SymbolFileInfo { Path = file, LastWriteTime = File.GetLastWriteTimeUtc(file) }));
        }

        private static bool MatchesAssemblyPattern(GlobPatternList assemblyPatterns, string file)
        {
            bool isMatch = assemblyPatterns.Matches(file);
            logger.Trace("Testing {file} against {assemblyPatterns}: match is {isMatch}", file, assemblyPatterns.Describe(), isMatch);
            return isMatch;
        }
    }
}

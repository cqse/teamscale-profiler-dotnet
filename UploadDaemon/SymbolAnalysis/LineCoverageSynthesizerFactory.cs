using UploadDaemon.Configuration;
using UploadDaemon.Scanning;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Creates real <see cref="LineCoverageSynthesizer"/>s that resolve line coverage from PDB files.
    /// </summary>
    public class LineCoverageSynthesizerFactory : ILineCoverageSynthesizerFactory
    {
        public ILineCoverageSynthesizer Create(AssemblyExtractor assemblyExtractor, string symbolDirectory, GlobPatternList assemblyPatterns)
        {
            return new LineCoverageSynthesizer(assemblyExtractor, symbolDirectory, assemblyPatterns);
        }
    }
}

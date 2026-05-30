using UploadDaemon.Configuration;
using UploadDaemon.Scanning;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Creates <see cref="ILineCoverageSynthesizer"/>s for a given assembly extractor, symbol directory and
    /// assembly pattern list. This indirection exists so that the line coverage synthesis can be replaced in tests.
    /// </summary>
    public interface ILineCoverageSynthesizerFactory
    {
        ILineCoverageSynthesizer Create(AssemblyExtractor assemblyExtractor, string symbolDirectory, GlobPatternList assemblyPatterns);
    }
}

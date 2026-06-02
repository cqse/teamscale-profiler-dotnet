using System.Collections.Generic;
using UploadDaemon.Report.Simple;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Scanning
{
    internal class TraceCollectingLineCoverageSynthesizer : ILineCoverageSynthesizer
    {
        public Trace LastTrace { get; set; }

        public SimpleCoverageReport ConvertToLineCoverage(Trace trace)
        {
            LastTrace = trace;

            return new SimpleCoverageReport(new Dictionary<string, FileCoverage>());
        }
    }
}

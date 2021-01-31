using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report
{
    /// <summary>
    /// A Teamscale simple coverage report.
    /// </summary>
    public class SimpleCoverageReport :ICoverageReport
    {
        private readonly LineCoverageReport coverage;

        public SimpleCoverageReport(LineCoverageReport coverage)
        {
            this.coverage = coverage;
        }
    }
}

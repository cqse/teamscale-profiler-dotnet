using System;
using System.Collections.Generic;
using System.Text;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report
{
    /// <summary>
    /// A Teamscale simple coverage report.
    /// </summary>
    public class SimpleCoverageReport : ICoverageReport
    {
        private readonly LineCoverageReport coverage;

        public SimpleCoverageReport(LineCoverageReport coverage)
        {
            this.coverage = coverage;
        }

        public bool IsEmpty => coverage.IsEmpty;

        public string FileExtension => "simple";

        /// <inheritDoc/>
        public ICoverageReport UnionWith(ICoverageReport otherReport)
        {
            if (!(otherReport is SimpleCoverageReport other))
            {
                throw new NotSupportedException();
            }

            LineCoverageReport union = new LineCoverageReport(new Dictionary<string, FileCoverage>());
            union.UnionWith(this.coverage);
            union.UnionWith(other.coverage);
            return new SimpleCoverageReport(union);
        }

        /// <summary>
        /// Converts this report into a SIMPLE format report for Teamscale.
        /// </summary>
        public override string ToString()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("# isMethodAccurate=true");
            foreach (string file in coverage.FileNames)
            {
                report.AppendLine(file);
                foreach ((uint startLine, uint endLine) in coverage[file].CoveredLineRanges)
                {
                    report.AppendLine($"{startLine}-{endLine}");
                }
            }
            return report.ToString();
        }
    }
}

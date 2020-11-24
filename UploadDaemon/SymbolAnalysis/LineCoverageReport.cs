using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// A line-coverage report.
    /// </summary>
    public class LineCoverageReport
    {
        private readonly IDictionary<string, FileCoverage> lineCoverage;

        public LineCoverageReport(IDictionary<string, FileCoverage> lineCoverage)
        {
            this.lineCoverage = lineCoverage;
        }

        public bool IsEmpty
        {
            get => lineCoverage.Count == 0 || lineCoverage.Values.All(fileCoverage => fileCoverage.CoveredLineRanges.Count() == 0);
        }

        /// <summary>
        /// The names of the files for which this report contains coverage data.
        /// </summary>
        public ICollection<string> FileNames
        {
            get => lineCoverage.Keys;
        }

        /// <summary>
        /// Returns the line coverage that this reports contains for a given file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public FileCoverage this[string fileName]
        {
            get => lineCoverage[fileName];
        }

        /// <summary>
        /// Converts the given line coverage (covered line ranges per file) into a SIMPLE format report for Teamscale.
        /// </summary>
        public string ToReportString()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("# isMethodAccurate=true");
            foreach (string file in lineCoverage.Keys)
            {
                report.AppendLine(file);
                foreach ((uint startLine, uint endLine) in lineCoverage[file].CoveredLineRanges)
                {
                    report.AppendLine($"{startLine}-{endLine}");
                }
            }
            return report.ToString();
        }

        /// <summary>
        /// Modifies the current <see cref="LineCoverageReport"/> to contain the coverage from this report and the other report.
        /// </summary>
        public void UnionWith(LineCoverageReport otherReport)
        {
            foreach (string file in otherReport.FileNames)
            {
                if (!lineCoverage.TryGetValue(file, out FileCoverage fileCoverage))
                {
                    fileCoverage = new FileCoverage();
                    lineCoverage[file] = fileCoverage;
                }
                fileCoverage.UnionWith(otherReport[file]);
            }
        }
    }
}

using System.Collections.Generic;
using System.Linq;

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

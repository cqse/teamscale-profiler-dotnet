using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report
{
    /// <summary>
    /// A Teamscale simple coverage report.
    /// </summary>
    public class SimpleCoverageReport : ICoverageReport
    {
        private readonly IDictionary<string, FileCoverage> lineCoverageByFile;

        public SimpleCoverageReport(IDictionary<string, FileCoverage> lineCoverageByFile)
        {
            this.lineCoverageByFile = lineCoverageByFile;
        }

        /// <inheritDoc/>
        public bool IsEmpty => lineCoverageByFile.Count == 0 || lineCoverageByFile.Values.All(fileCoverage => fileCoverage.CoveredLineRanges.Count() == 0);

        /// <inheritDoc/>
        public string FileExtension => "simple";

        /// <inheritDoc/>
        public string UploadFormat => "SIMPLE";

        /// <summary>
        /// The names of the files for which this report contains coverage data.
        /// </summary>
        public ICollection<string> FileNames
        {
            get => lineCoverageByFile.Keys;
        }

        /// <summary>
        /// Returns the line coverage that this reports contains for a given file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public FileCoverage this[string fileName]
        {
            get => lineCoverageByFile[fileName];
        }

        /// <inheritDoc/>
        public ICoverageReport Union(ICoverageReport otherReport)
        {
            if (!(otherReport is SimpleCoverageReport other))
            {
                throw new NotSupportedException();
            }

            return new SimpleCoverageReport(new[] { lineCoverageByFile, other.lineCoverageByFile }.SelectMany(dict => dict)
                .ToLookup(pair => pair.Key, pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.Aggregate((fc1, fc2) => new FileCoverage(fc1.CoveredLineRanges.Union(fc2.CoveredLineRanges)))));
        }

        /// <summary>
        /// Converts this report into a SIMPLE format report for Teamscale.
        /// </summary>
        public override string ToString()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("# isMethodAccurate=true");
            foreach (string file in FileNames)
            {
                report.AppendLine(file);
                foreach ((uint startLine, uint endLine) in this[file].CoveredLineRanges)
                {
                    report.AppendLine($"{startLine}-{endLine}");
                }
            }
            return report.ToString();
        }
    }
}

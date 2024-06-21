using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report.Simple
{
    /// <summary>
    /// A Teamscale simple coverage report.
    /// </summary>
    public class SimpleCoverageReport : ICoverageReport
    {
        private const int MAX_REPORT_STRING_SIZE = 536_870_912;

        private readonly IDictionary<string, FileCoverage> lineCoverageByFile;

        public List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)> EmbeddedUploadTargets { get; }

        public SimpleCoverageReport(IDictionary<string, FileCoverage> lineCoverageByFile, List<(string project, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets)
        {
            this.lineCoverageByFile = lineCoverageByFile;
            EmbeddedUploadTargets = embeddedUploadTargets;
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
                .ToDictionary(group => group.Key, group => group.Aggregate((fc1, fc2) => new FileCoverage(fc1.CoveredLineRanges.Union(fc2.CoveredLineRanges)))), EmbeddedUploadTargets);
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

        /// <summary>
        /// Creates a list containing one or more simple coverage reports for this report.
        /// Reports are split to avoid too large strings (around 1GB) which cause OutOfMemoryErrors.
        /// </summary>
        public List<string> ToStringList()
        {
            List<String> result = new List<string>();

            StringBuilder report = new StringBuilder();
            bool newReport = true;
            foreach (string file in FileNames)
            {
                if (newReport)
                {
                    report.AppendLine("# isMethodAccurate=true");
                    newReport = false;
                }

                report.AppendLine(file);
                foreach ((uint startLine, uint endLine) in this[file].CoveredLineRanges)
                {
                    report.AppendLine($"{startLine}-{endLine}");
                }

                if (report.Length > MAX_REPORT_STRING_SIZE)
                {
                    result.Add(report.ToString());
                    report = new StringBuilder();
                    newReport = true;
                }
            }
            result.Add(report.ToString());
            return result;
        }

    }
}

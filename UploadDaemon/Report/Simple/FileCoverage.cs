using System.Collections.Generic;

namespace UploadDaemon.Report.Simple
{
    /// <summary>
    /// The line coverage collected for one file.
    /// </summary>
    public class FileCoverage
    {
        /// <summary>
        /// The ranges of inclusive start and end lines that are covered in the file.
        /// </summary>
        public HashSet<(uint, uint)> CoveredLineRanges { get; } = new HashSet<(uint, uint)>();

        public FileCoverage(params (uint, uint)[] lineRanges) : this((IEnumerable<(uint, uint)>)lineRanges) {}

        public FileCoverage(IEnumerable<(uint, uint)> lineRanges)
        {
            foreach ((uint, uint) range in lineRanges)
            {
                CoveredLineRanges.Add(range);
            }
        }

        /// <summary>
        /// Modifies the current <see cref="FileCoverage"/> to contain all the coverage from the both itself and the other <see cref="FileCoverage"/>.
        /// </summary>
        public void UnionWith(FileCoverage otherCoverage)
        {
            CoveredLineRanges.UnionWith(otherCoverage.CoveredLineRanges);
        }

        public override bool Equals(object obj) =>
            obj is FileCoverage fileCoverage && fileCoverage.CoveredLineRanges.SetEquals(CoveredLineRanges);

        public override int GetHashCode() =>
            CoveredLineRanges.GetHashCode();

        public override string ToString() =>
            $"FileCoverage[{string.Join(", ", CoveredLineRanges)}]";
    }
}

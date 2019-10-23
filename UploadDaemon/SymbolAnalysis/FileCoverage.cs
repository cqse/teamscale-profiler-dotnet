using System.Collections.Generic;

namespace UploadDaemon.SymbolAnalysis
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

        public FileCoverage(params (uint, uint)[] lineRanges)
        {
            foreach ((uint, uint) range in lineRanges)
            {
                CoveredLineRanges.Add(range);
            }
        }

        public override bool Equals(object obj) =>
            obj is FileCoverage fileCoverage && fileCoverage.CoveredLineRanges.SetEquals(CoveredLineRanges);

        public override int GetHashCode() =>
            CoveredLineRanges.GetHashCode();

        public override string ToString() =>
            $"FileCoverage[{string.Join(", ", CoveredLineRanges)}]";
    }
}

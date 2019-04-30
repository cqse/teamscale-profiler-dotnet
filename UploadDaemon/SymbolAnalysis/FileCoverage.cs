using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}

using Newtonsoft.Json;
using System.Linq;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Report.Testwise
{
    /// <summary>
    /// The coverage in a specific file in a <see cref="TestwiseCoverageReport"/>.
    /// </summary>
    public class File : FileCoverage
    {
        [JsonProperty(PropertyName = "fileName")]
        public string FileName;

        public File(string fileName, FileCoverage coverage) : base(coverage.CoveredLineRanges)
        {
            this.FileName = fileName;
        }

        public File(string fileName, params (uint, uint)[] coveredLineRanges) : base(coveredLineRanges)
        {
            this.FileName = fileName;
        }

        [JsonProperty(PropertyName = "coveredLines")]
        public string CoveredLines
        {
            get => CoveredLineRanges.Select((range) => $"{range.Item1}-{range.Item2}").Aggregate((a, b) => $"{a},{b}");
            set
            {
                foreach (string rangeString in value.Split(','))
                {
                    uint[] range = rangeString.Split('-').Select(uint.Parse).ToArray();
                    CoveredLineRanges.Add((range[0], range[1]));
                }
            }
        }
    }
}

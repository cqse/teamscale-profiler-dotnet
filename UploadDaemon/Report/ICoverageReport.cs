using System.Collections.Generic;

namespace UploadDaemon.Report
{
    /// <summary>
    /// A coverage report.
    /// </summary>
    public interface ICoverageReport
    {
        /// <summary>
        /// Whether this report contains any coverage.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// The Teamscale identifier for the format of this report.
        /// </summary>
        string UploadFormat { get; }

        /// <summary>
        /// The file extension for this type of report.
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Returns a new report wth the union of the coverage data of both reports.
        /// </summary>
        ICoverageReport Union(ICoverageReport coverageReport);

        /// <summary>
        /// Converts this report into a list of serialized reports.
        /// Reports are split to avoid too large strings (around 1GB) which cause OutOfMemoryErrors.
        /// </summary>
        List<string> ToStringList();
    }
}

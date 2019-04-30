using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UploadDaemon.SymbolAnalysis;

namespace UploadDaemon.Upload
{
    /// <summary>
    /// Uploads data to a storage location, e.g. a directory on disk or a remote endpoint.
    /// </summary>
    public interface IUpload : IEquatable<IUpload>
    {
        /// <summary>
        /// Performs the upload asynchronously (may also be synchronous).
        /// Must not throw any exceptions.
        /// </summary>
        /// <param name="filePath">The path of the file to upload.</param>
        /// <param name="version">The parsed version number.</param>
        /// <returns>Whether the upload succeeded</returns>
        Task<bool> UploadAsync(string filePath, string version);

        /// <summary>
        /// Performs the upload of line coverage to a VCS revision asynchronously (may also be synchronous).
        /// Must not throw any exceptions.
        /// </summary>
        /// <param name="originalTraceFilePath">The path to the original trace file (used when logging to allow attribution of log messages to a trace file).</param>
        /// <param name="lineCoverageReport">The content of the coverage report.</param>
        /// <param name="revisionOrTimestamp">The VCS revision or Teamscale branch+timestamp to which the report should be uploaded.</param>
        /// <returns>Whether the upload succeeded</returns>
        Task<bool> UploadLineCoverageAsync(string originalTraceFilePath, string lineCoverageReport, RevisionFileUtils.RevisionOrTimestamp revisionOrTimestamp);

        /// <summary>
        /// Returns a human-readable description of the upload that can be incorporated in log messages.
        /// Must not contain passwords/access keys/...
        /// </summary>
        string Describe();
    }
}

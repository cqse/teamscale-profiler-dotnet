using System.Collections.Generic;
using static UploadDaemon.SymbolAnalysis.RevisionFileUtils;

namespace UploadDaemon.Scanning
{

    /// <summary>
    /// A method-call trace from a trace file.
    /// </summary>
    public class Trace
    {
        /// <summary>
        /// The path of the trace file that this trace comes from.
        /// </summary>
        public string OriginTraceFilePath;

        /// <summary>
        /// The (method id, assembly name) pairs of the methods covered by this trace.
        /// </summary>
        public IList<(string, uint)> CoveredMethods = new List<(string, uint)>();

        /// <summary>
        /// Whether this trace is empty.
        /// </summary>
        public bool IsEmpty => CoveredMethods.Count == 0;

        /// <summary>
        /// The uploads targets (revision/timestamp and optionally teamscale project) that are retrieved from resource files that are embedded into assemblies referenced in the trace file.
        /// </summary>
        public readonly List<(string project, RevisionOrTimestamp revisionOrTimestamp)> embeddedUploadTargets = new List<(string project, RevisionOrTimestamp revisionOrTimestamp)>();
    }
}

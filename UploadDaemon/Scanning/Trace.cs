using System.Collections.Generic;

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
    }
}

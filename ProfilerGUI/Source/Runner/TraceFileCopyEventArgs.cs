using System;
using System.Collections.Generic;

namespace ProfilerGUI.Source.Runner
{
    /// <summary> Event args for Trace-files copy events. </summary>
    public class TraceFileCopyEventArgs : EventArgs
    {
        /// <summary> The full paths of successfully copied trace files </summary>
        public List<string> SuccessfulCopiedFiles { get; } = new List<string>();

        /// <summary> The full paths of trace files that could not be copied. </summary>
        public List<string> FailedCopiedFiles { get; } = new List<string>();
    }
}

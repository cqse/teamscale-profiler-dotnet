using System.Collections.Generic;

namespace ProfilerGUI.Source.Shared
{
    /// <summary>
    /// Data class that encapsulates the result of running a process.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Return code the process emitted.
        /// </summary>
        public int ReturnCode { get; }

        /// <summary>
        /// Text printed to stdout.
        /// </summary>
        public string StdOut { get; }

        /// <summary>
        /// Text printed to stderr.
        /// </summary>
        public string StdErr { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ProcessResult(int returnCode, string stdout = "", string stderr = "")
        {
            ReturnCode = returnCode;
            StdOut = stdout;
            StdErr = stderr;
        }
    }
}
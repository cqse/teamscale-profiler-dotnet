using System;

namespace UploadDaemon.Scanning
{
    /// <summary>
    /// Signals and inconsistency within a trace file that cannot be handled.
    /// </summary>
    public class InvalidTraceFileException : Exception
    {
        public InvalidTraceFileException(string message) : base(message)
        {
        }
    }
}

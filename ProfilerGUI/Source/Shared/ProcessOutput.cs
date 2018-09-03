using System.Collections.Generic;

namespace ProfilerGUI.Source.Shared
{
    public class ProcessOutput
    {
        public int ReturnCode { get; }

        // TODO (FS) why is this a string? are these lines? if so, how about renaming to OutputLines?
        public List<string> Output { get; }

        public ProcessOutput(int returnCode, List<string> output = null)
        {
            ReturnCode = returnCode;
            Output = output ?? new List<string>();
        }
    }
}

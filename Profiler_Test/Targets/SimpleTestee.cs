using System.Diagnostics;
using System.IO;

namespace Cqse.Teamscale.Profiler.Dotnet.Targets
{
    class SimpleTestee : Testee<TesteeProcess>
    {
        public SimpleTestee(FileInfo exectable) : base(exectable) {}

        /// <inheritDoc/>
        protected override TesteeProcess CreateProcess(Process process)
        {
            return new TesteeProcess(process);
        }
    }
}

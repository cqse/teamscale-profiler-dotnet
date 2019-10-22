using NUnit.Framework;

namespace UploadDaemon.SymbolAnalysis
{
    [TestFixture]
    public class ParsedTraceFileTest
    {
        [Test]
        public void MustSupportNewStyleMethodReferences()
        {
            ParsedTraceFile trace = new ParsedTraceFile(new string[]
            {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:12345",
            }, "cov.txt");

            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(1));
            Assert.That(trace.CoveredMethods[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(trace.CoveredMethods[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void MustSupportOldStyleMethodReferences()
        {
            ParsedTraceFile trace = new ParsedTraceFile(new string[]
            {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:9876:12345",
            }, "cov.txt");

            Assert.That(trace.CoveredMethods, Has.Count.EqualTo(1));
            Assert.That(trace.CoveredMethods[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(trace.CoveredMethods[0].Item2, Is.EqualTo(12345));
        }
    }
}
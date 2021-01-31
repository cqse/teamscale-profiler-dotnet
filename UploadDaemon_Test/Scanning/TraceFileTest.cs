﻿using NUnit.Framework;
using UploadDaemon.Scanning;

namespace UploadDaemon.Scanning
{
    [TestFixture]
    public class TraceFileTest
    {
        [Test]
        public void SupportsNewStyleMethodReferences()
        {
            TraceFile trace = new TraceFile(":path:", new string[]
            {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:12345",
            });

            Assert.That(trace.FindCoveredMethods(), Has.Count.EqualTo(1));
            Assert.That(trace.FindCoveredMethods()[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(trace.FindCoveredMethods()[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void SupportsOldStyleMethodReferences()
        {
            TraceFile trace = new TraceFile(":path:", new string[]
            {
            "Assembly=ProfilerGUI:2 Version:1.0.0.0",
            $"Inlined=2:9876:12345",
            });

            Assert.That(trace.FindCoveredMethods(), Has.Count.EqualTo(1));
            Assert.That(trace.FindCoveredMethods()[0].Item1, Is.EqualTo("ProfilerGUI"));
            Assert.That(trace.FindCoveredMethods()[0].Item2, Is.EqualTo(12345));
        }

        [Test]
        public void IgnoresMethodReferenceFromUnknownAssembly()
        {
            TraceFile trace = new TraceFile(":path:", new string[]
            {
            $"Inlined=1:12345",
            });

            Assert.That(trace.FindCoveredMethods(), Is.Empty);
        }
    }
}

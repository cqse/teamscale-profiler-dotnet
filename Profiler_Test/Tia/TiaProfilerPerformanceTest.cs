using Cqse.Teamscale.Profiler.Dotnet.Proxies;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Cqse.Teamscale.Profiler.Dotnet.Tia
{
    /// <summary>
    /// Test case for measuring performance impact of the coverage profiler in TIA mode.
    /// </summary>
    public class TiaProfilerPerformanceTest : TiaProfilerTestBase
    {
        public TiaProfilerPerformanceTest() : base(IpcImplementation.Native) { }

        [Test]
        public void RunProfilerTestee32()
        {
            Testee testee = new Testee(GetTestProgram("ProfilerTestee32.exe"));

            MeasureAverageRuntimeAndPrintResults(
                "ProfilerTestee",
                (profiler) => testee.Run(arguments: "all", profiler),
                repetitions: 100);
        }

        [Test]
        public void RunProfilerTestee64()
        {
            Testee testee = new Testee(GetTestProgram("ProfilerTestee64.exe"));

            MeasureAverageRuntimeAndPrintResults(
                "ProfilerTestee",
                (profiler) => testee.Run(arguments: "all", profiler),
                repetitions: 100);
        }

        [Test]
        public void RunGeneratedTest()
        {
            Testee testee = new Testee(GetTestProgram("GeneratedTest.exe"));

            MeasureAverageRuntimeAndPrintResults(
                "GeneratedTest",
                (profiler) => testee.Run(profiler: profiler),
                repetitions: 1);
        }

        private void MeasureAverageRuntimeAndPrintResults(string testeeName, Action<IProfiler> testeeRunner, int repetitions)
        {
            Console.WriteLine($"### Run {testeeName} {repetitions} times without profiler");
            Console.WriteLine($"Average runtime: {ToString(MeasureAverageRuntime(testeeRunner, new NoProfiler(), repetitions))}");

            Console.WriteLine($"### Run {testeeName} {repetitions} times with profiler");
            Console.WriteLine($"Average runtime: {ToString(MeasureAverageRuntime(testeeRunner, profilerUnderTest, repetitions))}");
        }

        private string ToString(TimeSpan runtime)
        {
            return string.Format("{0:00}:{1:00}.{2:000}", runtime.TotalMinutes, runtime.Seconds, runtime.Milliseconds);
        }

        private TimeSpan MeasureAverageRuntime(Action<IProfiler> testeeRunner, IProfiler profiler, int repetitions)
        {
            List<TimeSpan> durations = new List<TimeSpan>();

            for (int i = 0; i < repetitions; i++)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                testeeRunner(profiler);

                stopWatch.Stop();
                durations.Add(stopWatch.Elapsed);
            }

            return new TimeSpan((long)durations.Average(duration => duration.Ticks));
        }
    }
}

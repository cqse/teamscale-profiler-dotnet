using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfilerTestee
{
    // Regressiontest program that can be profiled to see if changes to the profiler's code change its output.
    // Contains several interesting C# constructs.
    // Can be executed in "full" mode: then all methods are executed.
    // Can be executed in "interactive" mode: Selective call methods of the Interactive class based on console input.
    // Can be executed in "none" mode: then only the Main method is executed. (Some methods are inlined, though!)
    // The usual procedure is to compile this Testee program (Release|AnyCPU), profile it with the old and new versions of the profiler (32bit)
    // and compare the output of a test gap dashboard for each (old profiler version, new profiler version) pair.
    class Program
    {

        // static method. not inlined
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide an argument.");
                Environment.Exit(1);
            }

            switch (args[0])
            {
                case "none":
                    break;
                case "interactive":
                    Interactive.Run();
                    break;
                case "all":
                    Runner runner2 = new Runner(10);
                    runner2.Value = runner2.Value + 1;
                    int x = runner2.smallMethod(1);
                    runner2.longMethod(x);
                    runner2.longMethod(x + x);
                    foreach (int i in runner2.iterate())
                    {
                        Console.WriteLine("Got " + i);
                    }
                    break;
                default:
                    Console.WriteLine("Supply an argument of 'none', 'interactive' or 'all'!");
                    break;
            }

        }
    }
}

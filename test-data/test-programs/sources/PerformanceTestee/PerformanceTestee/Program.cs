using System;

namespace PerformanceTestee
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Please provide an argument.");
                Environment.Exit(1);
            }

            switch (args[0])
            {
                case "many-calls-recursive":
                    new ManyCalls().CallAMethodRecursively(times: int.Parse(args[1]));
                    break;
                case "many-calls":
                    new ManyCalls().CallAMethod(times: int.Parse(args[1]));
                    break;
                case "many-methods":
                    new ManyCalls().CallManyMethods();
                    break;
                default:
                    Console.WriteLine("Supply an argument of 'none', 'interactive' or 'all'!");
                    break;
            }
        }
    }
}

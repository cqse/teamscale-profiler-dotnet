using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProfilerTestee
{
    class Interactive
    {
        internal static void Run()
        {
            Console.WriteLine("interactive");
            while (true)
            {
                switch (Console.ReadLine())
                {
                    case nameof(A):
                        A();
                        break;
                    case nameof(B):
                        B();
                        break;
                    case nameof(C):
                        C();
                        break;
                    default:
                        Console.WriteLine("exit");
                        return;
                }
            }
        }

        private static void A()
        {
            Console.WriteLine(nameof(A));
        }

        private static void B()
        {
            Console.WriteLine(nameof(B));
        }

        private static void C()
        {
            Console.WriteLine(nameof(C));
        }
    }
}

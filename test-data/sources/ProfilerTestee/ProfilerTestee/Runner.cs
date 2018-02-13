using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class Runner
{

    // class constructor. not inlined
    static Runner() {
        Console.WriteLine("Static init");
        if (DateTime.Now.Minute > 60)
        {
            Console.WriteLine("Your clock is broken. Exiting");
            Environment.Exit(2);
        }
    }

    // property. trivial
    public int Value { get; set; }

    // constructor. not inlined
    public Runner(int val)
    {
        Value = val;
        if (val < 20)
        {
            if (val > 10)
            {
                if (val % 2 == 0)
                {
                    Console.WriteLine("Met the condition.");
                }
            }
        }
    }

    // inlined, trivial method
    public int smallMethod()
    {
        return 2;
    }

    // inlined, non-trivial overload
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int smallMethod(int overload)
    {
        int[] a = { overload, 2 };
        a[0] *= a[1];
        return  a[0];
    }

    // non-inlinable method
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int longMethod(int x)
    {
        int i = x + Value;
        while (i < 100)
        {
            if (i % 30 == 0)
            {
                Console.WriteLine(i);
            }
            i++;
        }
        if (new Random().Next(100) < 50)
        {
            Console.WriteLine("Random number < 50");
        }
        else
        {
            Console.WriteLine("Random number >= 50");
        }

        if (x != 1000) {
            longMethod(1000);
        }

        // lambda
        return executeSafely(val =>
        {
            return smallMethod() + val;
        }, 2);
    }

    // iterator. will be inlined by the compiler for some reason, but also generates additional, non-inlined methods
    public IEnumerable<int> iterate()
    {
        int i = 0;
        while (i < Value)
        {
            yield return i;
            i++;
            if (i == 2)
            {
                i++;
            }
            if (i == 5)
            {
                yield break;
            }
        }
    }

    // helper function for the lambda. not inlined
    public int executeSafely(Func<int, int> function, int value)
    {
        Console.WriteLine("Safety is on!");
        int retVal = function(value);
        if (retVal == 0)
        {
            retVal++;
        }
        Console.WriteLine("Safety is off!");
        return retVal;
    }

}

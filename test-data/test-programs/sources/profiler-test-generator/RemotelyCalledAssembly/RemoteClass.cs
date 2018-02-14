using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemotelyCalledAssembly
{
    /// <summary>
    /// Class with a method that is called remote by the test.
    /// </summary>
    public class RemoteClass
    {
        public void calledRemoteMethod()
        {
            Console.WriteLine("Called remote method.");
        }
    }
}

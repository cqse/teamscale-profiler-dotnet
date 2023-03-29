using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginLibrary
{
    public class Plugin : PluginSample.IPlugin
    {
        public void Call()
        {
            Console.WriteLine("Called plugin");
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PluginSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string pluginAssembly = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "plugins/plugin-library.dll");
            Type pluginType = Assembly.LoadFrom(pluginAssembly).GetType("PluginLibrary.Plugin");
            IPlugin plugin = pluginType.GetConstructor(new Type[0]).Invoke(new object[0]) as IPlugin;
            plugin.Call();
        }
    }
}

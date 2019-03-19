using Cqse.ConQAT.Dotnet.Bummer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DumpPdb
{
    /// <summary>
    /// Dumps all mappings from the given PDB file to the console as a CSV.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: dumppdb PDBFILE");
                Console.WriteLine("Dumps the parsed symbols from the given PDBFILE in CSV format.");
                return;
            }

            string pdbFile = args[0];
            string assemblyName = Path.GetFileNameWithoutExtension(pdbFile);
            MethodMapper mapper = new MethodMapper();
            AssemblyMethodMappings assemblyMappings = mapper.GetMethodMappings(pdbFile, assemblyName);

            Console.WriteLine("MethodToken, SourceFile, StartLine, EndLine");
            foreach (MethodMapping mapping in assemblyMappings.MethodMappings)
            {
                Console.WriteLine($"{mapping.MethodToken}, {mapping.SourceFile}, {mapping.StartLine}, {mapping.EndLine}");
            }
        }
    }
}
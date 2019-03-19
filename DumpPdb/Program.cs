using Cqse.ConQAT.Dotnet.Bummer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            // sorts the elements by source file and then line number since OrderBy guarantees a stable sort
            IEnumerable<MethodMapping> sortedMappings = assemblyMappings.MethodMappings.OrderBy(mapping => mapping.StartLine)
                .OrderBy(mapping => mapping.SourceFile);

            Console.WriteLine("MethodToken, StartLine, EndLine, SourceFile");
            foreach (MethodMapping mapping in assemblyMappings.MethodMappings)
            {
                // we pad all fields to the length of the corresponding header plus the comma to align
                // the fields nicely on the console for easy readability
                // the source file is the last field because we cannot pad it to a fixed length
                string tokenField = $"{mapping.MethodToken},".PadRight(12);
                string startLineField = $"{mapping.StartLine},".PadRight(10);
                string endLineField = $"{mapping.EndLine},".PadRight(8);
                Console.WriteLine($"{tokenField} {startLineField} {endLineField} {mapping.SourceFile}");
            }
        }
    }
}
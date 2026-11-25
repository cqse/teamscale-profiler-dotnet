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
            // a method with lines in several source files gets one row per source file
            IEnumerable<(uint methodToken, SourceFileMapping sourceFileMapping)> sortedRows = assemblyMappings.MethodMappings
                .SelectMany(mapping => mapping.SourceFileMappings.Select(sourceFileMapping => (methodToken: mapping.MethodToken, sourceFileMapping: sourceFileMapping)))
                .OrderBy(row => row.sourceFileMapping.LineRanges.FirstOrDefault()?.StartLine ?? 0)
                .OrderBy(row => row.sourceFileMapping.SourceFile);

            Console.WriteLine("MethodToken, LineRanges, SourceFile");
            foreach ((uint methodToken, SourceFileMapping sourceFileMapping) in sortedRows)
            {
                // we pad all fields to the length of the corresponding header plus the comma to align
                // the fields nicely on the console for easy readability
                // the source file is the last field because we cannot pad it to a fixed length
                string tokenField = $"{methodToken},".PadRight(12);
                string lineRangesField = $"{string.Join(" ", sourceFileMapping.LineRanges.Select(range => $"{range.StartLine}-{range.EndLine}"))},".PadRight(11);
                Console.WriteLine($"{tokenField} {lineRangesField} {sourceFileMapping.SourceFile}");
            }
        }
    }
}
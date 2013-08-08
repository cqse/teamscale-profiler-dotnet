using System;
using System.IO; 
using System.Collections.Generic;
using Mono.Cecil;

namespace Eu.Cqse.TestGap
{
    public class AssemblyMetaDataCollector
    {
        private const string ASSEMBLY_PREFIX = "Assembly=";
        private const string PROCESS_PREFIX = "Process=";

        public static void Main(String[] args)
        {
            string profilerOutput = args[0];
            if (!File.Exists(profilerOutput))
            {
                return;
            }
            StreamReader file = new System.IO.StreamReader(profilerOutput);

            Dictionary<string, string> assemblyNames = new Dictionary<string, string>();
            string process = "";
            string line;
            int counter = 0;
            while ((line = file.ReadLine()) != null)
            {
                if (line.StartsWith(ASSEMBLY_PREFIX))
                {
                    string assemblyName = line.Substring(ASSEMBLY_PREFIX.Length);
                    assemblyName = assemblyName.Remove(assemblyName.IndexOf(':'));
                    assemblyNames.Add(assemblyName, "unknown");
                }
                if (line.StartsWith(PROCESS_PREFIX))
                {
                    process = line.Substring(PROCESS_PREFIX.Length);
                    process = process.Remove(process.LastIndexOf('\\'));
                }
                counter++;
            }
            file.Close();

            TextWriter writer = new StreamWriter(File.Open(profilerOutput, FileMode.Append, FileAccess.Write));
            foreach (string assembly in assemblyNames.Keys)
            {
                string[] fileEndings = new string[] { ".exe", ".dll"};
                foreach (string fileEnding in fileEndings)
                {
                    string fullQualifiedName = process + @"\" + assembly + fileEnding;
                    if (AssemblyExists(fullQualifiedName))
                    {
                        writer.WriteLine("AssemblyVersion=" + assembly + fileEnding + ":" + GetVersion(fullQualifiedName));
                    }
                }
            }
            writer.Close();    
        }

        private static bool AssemblyExists(string assemblyPath)
        {
            if (File.Exists(assemblyPath))
            {
                return true;
            }
            return false;
        }

        private static string GetVersion(string assemblyPath)
        {
            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
            return  assemblyDefinition.FullName;
        }

    }
}

using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UploadDaemon.Scanning
{
    public class AssemblyExtractor
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly Regex AssemblyLineRegex = new Regex(@"^Assembly=(?<name>[^:]+):(?<id>\d+).*?(?: Path:(?<path>.*))?$");

        public readonly Dictionary<uint, (string name, string path)> Assemblies = new Dictionary<uint, (string name, string path)>();

        public void ExtractAssemblies(string[] lines)
        {
            foreach (string line in lines)
            {
                string[] keyValuePair = line.Split(new[] { '=' }, 2);
                if (keyValuePair.Length < 2)
                {
                    continue;
                }

                if (keyValuePair[0] == "Assembly")
                {
                    Match assemblyMatch = AssemblyLineRegex.Match(line);
                    Assemblies[Convert.ToUInt32(assemblyMatch.Groups["id"].Value)] = (assemblyMatch.Groups["name"].Value, assemblyMatch.Groups["path"].Value);
                }
            }
        }
    }
}
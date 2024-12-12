//------------------------------------------------------------------------------
// <copyright company="CQSE GmbH">
//   Copyright (c) CQSE GmbH
// </copyright>
// <license>
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </license>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Fclp;
using Newtonsoft.Json;
using System.Linq;

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Entry point class to the binary-source mapper.
    /// </summary>
    internal class Program
    {

        /// <summary>
        /// The entry point of the program, where the program control 
        /// starts and ends.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static int Main(string[] args)
        {
            var commandLineParser = new FluentCommandLineParser();

            var filenames = new List<string>();
            var assemblyNames = new List<string>();

            commandLineParser.Setup<List<string>>('f', "files")
                .Callback(items => filenames = items)
                .WithDescription("Path(s) to symbol file(s) that should be analyzed.")
                .Required();
            commandLineParser.Setup<List<string>>('a', "assemblyNames")
                .Callback(items => assemblyNames = items)
                .WithDescription("Assembly name(s) to be used in the AssemblyMethodMapping(s). Must have the same order and number of elements as the files parameter.")
                .Required();

            commandLineParser.SetupHelp("?", "help")
                .Callback(text => Console.WriteLine(text));

            var commandLineParseResult = commandLineParser.Parse(args);

            if (!filenames.Count.Equals(assemblyNames.Count))
            {
                Console.WriteLine("The parameters <files> and <assemblyNames> had different lengths. Aborting analysis.");
                return -1;
            }
            if (!commandLineParseResult.HasErrors)
            {
                List<AssemblyMethodMappings> methodMappings = GetMethodMappings(filenames, assemblyNames);
                OutputMethodMappings(methodMappings);
            }
            else
            {
                Console.WriteLine(commandLineParseResult.ErrorText);
                commandLineParser.HelpOption.ShowHelp(commandLineParser.Options);
            }
            return 0;
        }

        /// <summary>
        /// Gets the method mappings for the specified list of file names.
        /// </summary>
        /// <param name="filenames">The list of symbol filenames to analyze.</param>
        private static List<AssemblyMethodMappings> GetMethodMappings(List<string> filenames, List<string> assemblyNames)
        {
            var methodMappings = new List<AssemblyMethodMappings>();
            var methodMapper = new MethodMapper();
            // TODO (FS) please don't use 1-letter variable names
            foreach (var pair in filenames.Zip(assemblyNames, (f, s) => new { filename = f, assemblyName = s }))
            {
                if (!File.Exists(pair.filename))
                {
                    Console.WriteLine("File does not exist: " + pair.filename);
                    continue;
                }

                methodMappings.Add(methodMapper.GetMethodMappings(pair.filename, pair.assemblyName));
            }
            return methodMappings;
        }

        /// <summary>
        /// Prints the specified list of method mappings on the console as JSON.
        /// </summary>
        /// <param name="methodMappings">The method mappings to save.</param>
        private static void OutputMethodMappings(List<AssemblyMethodMappings> methodMappings)
        {
            var settings = new JsonSerializerSettings();
            settings.Formatting = Formatting.None;
            var serializer = JsonSerializer.Create(settings);
            serializer.Serialize(Console.Out, methodMappings);
        }
    }
}

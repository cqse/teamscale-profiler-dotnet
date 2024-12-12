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
using Fclp;
using Newtonsoft.Json;

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
                List<AssemblyMethodMappings> methodMappings = Bummer.GetMethodMappings(filenames, assemblyNames);
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

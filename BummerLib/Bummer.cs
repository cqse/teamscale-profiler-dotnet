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
using System.Linq;

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Entry point class to the binary-source mapper.
    /// </summary>
    public class Bummer
    {
        /// <summary>
        /// Gets the method mappings for the specified list of file names.
        /// </summary>
        /// <param name="filenames">The list of symbol filenames to analyze.</param>
        public static List<AssemblyMethodMappings> GetMethodMappings(List<string> filenames, List<string> assemblyNames)
        {
            var methodMappings = new List<AssemblyMethodMappings>();
            var methodMapper = new MethodMapper();
            foreach (var pair in filenames.Zip(assemblyNames, (filename, assemblyName) => new { filename = filename, assemblyName = assemblyName }))
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
    }
}

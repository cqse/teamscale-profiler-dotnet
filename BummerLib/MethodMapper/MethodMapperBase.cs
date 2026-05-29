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

using System.Collections.Generic;
using System.IO;

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Base class for concrete method mappers.
    /// Breaks down support for specific symbol files to 
    /// the corresponding extension.
    /// Handles setting assembly name and symbol file name for subclasses.
    /// </summary>
    public abstract class MethodMapperBase : IMethodMapper
    {
        /// <inheritdoc/>
        public bool CanInterpretSymbolFile(string pathToSymbolFile)
        {
            return Path.GetExtension(pathToSymbolFile).ToLower()
                .Equals(this.GetSupportedExtension().ToLower());
        }

        /// <inheritdoc/>
        public AssemblyMethodMappings GetMethodMappings(string pathToSymbolFile, string assemblyName)
        {
            var methodMappings = new AssemblyMethodMappings()
            {
                AssemblyName = assemblyName,
                SymbolFileName = pathToSymbolFile
            };

            methodMappings.MethodMappings.AddRange(
                this.ReadMethodMappings(pathToSymbolFile));
            return methodMappings;
        }

        /// <summary>
        /// When implemented in a subclass, gets the file extension 
        /// supported by the concrete method mapper.
        /// </summary>
        /// <returns>The file extension supported by the concrete 
        /// method mapper.</returns>
        protected abstract string GetSupportedExtension();

        /// <summary>
        /// When implemented in a subclass, reads the method mappings 
        /// from the specified symbol file.
        /// </summary>
        /// <returns>The list of method mappings.</returns>
        /// <param name="pathToSymbolFile">Path to symbol file.</param>
        protected abstract IEnumerable<MethodMapping> ReadMethodMappings(string pathToSymbolFile);
    }
}

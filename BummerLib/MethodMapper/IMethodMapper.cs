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

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Specifies the functionality of classes that can interpret symbol files 
    /// and provide a mapping between source code and binary code 
    /// based on the contained methods.
    /// </summary>
    public interface IMethodMapper
    {
        /// <summary>
        /// When implemented in a subclass, determines whether this instance 
        /// can interpret the specified symbol file.
        /// </summary>
        /// <returns><c>true</c> if this instance can interpret the specified 
        /// symbol file; <c>false</c>, otherwise.</returns>
        /// <param name="pathToSymbolFile">Path to a symbol file.</param>
        bool CanInterpretSymbolFile(string pathToSymbolFile);

        /// <summary>
        /// When implemented in a subclass, gets the method mappings that allow 
        /// to match binary methods to source methods, by interpreting 
        /// the specified symbol file.
        /// </summary>
        /// <returns>The method mappings for the specified symbol file.</returns>
        /// <param name="pathToSymbolFile">Path to the symbol file to interpret.</param>
        /// <param name="assemblyName">Assembly name to use for the generated mappings (usually the filename without extension).</param>
        AssemblyMethodMappings GetMethodMappings(string pathToSymbolFile, string assemblyName);
    }
}

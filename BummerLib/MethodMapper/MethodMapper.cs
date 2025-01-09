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

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// <para>
    /// Given a debug symbol file,
    /// provides a mapping from methods in IL assembly to source code lines.
    /// </para>
    /// <para>
    /// This class is a bridge to different concrete implementations 
    /// of method mappers.
    /// Currently, it supports Mono debug files (.mdb) and 
    /// Microsoft .NET symbol files (.pdb).
    /// </para>
    /// </summary>
    public class MethodMapper : IMethodMapper
    {
        /// <summary>
        /// Holds an instance of 
        /// <see cref="MdbMethodMapper"/>.
        /// </summary>
        /// <value>The MDB method mapper.</value>
        private MdbMethodMapper MdbMethodMapper { get; } = new MdbMethodMapper();

        /// <summary>
        /// Holds an instance of 
        /// <see cref="PdbMethodMapper"/>.
        /// </summary>
        /// <value>The PDB method mapper.</value>
        private PdbMethodMapper PdbMethodMapper { get; } = new PdbMethodMapper();

        /// <summary>
        /// Determines whether the specified symbol file can be interpreted.
        /// </summary>
        /// <returns><c>true</c>, if any of the concrete method mappers can 
        /// interpret the specified symbol file;
        /// <c>false</c>, otherwise.</returns>
        /// <param name="pathToSymbolFile">Path to a symbol file.</param>
        public bool CanInterpretSymbolFile(string pathToSymbolFile)
            => MdbMethodMapper.CanInterpretSymbolFile(pathToSymbolFile)
                || PdbMethodMapper.CanInterpretSymbolFile(pathToSymbolFile);

        /// <summary>
        /// Gets the method mappings that allow to match binary methods to 
        /// source methods, by forwarding the specified symbol file to 
        /// a suitable method mapper.
        /// </summary>
        /// <returns>The method mappings for the specified symbol file.</returns>
        /// <param name="pathToSymbolFile">Path to the symbol file to interpret.</param>
        public AssemblyMethodMappings GetMethodMappings(string pathToSymbolFile, string assemblyName)
        {
            if (this.MdbMethodMapper.CanInterpretSymbolFile(pathToSymbolFile))
            {
                return this.MdbMethodMapper.GetMethodMappings(pathToSymbolFile, assemblyName);
            }

            if (this.PdbMethodMapper.CanInterpretSymbolFile(pathToSymbolFile))
            {
                return this.PdbMethodMapper.GetMethodMappings(pathToSymbolFile, assemblyName);
            }

            throw new ArgumentException(
                "Symbol file " + pathToSymbolFile + " not supported.", 
                "pathToSymbolFile");
        }
    }
}

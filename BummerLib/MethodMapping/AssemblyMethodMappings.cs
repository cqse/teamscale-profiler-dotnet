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
    using System.Collections.Generic;

    /// <summary>
    /// Holds mappings for methods within one assembly.
    /// </summary>
    public class AssemblyMethodMappings
    {
        /// <summary>
        /// Gets the method mappings.
        /// </summary>
        /// <value>The method mappings.</value>
        public List<MethodMapping> MethodMappings { get; } = new List<MethodMapping>();

        /// <summary>
        /// Gets or sets the name of the corresponding assembly.
        /// </summary>
        /// <value>The name of the corresponding assembly.</value>
        public string AssemblyName { get; set; }

        /// <summary>
        /// Gets or sets the name of the corresponding symbol file.
        /// </summary>
        /// <value>The name of the corresponding symbol file.</value>
        public string SymbolFileName { get; set; }

        /// <summary>
        /// Initializes a new instance of the 
        /// <see cref="AssemblyMethodMappings"/> class.
        /// </summary>
        public AssemblyMethodMappings()
        {
            this.MethodMappings = new List<MethodMapping>();
        }
    }
}

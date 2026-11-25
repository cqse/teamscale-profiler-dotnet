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

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Stores the mapping of an IL method within an assembly to source code 
    /// lines in a specific source file.
    /// </summary>
    public class MethodMapping
    {
        /// <summary>
        /// Gets or sets the method token, which was given to the method 
        /// by the compiler.
        /// </summary>
        /// <value>The IL method token. This token identifies the method 
        /// within the assembly.</value>
        public uint MethodToken { get; set; }

        /// <summary>
        /// Gets the source files that contain the code of the method, together with the lines of the method in each.
        /// </summary>
        /// <value>The source files of the method. Usually a single one, but e.g. a constructor also contains the code
        /// of field and auto-property initializers, which may be located in other files of a partial class.
        /// Empty if the symbol file contains no source file for the method, e.g. if it is compiler-generated.</value>
        public List<SourceFileMapping> SourceFileMappings { get; } = new List<SourceFileMapping>();
    }
}

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
        /// Gets or sets the source file where the method is included.
        /// </summary>
        /// <value>The source file where the method is included. May be null if the method is compiler-generated.
        /// In that case, the start and end line are not valid.</value>
        public string SourceFile { get; set; }

        /// <summary>
        /// Gets or sets the start line of the method in the source code.
        /// </summary>
        /// <value>The 1-based, inclusive start line of the source code that corresponds 
        /// to the method.</value>
        public uint StartLine { get; set; }

        /// <summary>
        /// Gets or sets the end line of the method in the source code.
        /// </summary>
        /// <value>The 1-based, inclusive end line of the source code that corresponds 
        /// to the method.</value>
        public uint EndLine { get; set; }
    }
}

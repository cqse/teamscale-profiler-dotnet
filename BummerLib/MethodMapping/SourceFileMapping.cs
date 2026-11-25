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
    /// The lines of a method within a single source file.
    /// </summary>
    public class SourceFileMapping
    {
        /// <summary>
        /// Gets or sets the source file.
        /// </summary>
        /// <value>The source file. May be null if the symbol file contains no name for it.</value>
        public string SourceFile { get; set; }

        /// <summary>
        /// Gets the sorted, non-overlapping line ranges of the method within the source file.
        /// </summary>
        /// <value>The line ranges of the method within the source file. Empty if the method has no lines
        /// in the source file that are not compiler hidden. For MDB files, this contains a single range from
        /// the first to the last line of the method, or the first and the last line as separate ranges in some
        /// cases.</value>
        public List<LineRange> LineRanges { get; internal set; } = new List<LineRange>();
    }
}

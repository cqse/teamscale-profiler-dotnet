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
using Mono.CompilerServices.SymbolWriter;

namespace Cqse.ConQAT.Dotnet.Bummer
{
    /// <summary>
    /// Method mapper for Mono symbol files (.mdb).
    /// </summary>
    public class MdbMethodMapper : MethodMapperBase
    {
        /// <summary>
        /// File extension of Mdb files.
        /// </summary>
        private const string MdbExtension = ".mdb";

        /// <inheritdoc/>
        protected override string GetSupportedExtension() => MdbExtension;

        /// <inheritdoc/>
        protected override IEnumerable<MethodMapping> ReadMethodMappings(string pathToSymbolFile)
        {
            var symbolFile = MonoSymbolFile.ReadSymbolFile(pathToSymbolFile);

            foreach (MethodEntry methodEntry in symbolFile.Methods)
            {
                yield return this.GetMapping(methodEntry);
            }
        }

        /// <summary>
        /// Provides the method mapping for the specified MDB method.
        /// </summary>
        /// <param name="mdbMethod">The MDB MethodEntry to get the mapping for.</param>
        private MethodMapping GetMapping(MethodEntry mdbMethod)
        {
            var sourceFileMapping = new SourceFileMapping { SourceFile = mdbMethod.CompileUnit.SourceFile.FileName };
            var methodMapping = new MethodMapping()
            {
                MethodToken = (uint)mdbMethod.Token,
                SourceFileMappings = { sourceFileMapping }
            };

            LineNumberTable lineNumberTable = mdbMethod.GetLineNumberTable();
            if (lineNumberTable != null)
            {
                LineNumberEntry[] lines = lineNumberTable.LineNumbers;

                if (lines != null && lines.Length > 0)
                {
                    uint firstLine = (uint)lines[0].Row;
                    uint lastLine = (uint)lines[lines.Length - 1].Row;
                    if (firstLine != PdbFile.CompilerHiddenLine && lastLine != PdbFile.CompilerHiddenLine)
                    {
                        if (firstLine <= lastLine)
                        {
                            sourceFileMapping.LineRanges.Add(new LineRange { StartLine = firstLine, EndLine = lastLine });
                        }
                        else
                        {
                            // E.g. a constructor whose initializer is located after its body, since the initializers come first
                            // in the IL. This only avoids invalid ranges until MDB support is removed. Unlike for PDB files, the
                            // line ranges are not determined with the LineRangeMerger, so the range above may still span other
                            // methods, e.g. if a constructor's initializer is located before its body.
                            sourceFileMapping.LineRanges.Add(new LineRange { StartLine = lastLine, EndLine = lastLine });
                            sourceFileMapping.LineRanges.Add(new LineRange { StartLine = firstLine, EndLine = firstLine });
                        }
                    }
                }
            }

            return methodMapping;
        }
    }
}

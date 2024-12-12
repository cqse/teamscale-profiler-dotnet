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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Linq;

    /// <summary>
    /// <para>
    /// Adapter for access to Microsoft.Cci.Pdb.PdbFiles.
    /// Uses reflection to read Pdb files and load the enclosed functions.
    /// Wraps these functions in a corresponding local wrapper class
    /// <see cref="PdbFunction"/>.
    /// </para>
    /// <para>
    /// Wrapping access to the MS class is necessary, because the methods
    /// we need to use to interpret Pdb files are marked as internal within the
    /// MS assembly. Hence they cannot be accessed using Mono Cecil directly.
    /// </para>
    /// </summary>
    public static class PdbFile
    {
        /// <summary>
        /// Marks lines that are "hidden" from the compiler. These lines should be ignored when caluclating
        /// the start and end line of a function.
        /// </summary>
        public const uint CompilerHiddenLine = 16707566;

        /// <summary>
        /// Namespace of all Pdb related classes within the Microsoft Pdb assembly.
        /// </summary>
        private const string PdbNamespace = "Microsoft.Cci.Pdb";

        /// <summary>
        /// Name of the PdbFile type in the MS assembly.
        /// </summary>
        private const string PdbFileTypeName = PdbNamespace + ".PdbFile";

        /// <summary>
        /// Name of the PdbFunction type in the MS assembly.
        /// </summary>
        private const string PdbFunctionTypeName = PdbNamespace + ".PdbFunction";

        /// <summary>
        /// Name of the PdbLines type in the MS assembly.
        /// </summary>
        private const string PdbLinesTypeName = PdbNamespace + ".PdbLines";

        /// <summary>
        /// Name of the PdbSource type in the MS assembly.
        /// </summary>
        private const string PdbSourceTypeName = PdbNamespace + ".PdbSource";

        /// <summary>
        /// Name of the PdbLine type in the MS assembly.
        /// </summary>
        private const string PdbLineTypeName = PdbNamespace + ".PdbLine";

        /// <summary>
        /// Name of the LoadFunctions methods of the MS PdbFile type.
        /// </summary>
        private const string PdbFileLoadFunctionsMethodName = "LoadFunctions";

        /// <summary>
        /// The name of the token field of the MS PdbFunction type.
        /// </summary>
        private const string PdbFunctionTokenFieldName = "token";

        /// <summary>
        /// The name of the lines field of the MS PdbFunction type.
        /// </summary>
        private const string PdbFunctionLinesFieldName = "lines";

        /// <summary>
        /// The name of the file field of the MS PdbLines type.
        /// </summary>
        private const string PdbLinesFileFieldName = "file";

        /// <summary>
        /// The name of the name field of the MS PdbSource type.
        /// </summary>
        private const string PdbSourceNameFieldName = "name";

        /// <summary>
        /// The name of the lines field of the MS PdbLines type.
        /// </summary>
        private const string PdbLinesLinesFieldName = "lines";

        /// <summary>
        /// The name of the lineBegin field of the MS PdbLine type.
        /// </summary>
        private const string PdbLineLineBeginFieldName = "lineBegin";

        /// <summary>
        /// The name of the lineEnd field of the MS PdbLine type.
        /// </summary>
        private const string PdbLineLineEndFieldName = "lineEnd";

        /// <summary>
        /// Mono Cecil Pdb assembly.
        /// </summary>
        private static readonly Assembly MonoCecilPdb = typeof(Mono.Cecil.Pdb.PdbReaderProvider).Assembly;

        /// <summary>
        /// MS PdbFile type as gathered via reflection.
        /// </summary>
        private static readonly Type PdbFileType = MonoCecilPdb.GetType(PdbFileTypeName);

        /// <summary>
        /// Method info for the LoadFunctions methods of MS PdbFile type
        /// as gathered via reflection.
        /// </summary>
        private static readonly MethodInfo PdbFileLoadFunctionsMethodInfo =
            PdbFileType.GetMethod(PdbFileLoadFunctionsMethodName, BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// MS PdbFunction type as gathered via reflection.
        /// </summary>
        private static readonly Type PdbFunctionType = MonoCecilPdb.GetType(PdbFunctionTypeName);

        /// <summary>
        /// MS PdbLines type as gathered via reflection.
        /// </summary>
        private static readonly Type PdbLinesType = MonoCecilPdb.GetType(PdbLinesTypeName);

        /// <summary>
        /// MS PdbSource type as gathered via reflection.
        /// </summary>
        private static readonly Type PdbSourceType = MonoCecilPdb.GetType(PdbSourceTypeName);

        /// <summary>
        /// MS PdbLine type as gathered via reflection.
        /// </summary>
        private static readonly Type PdbLineType = MonoCecilPdb.GetType(PdbLineTypeName);

        /// <summary>
        /// Binding flags for internal types, methods, or fields.
        /// </summary>
        private static readonly BindingFlags BindingFlagsInternal =
            BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// Loads the Pdb functions contained in the specified stream to a Pdb file.
        /// </summary>
        /// <returns>The Pdb functions contained in the specified stream.</returns>
        /// <param name="pdbStream">Stream pointing to a Pdb file.</param>
        public static IEnumerable<PdbFunction> LoadPdbFunctions(Stream pdbStream)
        {
            Dictionary<uint, object> tokenToSourceMapping = null;
            string sourceServerData = null;
            const int Age = 0;
            Guid guid = Guid.Empty;

            object loadedPdbFunctions = PdbFileLoadFunctionsMethodInfo
                .Invoke(null, new object[] { pdbStream, tokenToSourceMapping, sourceServerData, Age, guid });

            foreach (object pdbFunction in loadedPdbFunctions as Array)
            {
                yield return WrapPdbFunction(pdbFunction);
            }
        }

        /// <summary>
        /// Wraps the specified MS PdbFunction object into an object of type
        /// <see cref="PdbFunction"/>.
        /// </summary>
        private static PdbFunction WrapPdbFunction(object pdbFunction)
        {
            var wrappedPdbFunction = new PdbFunction();
            wrappedPdbFunction.Token =
                (uint)GetField(pdbFunction, PdbFunctionType, PdbFunctionTokenFieldName);

            var pdbFunctionLines =
                GetField(pdbFunction, PdbFunctionType, PdbFunctionLinesFieldName);
            if (pdbFunctionLines != null)
            {
                AddPdbFunctionLines(wrappedPdbFunction, pdbFunctionLines);
            }

            return wrappedPdbFunction;
        }

        /// <summary>
        /// Adds the specified PdbFunction lines to the provided function wrapper.
        /// </summary>
        /// <param name="wrappedPdbFunction">The wrapped PdbFunction, the lines
        /// should be added to.</param>
        /// <param name="pdbFunctionLines">A PdbLines object containing the
        /// source lines of a PdbFunction.</param>
        private static void AddPdbFunctionLines(PdbFunction wrappedPdbFunction, object pdbFunctionLines)
        {
            if (pdbFunctionLines == null || (pdbFunctionLines as Array).Length == 0)
            {
                return;
            }

            object lines = (pdbFunctionLines as Array).GetValue(0);

            var pdbFunctionLinesFile =
                GetField(lines, PdbLinesType, PdbLinesFileFieldName);
            var pdbFunctionLinesFileName =
                GetField(pdbFunctionLinesFile, PdbSourceType, PdbSourceNameFieldName) as string;
            wrappedPdbFunction.SourceFilename = pdbFunctionLinesFileName;

            var pdbFunctionLinesLineArray =
                GetField(lines, PdbLinesType, PdbLinesLinesFieldName) as Array;
            if (pdbFunctionLinesLineArray?.Length > 0)
            {
                var lineNumbers = FilterAndSortLines(pdbFunctionLinesLineArray);

                if (lineNumbers.Count == 0)
                {
                    wrappedPdbFunction.StartLine = CompilerHiddenLine;
                    wrappedPdbFunction.EndLine = CompilerHiddenLine;
                }
                else
                {
                    wrappedPdbFunction.StartLine = lineNumbers.First();
                    wrappedPdbFunction.EndLine = lineNumbers.Last();
                }
            }
        }

        /// <summary>
        /// Returs the field of the given name from the given object of the given type using reflection.
        /// </summary>
        private static object GetField(object obj, Type type, string fieldName)
        {
            return type
                .GetField(fieldName, BindingFlagsInternal)
                .GetValue(obj);
        }

        /// <summary>
        /// Returns the line numbers in the given line array sorted and with compiler hidden lines removed.
        /// There are cases where only the first or last few lines of a method are compiler-hidden.
        /// </summary>
        /// <param name="pdbFunctionLinesLineArray">The array of PdbLine objects</param>
        private static List<uint> FilterAndSortLines(Array pdbFunctionLinesLineArray)
        {
            List<uint> lineNumbers = new List<uint>();
            foreach (object line in pdbFunctionLinesLineArray)
            {
                lineNumbers.Add((uint)GetField(line, PdbLineType, PdbLineLineBeginFieldName));
                lineNumbers.Add((uint)GetField(line, PdbLineType, PdbLineLineEndFieldName));
            }
            lineNumbers = lineNumbers.FindAll(line => line != CompilerHiddenLine);
            lineNumbers.Sort();
            return lineNumbers;
        }
    }
}

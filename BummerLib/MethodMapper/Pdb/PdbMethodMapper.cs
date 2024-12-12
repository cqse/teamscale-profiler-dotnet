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
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Cqse.ConQAT.Dotnet.Bummer
{
	/// <summary>
	/// Method mapper for Microsoft .NET symbol files (.pdb).
	/// </summary>
	public class PdbMethodMapper : MethodMapperBase
	{
		/// <summary>
		/// File extension of Pdb files.
		/// </summary>
		private const string PdbExtension = ".pdb";

		/// <summary>
		/// First bytes (as string) of a portable PDB.
		/// </summary>
		private const string PortablePdbMagic = "BSJB";

		/// <summary>
		/// First bytes (as string) of a native PDB (that is supported by Cecil).
		/// </summary>
		private const string NativePdbMagic = "Microsoft C/C++ MSF 7.00";

		/// <summary>
		/// The different string representations of magic bytes of known PDB types.
		/// </summary>
		private static readonly string[] MagicTypes = { NativePdbMagic, PortablePdbMagic };

		/// <inheritdoc/>
		protected override string GetSupportedExtension() => PdbExtension;

		/// <inheritdoc/>
		protected override IEnumerable<MethodMapping> ReadMethodMappings(string pathToSymbolFile)
		{
            string pdbType = DetectPdbType(pathToSymbolFile);
			using (Stream fileStream = File.OpenRead(pathToSymbolFile))
			{
				switch (pdbType)
				{
					case NativePdbMagic:
						return this.ReadNativePdbMethodMappings(fileStream).ToList();
					case PortablePdbMagic:
						return this.ReadPortablePdbMappings(fileStream).ToList();
					default:
						throw new ArgumentException($"PDB file {pathToSymbolFile} not supported. Unknown binary header.", nameof(pathToSymbolFile));
				}

			}
		}

		/// <summary>
		/// Reads method mappings for portable PDB files.
		/// </summary>
		internal IEnumerable<MethodMapping> ReadPortablePdbMappings(Stream fileStream)
		{
			MetadataReader metadataReader = MetadataReaderProvider.FromPortablePdbStream(fileStream, MetadataStreamOptions.PrefetchMetadata).GetMetadataReader(MetadataReaderOptions.ApplyWindowsRuntimeProjections);
            foreach (MethodDebugInformationHandle methodHandle in metadataReader.MethodDebugInformation)
			{
				MethodDebugInformation debugInfo = metadataReader.GetMethodDebugInformation(methodHandle);
				var sequencePoints = debugInfo.GetSequencePoints().Where(point => !point.IsHidden).ToList();
				if (sequencePoints.Count == 0)
				{
					// If there are no visible lines we cannot map the method to source code, skip.
					continue;
				}

				DocumentHandle docHandle = sequencePoints.First().Document;
				if (docHandle.IsNil)
				{
					docHandle = debugInfo.Document;
				}

				Document doc = metadataReader.GetDocument(docHandle);

				yield return new MethodMapping
				{
					MethodToken = (uint) MetadataTokens.GetToken(methodHandle.ToDefinitionHandle()),
					SourceFile = metadataReader.GetString(doc.Name),
					StartLine = (uint)sequencePoints.First().StartLine,
					EndLine = (uint)sequencePoints.Last().EndLine,
				};
			}
		}

		/// <summary>
		/// Reads method mappings for traditional (native) PDB files.
		/// </summary>
		internal IEnumerable<MethodMapping> ReadNativePdbMethodMappings(Stream fileStream)
		{
			IEnumerable<PdbFunction> pdbFunctions = PdbFile.LoadPdbFunctions(fileStream);
			foreach (PdbFunction pdbFunction in pdbFunctions)
			{
				yield return new MethodMapping
				{
					MethodToken = pdbFunction.Token,
					SourceFile = pdbFunction.SourceFilename,
					StartLine = pdbFunction.StartLine,
					EndLine = pdbFunction.EndLine
				};
			}
		}

		/// <summary>
		/// Returns the magic string that denotes the type of PDB that the file represents, or null for an unknown type.
		/// </summary>
		private static string DetectPdbType(string pdbFile)
		{

			byte[] magicBytes = new byte[MagicTypes.Max(magic => magic.Length)];
			using (FileStream pdbStream = File.OpenRead(pdbFile))
			{
				pdbStream.Read(magicBytes, 0, magicBytes.Length);
			}

			foreach (string magicString in MagicTypes)
			{
				if (magicString.Zip(magicBytes, (magicChar, magicByte) => magicChar == magicByte).All(result => result == true))
				{
					return magicString;
				}
			}

			return null;
		}
	}
}

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

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cqse.ConQAT.Dotnet.Bummer
{
	/// <summary>
	/// Tests for <see cref="PdbFile"/>.
	/// </summary>
	public class PdbFileTest : TestBase
	{
		/// <summary>
		/// The directory of the source files of the test PDBs.
		/// </summary>
		private const string SandboxDirectory = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\";

		/// <summary>
		/// Most basic smoke test.
		/// Makes sure that reading the pdb and loading the enclosed functions
		/// does not throw an exception.
		/// This means that accessing the needed method in the Mono assembly via
		/// reflection is still possible.
		/// </summary>
		[Test]
		public void SmokeTestReflection()
		{
			this.LoadMethodMappingsFromTestFile("Sandbox.pdb");
		}

		/// <summary>
		/// Makes sure that loading the method mappings from the Sandbox Pdb works
		/// and provides the correct results.
		/// </summary>
		[Test]
		public void TestSandbox()
		{
			List<MethodMapping> mappings = this.LoadMethodMappingsFromTestFile("Sandbox.pdb");
			Assert.AreEqual(2, mappings.Count);
			AssertMapping(mappings, 100663297, SandboxDirectory + "Sandbox\\Program.cs", (12, 15));
			AssertMapping(mappings, 100663298, SandboxDirectory + "Sandbox\\Program.cs", (19, 26));
		}

		/// <summary>
		/// Makes sure that loading the method mappings from the Library A Pdb works
		/// and provides the correct results.
		/// </summary>
		[Test]
		public void TestLibraryA()
		{
			List<MethodMapping> mappings = this.LoadMethodMappingsFromTestFile("LibraryARenamedInVersion8.pdb");
			Assert.AreEqual(4, mappings.Count);
			AssertMapping(mappings, 100663297, SandboxDirectory + "LibraryA\\ClassARenamedInVersion7.cs", (11, 17));
			AssertMapping(mappings, 100663298, SandboxDirectory + "LibraryA\\ClassARenamedInVersion7.cs", (21, 23));
			AssertMapping(mappings, 100663300, SandboxDirectory + "LibraryA\\ClassB.cs", (13, 17));
			AssertMapping(mappings, 100663301, SandboxDirectory + "LibraryA\\ClassB.cs", (20, 23));
		}

		/// <summary>
		/// Makes sure that loading the method mappings from the 'UnusedLibrary' Pdb
		/// works and provides the correct results.
		/// </summary>
		[Test]
		public void TestUnusedLibrary()
		{
			List<MethodMapping> mappings = this.LoadMethodMappingsFromTestFile("UnusedLibraryRenamedInVersion8.pdb");
			Assert.AreEqual(3, mappings.Count);
			AssertMapping(mappings, 100663297, SandboxDirectory + "UnusedLibrary\\SecondUnusedclassRenamedInVersion7.cs", (20, 27));
			AssertMapping(mappings, 100663298, SandboxDirectory + "UnusedLibrary\\SecondUnusedclassRenamedInVersion7.cs", (30, 38));
			AssertMapping(mappings, 100663300, SandboxDirectory + "UnusedLibrary\\UnusedClass.cs", (13, 18));
		}

		/// <summary>
		/// Asserts that the method with the given token is mapped to exactly the given line ranges in the given source file.
		/// </summary>
		private static void AssertMapping(List<MethodMapping> mappings, uint methodToken, string sourceFile, params (uint, uint)[] lineRanges)
		{
			MethodMapping mapping = mappings.Single(m => m.MethodToken == methodToken);
			Assert.That(mapping.SourceFileMappings, Has.Count.EqualTo(1));
			Assert.That(mapping.SourceFileMappings[0].SourceFile, Is.EqualTo(sourceFile));
			Assert.That(mapping.SourceFileMappings[0].LineRanges.Select(range => (range.StartLine, range.EndLine)), Is.EqualTo(lineRanges));
		}

		/// <summary>
		/// Loads the method mappings from the specified test file.
		/// </summary>
		private List<MethodMapping> LoadMethodMappingsFromTestFile(string testFileName)
		{
			FileInfo sandboxPdb = this.GetGlobalTestFile(testFileName);
			using (FileStream pdbStream = sandboxPdb.OpenRead())
			{
				return PdbFile.LoadMethodMappings(pdbStream);
			}
		}
	}
}

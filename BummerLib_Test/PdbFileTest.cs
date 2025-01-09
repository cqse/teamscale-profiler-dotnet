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
		/// Most basic smoke test.
		/// Makes sure that reading the pdb and loading the enclosed functions
		/// does not throw an exception.
		/// This means that accessing the needed method in the Mono assembly via
		/// reflection is still possible.
		/// </summary>
		[Test]
		public void SmokeTestReflection()
		{
			this.LoadPdbFunctionsFromTestFile("Sandbox.pdb");
		}

		/// <summary>
		/// Makes sure that loading PdbFunctions from the Sandbox Pdb works
		/// and provides the correct results.
		/// </summary>
		[Test]
		public void TestSandbox()
		{
			List<PdbFunction> functions = this.LoadPdbFunctionsFromTestFile("Sandbox.pdb");
			Assert.AreEqual(2, functions.Count);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 12,
				EndLine = 15,
				Token = 100663297,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\Sandbox\\Program.cs"
			}, functions);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 19,
				EndLine = 26,
				Token = 100663298,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\Sandbox\\Program.cs"
			}, functions);
		}

		/// <summary>
		/// Makes sure that loading PdbFunctions from the Library A Pdb works
		/// and provides the correct results.
		/// </summary>
		[Test]
		public void TestLibraryA()
		{
			List<PdbFunction> functions = this.LoadPdbFunctionsFromTestFile("LibraryARenamedInVersion8.pdb");
			Assert.AreEqual(4, functions.Count);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 11,
				EndLine = 17,
				Token = 100663297,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\LibraryA\\ClassARenamedInVersion7.cs"
			}, functions);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 21,
				EndLine = 23,
				Token = 100663298,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\LibraryA\\ClassARenamedInVersion7.cs"
			}, functions);

			Assert.Contains(new PdbFunction()
			{
				StartLine = 13,
				EndLine = 17,
				Token = 100663300,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\LibraryA\\ClassB.cs"
			}, functions);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 20,
				EndLine = 23,
				Token = 100663301,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\LibraryA\\ClassB.cs"
			}, functions);
		}

		/// <summary>
		/// Makes sure that loading PdbFunctions from the 'UnusedLibrary' Pdb 
		/// works and provides the correct results.
		/// </summary>
		[Test]
		public void TestUnusedLibrary()
		{
			List<PdbFunction> functions = this.LoadPdbFunctionsFromTestFile("UnusedLibraryRenamedInVersion8.pdb");
			Assert.AreEqual(3, functions.Count);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 20,
				EndLine = 27,
				Token = 100663297,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\UnusedLibrary\\SecondUnusedclassRenamedInVersion7.cs"
			}, functions);
			Assert.Contains(new PdbFunction()
			{
				StartLine = 30,
				EndLine = 38,
				Token = 100663298,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\UnusedLibrary\\SecondUnusedclassRenamedInVersion7.cs"
			}, functions);

			Assert.Contains(new PdbFunction()
			{
				StartLine = 13,
				EndLine = 18,
				Token = 100663300,
				SourceFilename = "c:\\cqse\\src\\conqat-engine\\testgap\\eu.cqse.conqat.engine.testgap\\test-data\\sandbox\\08\\Sandbox\\UnusedLibrary\\UnusedClass.cs"
			}, functions);
		}

		/// <summary>
		/// Loads the Pdb functions from the specified test file.
		/// </summary>
		private List<PdbFunction> LoadPdbFunctionsFromTestFile(string testFileName)
		{
			FileInfo sandboxPdb = this.GetGlobalTestFile(testFileName);
			using (FileStream pdbStream = sandboxPdb.OpenRead())
			{
				return PdbFile.LoadPdbFunctions(pdbStream).ToList();
			}
		}
	}
}

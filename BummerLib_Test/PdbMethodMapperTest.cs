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
using System;
using System.IO;

namespace Cqse.ConQAT.Dotnet.Bummer
{
	/// <summary>
	/// Tests for the <see cref="PdbMethodMapper"/>.
	/// </summary>
	public class PdbMethodMapperTest : TestBase
	{
		/// <summary>
		/// Tests reading a legacy PDB file is throwing an exception.
		/// </summary>
		[Test]
		public void TestReadingLegacyPdbWillThrowAnException()
		{
			string pdb = this.GetGlobalTestFile("LegacyPdb.pdb").FullName;
			Assert.Throws<ArgumentException>(() => new PdbMethodMapper().GetMethodMappings(pdb, "LegacyPdb"),
				$"PDB file {pdb} not supported. Unknown binary header.");
		}

		/// <summary>
		/// Tests reading a legacy PDB file is throwing an exception.
		/// </summary>
		[Test]
		public void TestReadingPortablePdbWillNotThrowAnException()
		{
			string pdb = this.GetGlobalTestFile("Newtonsoft.Json.pdb").FullName;
			Assert.DoesNotThrow(() => new PdbMethodMapper().GetMethodMappings(pdb, "Newtonsoft.Json"));
		}

		/// <summary>
		///  Smoke test that verifies that reading the sandbox pdb will not throw an exception.
		/// </summary>
		[Test]
		public void TestReadingSandboxPdb()
		{
			string pdb = this.GetGlobalTestFile("Sandbox.pdb").FullName;
			Assert.DoesNotThrow(() => new PdbMethodMapper().GetMethodMappings(pdb, "Sandbox"));
		}

		/// <summary>
		/// Tests reading a portable PDB file.
		/// </summary>
		[Test]
		public void TestReadPortablePdbMappings()
		{
			using (Stream pdb = File.OpenRead(this.GetGlobalTestFile("Newtonsoft.Json.pdb").FullName))
			{
				// Corresponds to constructor: https://github.com/JamesNK/Newtonsoft.Json/blob/12.0.1/Src/Newtonsoft.Json/JsonException.cs#L55
				AssertMapping(new PdbMethodMapper().ReadPortablePdbMappings(pdb), 100663435, "/_/Src/Newtonsoft.Json/JsonException.cs", (56, 58));
			}
		}

		/// <summary>
		/// Tests that a constructor with a field initializer is mapped to separate line ranges instead of a single
		/// range that spans all members in between.
		/// </summary>
		[Test]
		public void TestReadPortablePdbMappingsOfConstructorWithFieldInitializer()
		{
			using (Stream pdb = File.OpenRead(this.GetGlobalTestFile("Newtonsoft.Json.pdb").FullName))
			{
				// Corresponds to the constructor (line 60) and the initializer of the field _values (line 43): https://github.com/JamesNK/Newtonsoft.Json/blob/12.0.1/Src/Newtonsoft.Json/Linq/JArray.cs#L43
				AssertMapping(new PdbMethodMapper().ReadPortablePdbMappings(pdb), 100665706, "/_/Src/Newtonsoft.Json/Linq/JArray.cs", (43, 43), (60, 62));
			}
		}
	}
}

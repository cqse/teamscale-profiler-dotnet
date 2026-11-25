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
using System.Linq;

namespace Cqse.ConQAT.Dotnet.Bummer
{
	/// <summary>
	/// Tests for the <see cref="LineRangeMerger"/>.
	///
	/// Most test cases use the sequence points of the following class:
	/// <code>
	///  1  class Foo
	///  2  {
	///  3      private List&lt;int&gt; values = new List&lt;int&gt;();
	///  4
	///  5      public int Count()
	///  6      {
	///  7          return values.Count;
	///  8      }
	///  9
	/// 10      public Foo(bool flag)
	/// 11      {
	/// 12          if (flag)
	/// 13          {
	/// 14              Init();
	/// 15          }
	/// 16      }
	/// 17
	/// 18      public void Init() { values.Clear(); }
	/// 19
	/// 20      public string Name { get; } = "x";
	/// 21  }
	/// </code>
	/// </summary>
	public class LineRangeMergerTest
	{
		/// <summary>
		/// The sequence points of <c>Count()</c>: the braces and the return statement.
		/// </summary>
		private static readonly List<(uint, uint)> CountSequencePoints = new List<(uint, uint)> { (6, 6), (7, 7), (8, 8) };

		/// <summary>
		/// The sequence points of the constructor in IL order: the field initializer (line 3), the auto-property
		/// initializer (line 20), the constructor declaration, the braces, the <c>if</c> and the call of <c>Init()</c>.
		/// The braces of the <c>if</c> (lines 13 and 15) have no sequence points.
		/// </summary>
		private static readonly List<(uint, uint)> ConstructorSequencePoints = new List<(uint, uint)> { (3, 3), (20, 20), (10, 10), (11, 11), (12, 12), (14, 14), (16, 16) };

		/// <summary>
		/// The sequence points of <c>Init()</c>: the braces and the statement, all in line 18.
		/// </summary>
		private static readonly List<(uint, uint)> InitSequencePoints = new List<(uint, uint)> { (18, 18), (18, 18), (18, 18) };

		/// <summary>
		/// The sequence point of the getter of <c>Name</c>.
		/// </summary>
		private static readonly List<(uint, uint)> NameGetterSequencePoints = new List<(uint, uint)> { (20, 20) };

		/// <summary>
		/// The constructor is split where other methods are located in between, and the lines in between without code of
		/// other methods (the braces of the <c>if</c> in lines 13 and 15) are added to it.
		/// </summary>
		private static readonly TestCaseData ConstructorWithInitializers = new TestCaseData(
			new[]
			{
				("Foo.cs", CountSequencePoints),
				("Foo.cs", ConstructorSequencePoints),
				("Foo.cs", InitSequencePoints),
				("Foo.cs", NameGetterSequencePoints),
			},
			new[]
			{
				new[] { (6u, 8u) },
				new[] { (3u, 3u), (10u, 16u), (20u, 20u) },
				new[] { (18u, 18u) },
				new[] { (20u, 20u) },
			}).SetName(nameof(ConstructorWithInitializers));

		/// <summary>
		/// Only methods in the same source file split the line ranges of a method. Here, <c>Foo</c> is a partial class and
		/// <c>Count()</c> is located in another file, so it does not split the constructor. A method without sequence points
		/// (e.g. one whose lines are all compiler hidden) has no line ranges.
		/// </summary>
		private static readonly TestCaseData OnlyMethodsInTheSameSourceFile = new TestCaseData(
			new[]
			{
				("Foo.Count.cs", CountSequencePoints),
				("Foo.cs", ConstructorSequencePoints),
				("Foo.cs", InitSequencePoints),
				("Foo.cs", NameGetterSequencePoints),
				("Foo.cs", new List<(uint, uint)>()),
			},
			new[]
			{
				new[] { (6u, 8u) },
				new[] { (3u, 16u), (20u, 20u) },
				new[] { (18u, 18u) },
				new[] { (20u, 20u) },
				new (uint, uint)[0],
			}).SetName(nameof(OnlyMethodsInTheSameSourceFile));

		/// <summary>
		/// A statement that spans several lines and contains a lambda:
		/// <code>
		/// 1  void Run()
		/// 2  {
		/// 3      items.ForEach(item =>
		/// 4      {
		/// 5          Print(item);
		/// 6      });
		/// 7  }
		/// </code>
		/// The sequence points of the lambda are within the multi-line sequence point of <c>Run()</c>, so they do not
		/// split it. The lambda is a method of its own and gets its own line range.
		/// </summary>
		private static readonly TestCaseData MultiLineStatementWithLambda = new TestCaseData(
			new[]
			{
				("Run.cs", new List<(uint, uint)> { (2, 2), (3, 6), (7, 7) }),
				("Run.cs", new List<(uint, uint)> { (4, 4), (5, 5), (6, 6) }),
			},
			new[]
			{
				new[] { (2u, 7u) },
				new[] { (4u, 6u) },
			}).SetName(nameof(MultiLineStatementWithLambda));

		/// <summary>
		/// The test cases: the source file and sequence points of each method, and the expected line ranges of each method.
		/// </summary>
		private static readonly TestCaseData[] TestCases = { ConstructorWithInitializers, OnlyMethodsInTheSameSourceFile, MultiLineStatementWithLambda };

		/// <summary>
		/// Tests that the line ranges of the source file mappings of the given methods are set as expected.
		/// </summary>
		[TestCaseSource(nameof(TestCases))]
		public void TestSetLineRanges((string sourceFile, List<(uint, uint)> lines)[] methods, (uint, uint)[][] expectedLineRanges)
		{
			List<(SourceFileMapping sourceFileMapping, List<(uint, uint)> lines)> sourceFileMappings = methods
				.Select(method => (new SourceFileMapping { SourceFile = method.sourceFile }, method.lines))
				.ToList();

			LineRangeMerger.SetLineRanges(sourceFileMappings);

			Assert.That(sourceFileMappings.Select(entry => entry.sourceFileMapping.LineRanges.Select(range => (range.StartLine, range.EndLine))),
				Is.EqualTo(expectedLineRanges));
		}
	}
}

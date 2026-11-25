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
using System.Linq;

namespace Cqse.ConQAT.Dotnet.Bummer
{
	/// <summary>
	/// Determines which lines of a source file belong to a method, based on the sequence points of all methods.
	///
	/// The profiler only records which methods were called, but the coverage is reported per line. So for each method,
	/// we need the lines that belong to it, and only those:
	/// <list type="bullet">
	/// <item>A single range from the first to the last sequence point is too wide. The sequence points of a method are
	/// not necessarily contiguous, e.g. a constructor also contains the code of the field and auto-property initializers,
	/// which may be located anywhere in the class. Just using the lowest and highest line number as start and end line
	/// could then contain other methods, which would be reported as covered whenever the constructor is called.</item>
	/// <item>The lines of the sequence points alone are too narrow. Lines within a method that have no code, e.g. the
	/// braces of an <c>if</c>, comments and empty lines, have no sequence point and would be reported as not covered.</item>
	/// </list>
	///
	/// Thus, we add the lines between two consecutive sequence points of a method to the method if no method has a
	/// sequence point there.
	///
	/// This could theoretically also include code without sequence points that does not belong to the method, e.g.
	/// abstract methods, interface members and fields without initializers. This is usually harmless since such lines
	/// are not coverable.
	/// </summary>
	internal static class LineRangeMerger
	{
		/// <summary>
		/// Sets the line ranges of each of the given source file mappings by merging the lines of the sequence points of
		/// the method in that source file into sorted, non-overlapping line ranges. Requires the source file mappings of
		/// all methods of the symbol file, since the sequence points of the other methods in the same source file
		/// determine where the line ranges of a method are split.
		/// </summary>
		/// <param name="sourceFileMappings">The source file mapping of a method and the (inclusive) lines of the sequence
		/// points of the method in that source file. A method with sequence points in several source files has one entry
		/// per source file. The lines must not contain compiler hidden lines.</param>
		internal static void SetLineRanges(IList<(SourceFileMapping sourceFileMapping, List<(uint startLine, uint endLine)> lines)> sourceFileMappings)
		{
			Dictionary<string, SortedSet<uint>> startLinesBySourceFile = sourceFileMappings
				.GroupBy(entry => entry.sourceFileMapping.SourceFile ?? string.Empty)
				.ToDictionary(group => group.Key, group => new SortedSet<uint>(group
					.SelectMany(entry => entry.lines)
					.Select(line => line.startLine)));

			foreach ((SourceFileMapping sourceFileMapping, List<(uint startLine, uint endLine)> lines) in sourceFileMappings)
			{
				sourceFileMapping.LineRanges = MergeIntoLineRanges(lines, startLinesBySourceFile[sourceFileMapping.SourceFile ?? string.Empty]);
			}
		}

		/// <summary>
		/// Merges the (inclusive) lines of the sequence points of a method into sorted, non-overlapping line ranges.
		/// </summary>
		/// <param name="lines">The lines of the method's sequence points.</param>
		/// <param name="sourceFileStartLines">The start lines of all sequence points of all methods in the method's source
		/// file, including the method itself.</param>
		private static List<LineRange> MergeIntoLineRanges(IEnumerable<(uint startLine, uint endLine)> lines, SortedSet<uint> sourceFileStartLines)
		{
			List<LineRange> mergedRanges = new List<LineRange>();
			foreach ((uint startLine, uint endLine) in lines.OrderBy(line => line.startLine))
			{
				LineRange lastRange = mergedRanges.LastOrDefault();
				if (lastRange != null && !HasSequencePointBetween(sourceFileStartLines, lastRange.EndLine + 1, startLine - 1))
				{
					lastRange.EndLine = Math.Max(lastRange.EndLine, endLine);
				}
				else
				{
					mergedRanges.Add(new LineRange { StartLine = startLine, EndLine = endLine });
				}
			}
			return mergedRanges;
		}

		/// <summary>
		/// Returns whether a sequence point starts between the given (inclusive) lines.
		/// </summary>
		private static bool HasSequencePointBetween(SortedSet<uint> sequencePointStartLines, uint fromLine, uint toLine)
		{
			return fromLine <= toLine && sequencePointStartLines.GetViewBetween(fromLine, toLine).Count > 0;
		}
	}
}

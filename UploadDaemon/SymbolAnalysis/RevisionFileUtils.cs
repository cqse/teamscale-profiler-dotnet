using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UploadDaemon.SymbolAnalysis
{
    /// <summary>
    /// Utilities for parsing a revision file.
    /// </summary>
    public class RevisionFileUtils
    {
        /// <summary>
        /// Either a revision or a branch + timestamp in format BRANCH:TIMESTAMP.
        /// Used for uploading line coverage to Teamscale.
        /// </summary>
        public class RevisionAndTimestamp
        {
            /// <summary>
            /// The revision.
            /// </summary>
            public string RevisionValue { get; set; }

            /// <summary>
            /// The timestamp.
            /// </summary>
            public string TimestampValue { get; set; }

            public string BranchName { get; set; }

            public override bool Equals(object other) =>
                other is RevisionAndTimestamp revision && revision.RevisionValue.Equals(RevisionValue) && revision.TimestampValue.Equals(TimestampValue) && revision.BranchName.Equals(BranchName);

            public override int GetHashCode() =>
                (RevisionValue, TimestampValue).GetHashCode();

            /// <summary>
            /// Returns the contents of a revision file that represents the same revision or timestamp
            /// as this object.
            /// </summary>
            public string ToRevisionFileContent()
            {
                string result = "";
                if (RevisionValue.Length != 0)
                {
                    result += $"revision: {RevisionValue}";
                }
                if (TimestampValue.Length != 0)
                {
                    if (result.Length > 0)
                    {
                        result += "\n";
                    }
                    result += $"timestamp: {TimestampValue}";
                }
                if (TimestampValue.Length != 0)
                {
                    if (result.Length > 0)
                    {
                        result += "\n";
                    }
                    result += $"branch: {BranchName}";
                }
                return result;
            }
        }

        private static readonly Regex FileContentRegex = new Regex(@"^\s*(timestamp|revision|branch):(.*)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses the given revision file lines. May throw exceptions if the file is malformed or cannot be read.
        /// </summary>
        public static RevisionAndTimestamp Parse(string[] revisionFileLines, string filePath)
        {
            IEnumerable<(string, string)> matches = revisionFileLines.Select(line => FileContentRegex.Match(line))
                .Where(match => match.Success)
                .Select(match => (match.Groups[1].Value, match.Groups[2].Value.Trim()));
            if (matches.Count() == 0)
            {
                throw new InvalidRevisionFileException($"The revision file {filePath} is not valid:" +
                    " found neither a timestamp nor a revision entry." +
                    " Examples: 'timestamp: 1234567890' or 'revision: 123456'");
            }
            string RevisionValue = "";
            string TimestampValue = "";
            string BranchName = "";
            foreach ((string type, string value) in matches)
            {
                switch (type.ToLower())
                {
                    case "timestamp":
                        TimestampValue = value;
                        break;
                    case "revision":
                        RevisionValue = value;
                        break;
                    case "branch":
                        BranchName = value;
                        break;
                    default:
                        throw new InvalidRevisionFileException($"The revision file {filePath} is not valid:" +
                            $" unknown type '{type}'");
                }
            }
            return new RevisionAndTimestamp
            {
                RevisionValue = RevisionValue,
                TimestampValue = TimestampValue,
                BranchName = BranchName
            };
        }

        private class InvalidRevisionFileException : Exception
        {
            public InvalidRevisionFileException(string message) : base(message)
            {
            }
        }
    }
}

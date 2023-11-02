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
        public class RevisionOrTimestamp
        {
            public RevisionOrTimestamp(string value, bool isRevision) {
                this.Value = value; 
                this.IsRevision = isRevision;
            }
            /// <summary>
            /// The timestamp or revision.
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// True if the value is a revision. False if it's a timestamp.
            /// </summary>
            public bool IsRevision { get; set; }

            public override bool Equals(object other) =>
                other is RevisionOrTimestamp revision && revision.Value.Equals(Value) &&
                revision.IsRevision.Equals(IsRevision);

            public override int GetHashCode() =>
                (Value, IsRevision).GetHashCode();

            /// <summary>
            /// Returns the contents of a revision file that represents the same revision or timestamp
            /// as this object.
            /// </summary>
            public string ToRevisionFileContent()
            {
                if (IsRevision)
                {
                    return $"revision: {Value}";
                }
                else
                {
                    return $"timestamp: {Value}";
                }
            }
        }

        private static readonly Regex FileContentRegex = new Regex(@"^\s*(timestamp|revision):(.*)$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses the given revision file lines. May throw exceptions if the file is malformed or cannot be read.
        /// </summary>
        public static List<RevisionOrTimestamp> Parse(string[] revisionFileLines, string filePath)
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
            List<RevisionOrTimestamp> result = new List<RevisionOrTimestamp>();

            foreach ((string type, string value) in matches)
            {
                switch (type.ToLower())
                {
                    case "timestamp":
                        result.Add(new RevisionOrTimestamp(value,false));
                        break;

                    case "revision":
                        result.Add(new RevisionOrTimestamp(value, true));
                        break;

                    default:
                        throw new InvalidRevisionFileException($"The revision file {filePath} is not valid:" +
                            $" unknown type '{type}'");
                }
            }

            return result;
          
        }

        private class InvalidRevisionFileException : Exception
        {
            public InvalidRevisionFileException(string message) : base(message)
            {
            }
        }
    }
}

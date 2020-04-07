using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace UploadDaemon.Configuration
{
    /// <summary>
    /// Maintains a list of include and exclude glob patterns.
    /// Special characters:
    ///
    /// - * matches any number of characters
    /// - ? matches any one character
    ///
    /// Patterns must always match the entire input string.
    /// </summary>
    public class GlobPatternList
    {
        private readonly List<string> includePatterns;
        private readonly List<string> excludePatterns;
        private readonly List<Regex> includeRegexes;
        private readonly List<Regex> excludeRegexes;

        public GlobPatternList(List<string> includePatterns, List<string> excludePatterns)
        {
            this.includePatterns = includePatterns;
            this.excludePatterns = excludePatterns;
            this.includeRegexes = includePatterns.Select(pattern => ConvertGlobPatternToRegex(pattern)).ToList();
            this.excludeRegexes = excludePatterns.Select(pattern => ConvertGlobPatternToRegex(pattern)).ToList();
        }

        private static Regex ConvertGlobPatternToRegex(String pattern)
        {
            StringBuilder regexBuilder = new StringBuilder("^");
            StringBuilder partBuilder = new StringBuilder();

            foreach (char character in pattern.ToCharArray())
            {
                switch (character)
                {
                    case '*':
                        regexBuilder.Append(Regex.Escape(partBuilder.ToString())).Append(".*");
                        partBuilder.Clear();
                        break;

                    case '?':
                        regexBuilder.Append(Regex.Escape(partBuilder.ToString())).Append(".");
                        partBuilder.Clear();
                        break;

                    default:
                        partBuilder.Append(character);
                        break;
                }
            }

            regexBuilder.Append(Regex.Escape(partBuilder.ToString())).Append("$");

            return new Regex(regexBuilder.ToString(), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Returns a human-readable string describing this pattern list.
        /// </summary>
        public string Describe()
        {
            return $"include={string.Join(",", includePatterns)} exclude={string.Join(",", excludePatterns)}";
        }

        /// <summary>
        /// Returns true if at least one include pattern matches the given text and none of the exclude patterns match it.
        /// </summary>
        public Boolean Matches(string text)
        {
            return includeRegexes.Any(regex => regex.IsMatch(text)) && !excludeRegexes.Any(regex => regex.IsMatch(text));
        }

        /// <inheritdoc/>
        public override bool Equals(object other) => other is GlobPatternList otherList && otherList.includePatterns.SequenceEqual(includePatterns)
                && otherList.excludePatterns.SequenceEqual(excludePatterns);

        /// <inheritdoc/>
        public override int GetHashCode() => (includePatterns, excludePatterns).GetHashCode();
    }
}
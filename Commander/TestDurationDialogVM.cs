using Cqse.Teamscale.Profiler.Commons.Ipc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace Cqse.Teamscale.Profiler.Commander
{
    /// <summary>
    /// View model for TestDurationDialog.xaml
    /// </summary>
    public class TestDurationDialogVM : INotifyPropertyChanged
    {
        /// <summary>
        /// Event for view binding changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private string durationString;

        /// <summary>
        /// The duration string entered in the text box.
        /// </summary>
        public string DurationString
        {
            get => durationString;
            set
            {
                SetField(ref durationString, value);
                OnPropertyChanged(nameof(IsValid));
            }
        }

        /// <summary>
        /// Whether the entered string is valid.
        /// </summary>
        public bool IsValid => DurationMs != null;

        public TestDurationDialogVM(long duration)
        {
            durationString = MillisecondsToString(duration);
        }

        /// <summary>
        /// The calculated duration in milliseconds or null if parsing the string failed.
        /// </summary>
        public long? DurationMs => StringToMilliseconds(durationString);

        /// <summary>
        /// Converts a duration to a human-readable string.
        /// </summary>
        public static string MillisecondsToString(long duration)
        {
            long seconds = (duration / 1000 ) % 60;
            long minutes = (duration / 60_000 ) % 60;
            long hours = duration / 3_600_000;
            if (seconds == 0 && minutes == 0 && hours == 0)
            {
                return "1s";
            }

            string result = "";
            string separator = "";
            if (hours > 0)
            {
                result += hours + "h";
                separator = " ";
            }
            if (minutes > 0)
            {
                result += separator + minutes + "m";
                separator = " ";
            }
            if (seconds > 0)
            {
                result += separator + seconds + "s";
            }
            return result;
        }

        /// <summary>
        /// Converts a human-edited string into a duration in milliseconds. Returns null on parsing errors.
        /// </summary>
        public static long? StringToMilliseconds(string durationString)
        {
            if (durationString == "")
            {
                return null;
            }

            MatchCollection matches = Regex.Matches(durationString, @"^\s*(?:(?<hours>\d+)h)?\s*(?:(?<minutes>\d+)m)?\s*(?:(?<seconds>\d+)s)?\s*$");
            if (matches.Count != 1)
            {
                return null;
            }

            Match match = matches[0];
            int hours = ParseNumberFromMatchGroup(match, "hours");
            int minutes = ParseNumberFromMatchGroup(match, "minutes");
            int seconds = ParseNumberFromMatchGroup(match, "seconds");

            long milliseconds = hours * 3_600_000 + minutes * 60_000 + seconds * 1000;
            if (milliseconds <= 0)
            {
                return null;
            }
            return milliseconds;
        }

        private static int ParseNumberFromMatchGroup(Match match, string groupName)
        {
           Group group = match.Groups[groupName];
            if (group == null)
            {
                return 0;
            }
            if (int.TryParse(group.Value, out int milliseconds))
            {
                return milliseconds;
            }
            return 0;
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

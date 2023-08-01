using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cqse.Teamscale.Profiler.Commander
{
    public class TestDurationDialogVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string durationString;

        public string DurationString
        {
            get => durationString;
            set
            {
                SetField(ref durationString, value);
                OnPropertyChanged(nameof(IsValid));
            }
        }

        public bool IsValid => DurationMs != null;

        public string InputBoxBackground
        {
            get
            {
                if (IsValid)
                {
                    return "#FFFFFFFF";
                }
                return "#FFFFCCCC";
            }
        }

        public TestDurationDialogVM(long duration)
        {
            durationString = MillisecondsToString(duration);
        }

        public long? DurationMs => StringToMilliseconds(durationString);

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

        public static long? StringToMilliseconds(string durationString)
        {
            return null;
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

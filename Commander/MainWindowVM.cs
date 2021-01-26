using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Cqse.Teamscale.Profiler.Commander
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string testName = null;

        public string TestName
        {
            get => testName;
            set
            {
                SetField(ref testName, value);
                OnPropertyChanged(nameof(CanStart));
            }
        }

        private string buttonText = null;

        public string ButtonText
        {
            get => buttonText;
            set => SetField(ref buttonText, value);
        }

        private bool isStopped = false;

        public bool IsStopped
        {
            get => isStopped;
            set
            {
                SetField(ref isStopped, value);
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(CanStart));
            }
        }

        public bool IsRunning
        {
            get => !IsStopped;
        }

        public bool CanStart
        {
            get => IsStopped && !string.IsNullOrEmpty(testName);
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

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Cqse.Teamscale.Profiler.Commander
{
    internal class MainWindowViewModel : INotifyPropertyChanged
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

        private bool isRunning = false;

        public bool IsRunning
        {
            get => isRunning;
            set
            {
                SetField(ref isRunning, value);
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(CanStart));
            }
        }

        public bool CanStart
        {
            get => !IsRunning && !string.IsNullOrEmpty(testName) && IsValidTestName();
        }

        private bool IsValidTestName()
        {
            if (TestNamePattern == null) {
                return true;
            }

            return TestNamePattern.IsMatch(testName);
        }

        public Regex TestNamePattern { get; internal set; }

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

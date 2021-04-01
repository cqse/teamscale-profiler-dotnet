using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Cqse.Teamscale.Profiler.Commander
{
    internal class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string testName = null;
        private BindingList<string> previousTests = new BindingList<string>();
        private HashSet<string> previousTestSet = new HashSet<string>();

        public string TestName
        {
            get => testName;
            set
            {
                SetField(ref testName, value);
                OnPropertyChanged(nameof(CanStart));
            }
        }

        public BindingList<string> PreviousTests
        {
            get => previousTests;
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

                // The following Block is inefficient but the list has at most 10 members, so it shouldn't be a problem.
                if (String.IsNullOrEmpty(TestName))
                {
                    return;
                }
                if (previousTestSet.Add(TestName))
                {
                    previousTests.Insert(0, TestName);
                    if (previousTests.Count > 10)
                    {
                        previousTests.RemoveAt(previousTests.Count - 1);
                    }
                }
                else
                {
                    string temp = TestName;
                    previousTests.Remove(temp);
                    previousTests.Insert(0, temp);
                }

                OnPropertyChanged(nameof(previousTests));
            }
        }

        public bool IsRunning
        {
            get => !IsStopped;
        }

        public bool CanStart
        {
            get => IsStopped && !string.IsNullOrEmpty(testName) && IsValidTestName();
        }

        private bool IsValidTestName()
        {
            if (TestNamePattern == null)
            {
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

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
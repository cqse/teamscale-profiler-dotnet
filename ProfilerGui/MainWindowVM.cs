using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProfilerGui
{
    class MainWindowVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private bool enableProfiling = true;
        public bool EnableProfiling
        { 
            get => enableProfiling;
            set => SetField(ref enableProfiling, value);
        }

        private bool testWiseProfiling = false;
        public bool TestWiseProfiling
        {
            get => testWiseProfiling;
            set => SetField(ref testWiseProfiling, value);
        }

        private string testName = null;
        public string TestName
        {
            get => testName;
            set => SetField(ref testName, value);
        }

        private bool needsUpdate = false;
        public bool NeedsUpdate
        {
            get => needsUpdate;
            set => SetField(ref needsUpdate, value);
        }


        private string statusText = null;
        public string StatusText
        {
            get => statusText;
            set => SetField(ref statusText, value);
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);

            if (propertyName != nameof(NeedsUpdate) && propertyName != nameof(StatusText))
            {
                NeedsUpdate = true;
            }
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

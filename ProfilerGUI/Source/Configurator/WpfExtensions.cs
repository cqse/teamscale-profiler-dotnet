using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Extensions to make working with WPF easier.
    /// </summary>
    public static class WpfExtensions
    {
        /// <summary>
        /// Raises the OnPropertyChangedEvent of an INotifyPropertyChanged object.
        /// </summary>
        public static void Raise(this PropertyChangedEventHandler handler, INotifyPropertyChanged sender, [CallerMemberName] string propertyName = "")
        {
            handler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Re-raises the OnPropertyChangedEvent of a descendant INotifyPropertyChanged object.
        /// </summary>
        public static void ReRaise(this PropertyChangedEventHandler handler, INotifyPropertyChanged sender, string originalSenderName, PropertyChangedEventArgs originalArgs)
        {
            if (originalArgs.PropertyName == null)
            {
                handler.Raise(sender, null);
            }
            else
            {
                handler.Raise(sender, originalSenderName + "." + originalArgs.PropertyName);
            }
        }
    }
}
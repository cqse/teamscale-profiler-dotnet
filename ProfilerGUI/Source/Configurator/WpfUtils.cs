using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Extensions to make working with WPF easier.
    /// </summary>
    public static class WpfUtils
    {
        /// <summary>
        /// Opens a folder chooser and calls the given action with the selected folder if the user chose one.
        /// </summary>
        public static void OpenFolderChooser(Action<string> pathSelectedAction)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    pathSelectedAction.Invoke(dialog.SelectedPath);
                }
            }
        }

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
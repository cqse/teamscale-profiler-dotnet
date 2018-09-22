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
    /// Utilities to make working with the UI easier.
    /// </summary>
    public static class UiUtils
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
                    pathSelectedAction(dialog.SelectedPath);
                }
            }
        }

        /// <summary>
        /// Raises the OnPropertyChangedEvent of an INotifyPropertyChanged object.
        /// </summary>
        public static void Raise(PropertyChangedEventHandler handler, INotifyPropertyChanged sender, [CallerMemberName] string propertyName = "")
        {
            handler?.Invoke(sender, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Re-raises the OnPropertyChangedEvent of a descendant INotifyPropertyChanged object.
        /// </summary>
        public static void ReRaise(PropertyChangedEventHandler handler, INotifyPropertyChanged sender, string originalSenderName, PropertyChangedEventArgs originalArgs)
        {
            if (originalArgs.PropertyName == null)
            {
                Raise(handler, sender, null);
            }
            else
            {
                Raise(handler, sender, originalSenderName + "." + originalArgs.PropertyName);
            }
        }
    }
}
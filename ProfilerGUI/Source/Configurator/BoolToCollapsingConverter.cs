using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Converts a boolean to either Visibility.Visible or Visibility.Collapsed
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToCollapsingConverter : IValueConverter
    {
        /// <summary>
        /// Converts from bool to Visibility.
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = (bool)value;
            if (isVisible)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        /// <summary>
        /// Converts from Visibility to bool.
        /// Not needed, thus not implemented.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
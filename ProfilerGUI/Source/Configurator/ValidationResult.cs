using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProfilerGUI.Source.Configurator
{
    /// <summary>
    /// Result of validating something.
    /// </summary>
    internal class ValidationResult
    {
        /// <summary>
        /// Whether validation succeeded.
        /// </summary>
        public bool Successful { get; }

        /// <summary>
        /// A message to show to the user.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The color that should be used to display this result in the UI.
        /// </summary>
        public Brush Color
        {
            get
            {
                if (Successful)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                return new SolidColorBrush(Colors.Red);
            }
        }

        public ValidationResult(bool successful, string message)
        {
            Successful = successful;
            Message = message;
        }
    }
}
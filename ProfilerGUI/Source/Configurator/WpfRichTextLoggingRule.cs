using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ProfilerGUI.Source.Configurator
{
    public class WpfRichTextTarget : TargetWithLayout
    {
        private readonly RichTextBox RichTextBox;

        public WpfRichTextTarget(RichTextBox richTextBox)
        {
            RichTextBox = richTextBox;
        }

        protected override void Write(LogEventInfo logEvent)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => WriteOnUiThread(logEvent));
        }

        private void WriteOnUiThread(LogEventInfo logEvent)
        {
            string message = Layout.Render(logEvent) + "\r\n";

            TextRange range = new TextRange(RichTextBox.Document.ContentEnd, RichTextBox.Document.ContentEnd)
            {
                Text = message
            };

            Color textColor = GetLogLevelColor(logEvent);
            range.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(textColor));

            RichTextBox.ScrollToEnd();
        }

        private static Color GetLogLevelColor(LogEventInfo logEvent)
        {
            Color textColor;
            if (logEvent.Level == LogLevel.Debug)
            {
                textColor = Colors.DarkMagenta;
            }
            else if (logEvent.Level == LogLevel.Error || logEvent.Level == LogLevel.Fatal)
            {
                textColor = Colors.DarkRed;
            }
            else if (logEvent.Level == LogLevel.Warn)
            {
                textColor = Colors.DarkOrange;
            }
            else
            {
                textColor = Colors.Black;
            }

            return textColor;
        }
    }
}
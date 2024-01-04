using System.Windows;

namespace QuickPaste.Utilities
{
    public static class ClipboardHelper
    {
        /// <summary>
        /// Sets the specified text to the clipboard.
        /// </summary>
        /// <param name="text">The text to be set to the clipboard.</param>
        public static void SetText(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Clipboard.SetText(text);
            });
        }
    }
}
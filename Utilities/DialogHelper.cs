using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QuickPaste.Utilities
{
    public static class DialogHelper
    {
        /// <summary>
        /// Displays a temporary popup with a specified message for a given duration.
        /// </summary>
        /// <param name="owner">The owner window or control for the popup.</param>
        /// <param name="message">The message to be displayed in the popup.</param>
        /// <param name="displaySeconds">The duration in seconds for which the popup will be displayed. Default is 1 second.</param>
        public static async Task ShowTemporaryPopup(string message, int displaySeconds = 1)
        {
            var popup = new Popup
            {
                Placement = PlacementMode.Center,
                PlacementTarget = Application.Current.MainWindow,
                IsOpen = true,
                PopupAnimation = PopupAnimation.Fade,
                Child = new TextBlock
                {
                    Text = message,
                    Background = Brushes.LightGray,
                    Padding = new Thickness(10)
                }
            };

            await Task.Delay(TimeSpan.FromSeconds(displaySeconds));

            if (Application.Current.MainWindow.Dispatcher.CheckAccess())
            {
                popup.IsOpen = false;
            }
            else
            {
                Application.Current.MainWindow.Dispatcher.Invoke(() => popup.IsOpen = false);
            }
        }

        /// <summary>
        /// Displays an error message box.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        public static void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Displays a yes/no confirmation dialog.
        /// </summary>
        /// <param name="message">The message to be displayed.</param>
        /// <returns>True if 'Yes' is clicked, false otherwise.</returns>
        public static bool ShowYesNoDialog(string message)
        {
            var result = MessageBox.Show(message, "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }
    }
}
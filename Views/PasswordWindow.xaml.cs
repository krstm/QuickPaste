using QuickPaste.Utilities;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickPaste
{
    /// <summary>
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        public SecureString Password { get; private set; }

        public PasswordWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the click event of the confirm button. Attempts to confirm the password.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data.</param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            TryConfirmPassword();
        }

        /// <summary>
        /// Handles the KeyDown event of the PasswordBox. Checks if the Enter key was pressed to confirm the password.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data containing key information.</param>
        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryConfirmPassword();
            }
        }

        /// <summary>
        /// Validates the password and sets it if valid. Closes the window on successful validation.
        /// </summary>
        private void TryConfirmPassword()
        {
            if (IsValidPassword(PasswordBox.SecurePassword))
            {
                Password = PasswordBox.SecurePassword;
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Validates the length of the given SecureString password and checks its validity by attempting to decrypt a sample file.
        /// </summary>
        /// <param name="securePassword">The SecureString password to validate.</param>
        /// <returns>True if the password is valid, meets length requirements, and successfully decrypts the sample file; otherwise, false.</returns>
        private bool IsValidPassword(SecureString securePassword)
        {
            if (securePassword.Length >= 4 && securePassword.Length <= 32)
            {
                var checkPassword = JsonFileHandler.CheckPassword(AppConstants.QuickPasteSettingsFilePath, securePassword);
                if (checkPassword)
                {
                    return true;
                }
                else
                {
                    DialogHelper.ShowErrorMessage("Password is invalid.");
                    return false;
                }
            }
            else
            {
                DialogHelper.ShowErrorMessage("Password is invalid. It must be between 4 and 32 characters.");
                return false;
            }
        }

        /// <summary>
        /// Handles the Loaded event of the window. Sets focus to the PasswordBox for user convenience.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            PasswordBox.Focus();
        }
    }
}
using Newtonsoft.Json.Linq;
using QuickPaste.Models;
using QuickPaste.Utilities;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace QuickPaste
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SecureString EncryptionKey { get; }

        public MainWindow()
        {
            InitializeComponent();
            PasswordWindow passwordWindow = new PasswordWindow();
            if (passwordWindow.ShowDialog() == true)
            {
                this.EncryptionKey = passwordWindow.Password;
                LoadButtonsFromJson(AppConstants.QuickPasteSettingsFilePath);
            }
            else
            {
                Close();
            }
        }

        /// <summary>
        /// Creates a Button based on the provided ButtonModel.
        /// </summary>
        /// <param name="buttonModel">ButtonModel containing button information.</param>
        /// <returns>A new Button instance.</returns>
        private Button CreateButton(ButtonModel buttonModel)
        {
            if (string.IsNullOrWhiteSpace(buttonModel.ButtonName) || string.IsNullOrWhiteSpace(buttonModel.CopyText))
            {
                DialogHelper.ShowErrorMessage("Missing 'ButtonName' or 'CopyText'.");
                return null;
            }

            var button = new Button
            {
                Content = buttonModel.ButtonName,
                ToolTip = buttonModel.ButtonName,
                Margin = new Thickness(5),
                Style = (Style)FindResource(AppConstants.ButtonStyleResourceKey)
            };

            ToolTipService.SetInitialShowDelay(button, 0);

            button.Click += (s, e) =>
            {
                ClipboardHelper.SetText(buttonModel.CopyText);
                DialogHelper.ShowTemporaryPopup("Copied");
            };

            var contextMenu = new ContextMenu();
            var menuItemDelete = new MenuItem { Header = "Delete" };
            menuItemDelete.Click += (s, e) => ConfirmDeleteButton(buttonModel.ButtonName);
            contextMenu.Items.Add(menuItemDelete);

            button.ContextMenu = contextMenu;

            return button;
        }

        /// <summary>
        /// Adds buttons to the specified panel based on the given JArray of buttons.
        /// </summary>
        /// <param name="buttons">JArray of button information.</param>
        /// <param name="panel">The panel to which buttons will be added.</param>
        private void AddButtonsToPanel(JArray buttons, Panel panel)
        {
            if (buttons == null)
            {
                DialogHelper.ShowErrorMessage("Button array is null.");
                return;
            }

            if (panel == null)
            {
                DialogHelper.ShowErrorMessage("Panel is null.");
                return;
            }

            foreach (var buttonInfo in buttons)
            {
                var buttonModel = JsonFileHandler.ConvertToList<ButtonModel>(new JArray(buttonInfo)).FirstOrDefault();
                if (buttonModel != null)
                {
                    var button = CreateButton(buttonModel);
                    if (button != null)
                    {
                        panel.Children.Add(button);
                    }
                }
                else
                {
                    DialogHelper.ShowErrorMessage("Error converting JSON to ButtonModel.");
                }
            }
        }

        /// <summary>
        /// Loads buttons from JSON file and adds them to the panel.
        /// </summary>
        /// <param name="filePath">The file path of the JSON file.</param>
        private async Task LoadButtonsFromJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                DialogHelper.ShowErrorMessage("File path is null or empty.");
                return;
            }

            var buttons = await JsonFileHandler.ReadAndValidateJson(filePath, EncryptionKey);
            if (buttons == null)
            {
                DialogHelper.ShowErrorMessage("Invalid password or file format error.");
                return;
            }

            if (!(FindName(AppConstants.ButtonsPanelResourceKey) is Panel panel))
            {
                DialogHelper.ShowErrorMessage("Panel not found in the current context.");
                return;
            }

            AddButtonsToPanel(buttons, panel);
        }

        private async Task AddButton(string buttonName, string copyText)
        {
            var buttons = await JsonFileHandler.ReadAndValidateJson(AppConstants.QuickPasteSettingsFilePath, EncryptionKey);
            if (buttons == null)
            {
                DialogHelper.ShowErrorMessage("Failed to load buttons.");
                return;
            }

            var buttonModels = JsonFileHandler.ConvertToList<ButtonModel>(buttons);
            if (buttonModels.Any(b => b.ButtonName == buttonName))
            {
                DialogHelper.ShowErrorMessage("A button with this name already exists.");
                return;
            }

            var newButtonModel = new ButtonModel
            {
                ButtonName = buttonName,
                CopyText = copyText
            };
            buttonModels.Add(newButtonModel);

            var updatedButtonsJArray = JArray.FromObject(buttonModels);
            await JsonFileHandler.WriteToJsonFile(AppConstants.QuickPasteSettingsFilePath, updatedButtonsJArray, EncryptionKey);
            RefreshButtons();
        }

        /// <summary>
        /// Removes an existing button from the JSON configuration file and refreshes the UI.
        /// </summary>
        /// <param name="buttonName">The name of the button to be removed.</param>
        /// <remarks>
        /// This method searches for a button with the specified name and removes it from the configuration.
        /// If the button is not found, it shows a message box.
        /// </remarks>
        private async Task RemoveButton(string buttonName)
        {
            await JsonFileHandler.RemoveButtonFromJson(AppConstants.QuickPasteSettingsFilePath, buttonName, EncryptionKey);
            RefreshButtons();
        }

        /// <summary>
        /// Refreshes the UI by reloading the buttons from the JSON configuration file.
        /// </summary>
        /// <remarks>
        /// This method clears the current buttons from the UI and then reloads them from the configuration file.
        /// If the configuration file is empty or invalid, it shows a message box.
        /// </remarks>
        private async Task RefreshButtons()
        {
            var buttons = await JsonFileHandler.ReadAndValidateJson(AppConstants.QuickPasteSettingsFilePath, EncryptionKey);
            if (buttons == null)
            {
                DialogHelper.ShowErrorMessage("No buttons data found or invalid file format.");
                return;
            }

            if (!(FindName(AppConstants.ButtonsPanelResourceKey) is Panel panel))
            {
                DialogHelper.ShowErrorMessage("Panel not found in the current context.");
                return;
            }

            panel.Children.Clear();
            AddButtonsToPanel(buttons, panel);
        }

        /// <summary>
        /// Opens the popup for adding a new button.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data.</param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            addButtonPopup.IsOpen = true;
        }

        /// <summary>
        /// Confirms the addition of a new button. Validates the input fields and adds the button if valid.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data.</param>
        private void ConfirmAddButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = newButtonName.Text;
            string copyText = newCopyText.Text;

            if (!string.IsNullOrWhiteSpace(buttonName) && !string.IsNullOrWhiteSpace(copyText))
            {
                AddButton(buttonName, copyText);
                addButtonPopup.IsOpen = false;
                newButtonName.Text = string.Empty;
                newCopyText.Text = string.Empty;
            }
            else
            {
                DialogHelper.ShowErrorMessage("Button name and copy text cannot be empty.");
            }
        }

        /// <summary>
        /// Confirms the deletion of a button. Displays a confirmation dialog and deletes the button if confirmed.
        /// </summary>
        /// <param name="buttonName">The name of the button to be deleted.</param>
        private async Task ConfirmDeleteButton(string buttonName)
        {
            var result = DialogHelper.ShowYesNoDialog($"Are you sure you want to delete '{buttonName}'?");

            if (result)
            {
                await RemoveButton(buttonName);
            }
        }

        /// <summary>
        /// Handles the click event of the close button in the popup.
        /// Closes the popup when the button is clicked.
        /// </summary>
        /// <param name="sender">The object that triggered the event.</param>
        /// <param name="e">Event data.</param>
        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            addButtonPopup.IsOpen = false;
            newButtonName.Text = string.Empty;
            newCopyText.Text = string.Empty;
        }
    }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace QuickPaste
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadButtonsFromJson(_quickPasteSettingsFilePath);
        }

        private readonly string _quickPasteSettingsFilePath = "QuickPasteSettings.json";

        /// <summary>
        /// Reads and validates the JSON file. If the file does not exist, creates a new one.
        /// </summary>
        /// <param name="filePath">The file path of the JSON file.</param>
        /// <returns>A JArray of button information.</returns>
        private async Task<JArray> ReadAndValidateJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("File path is null or empty.");
                return null;
            }

            if (!File.Exists(filePath))
            {
                return await CreateInitialJsonFile(filePath);
            }

            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                if (string.IsNullOrEmpty(json))
                {
                    MessageBox.Show("File content is empty.");
                    return null;
                }

                return JArray.Parse(json);
            }
            catch (JsonReaderException jrex)
            {
                MessageBox.Show($"JSON parsing error: {jrex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates a Button based on the provided JSON token.
        /// </summary>
        /// <param name="buttonInfo">JSON token containing button information.</param>
        /// <returns>A new Button instance.</returns>
        private Button CreateButton(JToken buttonInfo)
        {
            if (buttonInfo["ButtonName"] == null || buttonInfo["CopyText"] == null)
            {
                MessageBox.Show("Missing 'ButtonName' or 'CopyText'.");
                return null;
            }

            var button = new Button
            {
                Content = buttonInfo["ButtonName"].ToString(),
                ToolTip = buttonInfo["ButtonName"].ToString(),
                Margin = new Thickness(5),
                Style = (Style)FindResource("ButtonStyle")
            };

            ToolTipService.SetInitialShowDelay(button, 0);

            button.Click += (s, e) =>
            {
                Clipboard.SetText(buttonInfo["CopyText"].ToString());
                ShowTemporaryPopup("Copied");
            };

            var contextMenu = new ContextMenu();
            var menuItemDelete = new MenuItem { Header = "Delete" };
            menuItemDelete.Click += (s, e) => ConfirmDeleteButton(buttonInfo["ButtonName"].ToString());
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
                MessageBox.Show("Button array is null.");
                return;
            }

            if (panel == null)
            {
                MessageBox.Show("Panel is null.");
                return;
            }

            foreach (var buttonInfo in buttons)
            {
                var button = CreateButton(buttonInfo);
                if (button != null)
                {
                    panel.Children.Add(button);
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
                MessageBox.Show("File path is null or empty.");
                return;
            }

            var buttons = await ReadAndValidateJson(filePath);
            if (buttons == null || !buttons.Any())
            {
                MessageBox.Show("No buttons data found in the file or invalid file format.");
                return;
            }

            if (!(FindName("buttonsPanel") is Panel panel))
            {
                MessageBox.Show("Panel not found in the current context.");
                return;
            }

            AddButtonsToPanel(buttons, panel);
        }

        /// <summary>
        /// Creates an initial JSON file with default button data if the file does not exist.
        /// </summary>
        /// <param name="filePath">The file path where the JSON file will be created.</param>
        /// <returns>A JArray of initial button information.</returns>
        private async Task<JArray> CreateInitialJsonFile(string filePath)
        {
            try
            {
                var initialButton = new JArray
                {
                    new JObject
                    {
                        ["ButtonName"] = "Test",
                        ["CopyText"] = "Test"
                    }
                };
                await File.WriteAllTextAsync(filePath, initialButton.ToString());
                ShowTemporaryPopup("New JSON file created.", 2);
                return initialButton;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create initial JSON file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Displays a temporary popup with a specified message for a given duration.
        /// </summary>
        /// <param name="message">The message to be displayed in the popup.</param>
        /// <param name="displaySeconds">The duration in seconds for which the popup will be displayed. Default is 1 second.</param>
        private async Task ShowTemporaryPopup(string message, int displaySeconds = 1)
        {
            var popup = new Popup
            {
                Placement = PlacementMode.Center,
                PlacementTarget = this,
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

            popup.IsOpen = false;
        }

        /// <summary>
        /// Adds a new button to the JSON configuration file and refreshes the UI.
        /// </summary>
        /// <param name="buttonName">The name of the button to be added.</param>
        /// <param name="copyText">The text that will be copied to the clipboard when this button is clicked.</param>
        /// <remarks>
        /// This method checks if a button with the same name already exists to avoid duplicates.
        /// If a duplicate is found, it shows a message box and does not add the button.
        /// </remarks>
        private async Task AddButton(string buttonName, string copyText)
        {
            var buttons = await ReadAndValidateJson(_quickPasteSettingsFilePath);
            if (buttons == null)
            {
                MessageBox.Show("Failed to load buttons.");
                return;
            }

            if (buttons.Any(b => b["ButtonName"].ToString() == buttonName))
            {
                MessageBox.Show("A button with this name already exists.");
                return;
            }

            var newButton = new JObject
            {
                ["ButtonName"] = buttonName,
                ["CopyText"] = copyText
            };
            buttons.Add(newButton);
            await File.WriteAllTextAsync(_quickPasteSettingsFilePath, buttons.ToString());
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
            var buttons = await ReadAndValidateJson(_quickPasteSettingsFilePath);
            if (buttons == null)
            {
                MessageBox.Show("Failed to load buttons.");
                return;
            }

            var buttonToRemove = buttons.FirstOrDefault(b => b["ButtonName"].ToString() == buttonName);
            if (buttonToRemove == null)
            {
                MessageBox.Show("Button not found.");
                return;
            }

            buttons.Remove(buttonToRemove);
            await File.WriteAllTextAsync(_quickPasteSettingsFilePath, buttons.ToString());
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
            var buttons = await ReadAndValidateJson(_quickPasteSettingsFilePath);
            if (buttons == null || !buttons.Any())
            {
                MessageBox.Show("No buttons data found or invalid file format.");
                return;
            }

            if (!(FindName("buttonsPanel") is Panel panel))
            {
                MessageBox.Show("Panel not found in the current context.");
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
            }
            else
            {
                MessageBox.Show("Buton adı ve kopyalama metni boş bırakılamaz.");
            }
        }

        /// <summary>
        /// Confirms the deletion of a button. Displays a confirmation dialog and deletes the button if confirmed.
        /// </summary>
        /// <param name="buttonName">The name of the button to be deleted.</param>
        private async Task ConfirmDeleteButton(string buttonName)
        {
            var result = MessageBox.Show($"Are you sure you want to delete '{buttonName}'?",
                                         "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
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
        }
    }
}
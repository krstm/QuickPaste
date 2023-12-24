using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

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
        private JArray ReadAndValidateJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("File path is null or empty.");
                return null;
            }

            if (!File.Exists(filePath))
            {
                return CreateInitialJsonFile(filePath);
            }

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrEmpty(json))
            {
                MessageBox.Show("File content is empty.");
                return null;
            }

            try
            {
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
        private void LoadButtonsFromJson(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("File path is null or empty.");
                return;
            }

            var buttons = ReadAndValidateJson(filePath);
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
        private JArray CreateInitialJsonFile(string filePath)
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
                File.WriteAllText(filePath, initialButton.ToString());
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
        private void ShowTemporaryPopup(string message, int displaySeconds = 1)
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

            var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(displaySeconds) };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                popup.IsOpen = false;
            };
            timer.Start();
        }
    }
}
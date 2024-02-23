using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuickPaste.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace QuickPaste.Utilities
{
    public static class JsonFileHandler
    {
        /// <summary>
        /// Reads the encrypted JSON file, decrypts it, and returns a JArray of button information.
        /// </summary>
        /// <param name="filePath">The file path of the JSON file.</param>
        /// <param name="encryptionKey">The SecureString encryption key.</param>
        /// <returns>A JArray of button information if successful, otherwise null.</returns>
        public static async Task<JArray> ReadAndValidateJson(string filePath, SecureString encryptionKey)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                DialogHelper.ShowErrorMessage("File path is null or empty.");
                return null;
            }

            if (!File.Exists(filePath))
            {
                return await CreateInitialJsonFile(filePath, encryptionKey);
            }

            try
            {
                var encryptedJson = await File.ReadAllTextAsync(filePath);
                var decryptedJson = EncryptionHelper.DecryptString(encryptedJson, ConvertToUnsecureString(encryptionKey));
                if (string.IsNullOrEmpty(decryptedJson))
                {
                    DialogHelper.ShowErrorMessage("Invalid password or corrupted file.");
                    return null;
                }
                return JArray.Parse(decryptedJson);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowErrorMessage($"Error reading JSON file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Writes the given JArray content to a JSON file after encrypting it.
        /// </summary>
        /// <param name="filePath">The file path where the JSON file will be written.</param>
        /// <param name="content">The JArray content to write to the file.</param>
        /// <param name="encryptionKey">The SecureString encryption key.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static async Task WriteToJsonFile(string filePath, JArray content, SecureString encryptionKey)
        {
            try
            {
                string json = content.ToString();
                var encryptedJson = EncryptionHelper.EncryptString(json, ConvertToUnsecureString(encryptionKey));
                await File.WriteAllTextAsync(filePath, encryptedJson);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to write to JSON file: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates an initial JSON file with default button data if the file does not exist and encrypts it.
        /// </summary>
        /// <param name="filePath">The file path where the JSON file will be created.</param>
        /// <param name="encryptionKey">The SecureString encryption key.</param>
        /// <returns>A JArray of initial button information if successful, otherwise null.</returns>
        public static async Task<JArray> CreateInitialJsonFile(string filePath, SecureString encryptionKey)
        {
            try
            {
                var initialButton = new JArray
                {
                    new JObject
                    {
                        ["ButtonName"] = "Title",
                        ["CopyText"] = "Copied Text"
                    }
                };
                var encryptedJson = EncryptionHelper.EncryptString(initialButton.ToString(), ConvertToUnsecureString(encryptionKey));
                await File.WriteAllTextAsync(filePath, encryptedJson);

                DialogHelper.ShowTemporaryPopup("New JSON file created.", 2);
                return initialButton;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create initial JSON file: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts a SecureString to an unsecured string.
        /// </summary>
        /// <param name="secureString">The SecureString to convert.</param>
        /// <returns>An unsecured string representation of the SecureString.</returns>
        private static string ConvertToUnsecureString(SecureString secureString)
        {
            IntPtr unmanagedString = IntPtr.Zero;
            try
            {
                unmanagedString = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secureString);
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }

        /// <summary>
        /// Removes a specified button from the JSON configuration file and updates the file.
        /// </summary>
        /// <param name="filePath">The file path of the JSON file.</param>
        /// <param name="buttonName">The name of the button to be removed.</param>
        /// <param name="encryptionKey">The SecureString encryption key.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public static async Task RemoveButtonFromJson(string filePath, string buttonName, SecureString encryptionKey)
        {
            var buttonsJson = await ReadAndValidateJson(filePath, encryptionKey);
            if (buttonsJson == null)
            {
                MessageBox.Show("Failed to load buttons.");
                return;
            }

            var buttonModels = ConvertToList<ButtonModel>(buttonsJson);
            var buttonToRemove = buttonModels.FirstOrDefault(b => b.ButtonName == buttonName);
            if (buttonToRemove != null)
            {
                buttonModels.Remove(buttonToRemove);
                var updatedButtonsJArray = JArray.FromObject(buttonModels);
                await WriteToJsonFile(filePath, updatedButtonsJArray, encryptionKey);
            }
            else
            {
                MessageBox.Show("Button not found.");
            }
        }

        /// <summary>
        /// Converts a JArray to a list of specified type.
        /// </summary>
        /// <typeparam name="T">The type of objects in the list.</typeparam>
        /// <param name="jArray">The JArray to convert.</param>
        /// <returns>A list of objects of the specified type.</returns>
        public static List<T> ConvertToList<T>(JArray jArray)
        {
            try
            {
                return jArray.ToObject<List<T>>();
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"JSON conversion error: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Checks the validity of the password by attempting to decrypt the content of an encrypted JSON file.
        /// </summary>
        /// <param name="filePath">The file path of the encrypted JSON file.</param>
        /// <param name="encryptionKey">The SecureString encryption key to use for decryption.</param>
        /// <returns>True if the password is valid and the file is successfully decrypted, otherwise false.</returns>
        public static bool CheckPassword(string filePath, SecureString encryptionKey)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                CreateInitialJsonFile(filePath, encryptionKey);
                return true;
            }

            try
            {
                var encryptedJson = File.ReadAllText(filePath);
                var decryptedJson = EncryptionHelper.DecryptString(encryptedJson, ConvertToUnsecureString(encryptionKey));
                return !string.IsNullOrEmpty(decryptedJson);
            }
            catch (Exception ex)
            {
                DialogHelper.ShowErrorMessage($"Error checking password: {ex.Message}");
                return false;
            }
        }

    }
}
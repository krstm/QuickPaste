namespace QuickPaste.Utilities
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public static class EncryptionHelper
    {
        private const int KeySize = 256 / 8;

        /// <summary>
        /// Encrypts the given text using AES encryption with the specified key.
        /// </summary>
        /// <param name="text">The text to encrypt.</param>
        /// <param name="keyString">The encryption key as a string.</param>
        /// <returns>The encrypted text, combined with the IV, as a Base64-encoded string.</returns>
        public static string EncryptString(string text, string keyString)
        {
            byte[] key = GetKeyBytes(keyString);
            using (AesManaged aes = new AesManaged())
            {
                ICryptoTransform encryptor = aes.CreateEncryptor(key, aes.IV);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(text);
                        }
                    }
                    string iv = Convert.ToBase64String(aes.IV);
                    string encryptedContent = Convert.ToBase64String(ms.ToArray());
                    return iv + ":" + encryptedContent;
                }
            }
        }

        /// <summary>
        /// Decrypts the given encrypted text (cipher text) using AES decryption with the specified key.
        /// </summary>
        /// <param name="cipherText">The encrypted text to decrypt, combined with the IV, as a Base64-encoded string.</param>
        /// <param name="keyString">The decryption key as a string.</param>
        /// <returns>The decrypted plain text.</returns>
        public static string DecryptString(string cipherText, string keyString)
        {
            try
            {
                string[] parts = cipherText.Split(':');
                byte[] iv = Convert.FromBase64String(parts[0]);
                byte[] cipherBytes = Convert.FromBase64String(parts[1]);
                byte[] key = GetKeyBytes(keyString);
                using (AesManaged aes = new AesManaged())
                {
                    ICryptoTransform decryptor = aes.CreateDecryptor(key, iv);
                    using (MemoryStream ms = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader reader = new StreamReader(cs))
                            {
                                return reader.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        /// <summary>
        /// Converts a key string into a byte array for use in encryption or decryption.
        /// </summary>
        /// <param name="keyString">The key string to convert.</param>
        /// <returns>A byte array representing the key.</returns>
        private static byte[] GetKeyBytes(string keyString)
        {
            byte[] keyBytes = new byte[KeySize];
            byte[] providedBytes = Encoding.UTF8.GetBytes(keyString);
            Array.Copy(providedBytes, keyBytes, Math.Min(keyBytes.Length, providedBytes.Length));
            return keyBytes;
        }
    }
}
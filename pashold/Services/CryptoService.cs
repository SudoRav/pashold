using System;
using System.Security.Cryptography;
using System.Text;

namespace pashold.Services
{
    public static class CryptoService
    {
        public static byte[] GetKey(string password)
        {
            if (string.IsNullOrEmpty(password))
                return null;

            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        public static string Encrypt(string text, string password)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            using var aes = Aes.Create();
            aes.Key = GetKey(password);
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();

            byte[] plain = Encoding.UTF8.GetBytes(text);
            byte[] encrypted = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            byte[] result = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string encryptedText, string password)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return "";

            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);

                using var aes = Aes.Create();
                aes.Key = GetKey(password);

                byte[] iv = new byte[16];
                byte[] cipher = new byte[data.Length - 16];

                Buffer.BlockCopy(data, 0, iv, 0, 16);
                Buffer.BlockCopy(data, 16, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();

                byte[] decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

                return Encoding.UTF8.GetString(decrypted);
            }
            catch (CryptographicException)
            {
                // Выбрасываем сообщение о неправильном ключе
                System.Windows.MessageBox.Show(
                    "Неверный ключ шифрования! Дешифровка невозможна.",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error
                );
                return null; // или ""
            }
        }
        public static string SafeDecrypt(string encryptedText, string password)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(encryptedText))
                return null;

            try
            {
                return Decrypt(encryptedText, password);
            }
            catch
            {
                return null;
            }
        }
    }
}
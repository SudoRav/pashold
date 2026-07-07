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
            TryDecrypt(encryptedText, password, out string decryptedText);
            return decryptedText;
        }

        public static bool TryDecrypt(string encryptedText, string password, out string decryptedText)
        {
            decryptedText = null;

            if (string.IsNullOrEmpty(encryptedText))
            {
                decryptedText = "";
                return true;
            }

            if (string.IsNullOrEmpty(password))
                return false;

            try
            {
                byte[] data = Convert.FromBase64String(encryptedText);
                if (data.Length < 17)
                    return false;

                using var aes = Aes.Create();
                aes.Key = GetKey(password);

                byte[] iv = new byte[16];
                byte[] cipher = new byte[data.Length - 16];

                Buffer.BlockCopy(data, 0, iv, 0, 16);
                Buffer.BlockCopy(data, 16, cipher, 0, cipher.Length);

                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();

                byte[] decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                decryptedText = Encoding.UTF8.GetString(decrypted);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }

        public static string SafeDecrypt(string encryptedText, string password)
        {
            return TryDecrypt(encryptedText, password, out string decryptedText)
                ? decryptedText
                : null;
        }
    }
}
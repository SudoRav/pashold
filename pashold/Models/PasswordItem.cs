using System.Text.Json.Serialization;
using pashold.Services;
using pashold.ViewModels;


namespace pashold.Models
{
    public class PasswordItem
    {
        public string EncryptedName { get; set; }
        public string EncryptedContent { get; set; }

        [JsonIgnore]
        public string Name
        {
            get => CryptoService.SafeDecrypt(EncryptedName, MainViewModel.EncryptionKey) ?? "[Неверный ключ]";
            set => EncryptedName = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        [JsonIgnore]
        public string Content
        {
            get => CryptoService.SafeDecrypt(EncryptedContent, MainViewModel.EncryptionKey) ?? "[Неверный ключ]";
            set => EncryptedContent = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        public PasswordItem(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public PasswordItem() { }
    }
}
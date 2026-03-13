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
            get => CryptoService.Decrypt(EncryptedName, MainViewModel.EncryptionKey);
            set => EncryptedName = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        [JsonIgnore]
        public string Content
        {
            get => CryptoService.Decrypt(EncryptedContent, MainViewModel.EncryptionKey);
            set => EncryptedContent = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }
    }
}
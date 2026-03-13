using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using pashold.Services;
using pashold.ViewModels;

namespace pashold.Models
{
    public class Block
    {
        public string EncryptedName { get; set; }
        public string EncryptedDescription { get; set; }

        public ObservableCollection<PasswordItem> PasswordItems { get; set; } = new ObservableCollection<PasswordItem>();

        [JsonIgnore]
        public string Name
        {
            get => CryptoService.Decrypt(EncryptedName, MainViewModel.EncryptionKey);
            set => EncryptedName = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        [JsonIgnore]
        public string Description
        {
            get => CryptoService.Decrypt(EncryptedDescription, MainViewModel.EncryptionKey);
            set => EncryptedDescription = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }
    }
}
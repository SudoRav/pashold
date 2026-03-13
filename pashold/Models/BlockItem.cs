using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using System.Windows;
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
            get => CryptoService.SafeDecrypt(EncryptedName, MainViewModel.EncryptionKey) ?? "[Неверный ключ]";
            set => EncryptedName = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        [JsonIgnore]
        public string Description
        {
            get => CryptoService.SafeDecrypt(EncryptedDescription, MainViewModel.EncryptionKey) ?? "[Неверный ключ]";
            set => EncryptedDescription = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);
        }

        public Block(string name, string description)
        {
            if (string.IsNullOrEmpty(name)) name = "Название Блока";
            if (string.IsNullOrEmpty(description)) description = "Описание Блока";

            Name = name;
            Description = description;
        }

        public Block() { }
    }
}
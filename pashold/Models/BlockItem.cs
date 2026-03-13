using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using pashold.Services;
using pashold.ViewModels;

namespace pashold.Models
{
    public class Block : INotifyPropertyChanged
    {
        public string EncryptedName { get; set; }
        public string EncryptedDescription { get; set; }

        public ObservableCollection<PasswordItem> PasswordItems { get; set; } = new();

        private string _name;
        private string _description;

        [JsonIgnore]
        public string Name
        {
            get
            {
                if (_name == null && EncryptedName != null)
                    _name = CryptoService.SafeDecrypt(EncryptedName, MainViewModel.EncryptionKey);
                return _name;
            }
            set
            {
                _name = value;

                if (!string.IsNullOrEmpty(MainViewModel.EncryptionKey))
                    EncryptedName = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);

                OnPropertyChanged();
                MainViewModel.SaveCurrentJsonStatic(); // сразу сохраняем файл
            }
        }

        [JsonIgnore]
        public string Description
        {
            get
            {
                if (_description == null && EncryptedDescription != null)
                    _description = CryptoService.SafeDecrypt(EncryptedDescription, MainViewModel.EncryptionKey);
                return _description;
            }
            set
            {
                _description = value;

                if (!string.IsNullOrEmpty(MainViewModel.EncryptionKey))
                    EncryptedDescription = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);

                OnPropertyChanged();
                MainViewModel.SaveCurrentJsonStatic(); // сразу сохраняем файл
            }
        }

        public Block(string name, string description)
        {
            if (string.IsNullOrEmpty(name)) name = "Название Блока";
            if (string.IsNullOrEmpty(description)) description = "Описание Блока";

            Name = name;
            Description = description;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
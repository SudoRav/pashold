using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using pashold.Services;
using pashold.ViewModels;

namespace pashold.Models
{
    public class PasswordItem : INotifyPropertyChanged
    {
        public string EncryptedName { get; set; }
        public string EncryptedContent { get; set; }

        private string _name;
        private string _content;

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
        public string Content
        {
            get
            {
                if (_content == null && EncryptedContent != null)
                    _content = CryptoService.SafeDecrypt(EncryptedContent, MainViewModel.EncryptionKey);
                return _content;
            }
            set
            {
                _content = value;
                if (!string.IsNullOrEmpty(MainViewModel.EncryptionKey))
                    EncryptedContent = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);

                OnPropertyChanged();
                MainViewModel.SaveCurrentJsonStatic(); // сразу сохраняем файл
            }
        }

        public PasswordItem() { }

        public PasswordItem(string name, string content)
        {
            _name = name;
            _content = content;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
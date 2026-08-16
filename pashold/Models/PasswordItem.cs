using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using pashold.Services;
using pashold.ViewModels;

namespace pashold.Models
{
    public class PasswordItem : INotifyPropertyChanged
    {
        private string _encryptedName;
        public string EncryptedName
        {
            get => _encryptedName;
            set
            {
                _encryptedName = value;
                _name = null;
            }
        }

        private string _encryptedContent; 
        public string EncryptedContent
        {
            get => _encryptedContent;
            set
            {
                _encryptedContent = value;
                _content = null;
            }
        }

        private string _name;
        private string _content;

        // новое свойство: видимость пароля
        private bool _isContentVisible = false;
        [JsonIgnore]
        public bool IsContentVisible
        {
            get => _isContentVisible;
            set
            {
                _isContentVisible = value;
                OnPropertyChanged(nameof(IsContentVisible));
                OnPropertyChanged(nameof(Content)); // обновляем отображение
            }
        }

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
                MainViewModel.SaveCurrentJsonStatic();
            }
        }

        [JsonIgnore]
        public string Content
        {
            get
            {
                // если пароль скрыт — возвращаем маску
                if (!IsContentVisible)
                    return "****************************************";

                // расшифровываем только когда нужно
                if (_content == null && EncryptedContent != null)
                    _content = CryptoService.SafeDecrypt(EncryptedContent, MainViewModel.EncryptionKey);

                return _content;
            }
            set
            {
                // ВАЖНО: игнорируем установку маски
                if (value == "****************************************")
                    return;

                _content = value;

                if (!string.IsNullOrEmpty(MainViewModel.EncryptionKey))
                    EncryptedContent = CryptoService.Encrypt(value, MainViewModel.EncryptionKey);

                OnPropertyChanged();
                MainViewModel.SaveCurrentJsonStatic();
            }
        }

        public string GetDecryptedContent()
        {
            if (EncryptedContent == null)
                return "";

            return CryptoService.SafeDecrypt(EncryptedContent, MainViewModel.EncryptionKey);
        }

        public PasswordItem() { }

        public PasswordItem(string name, string content)
        {
            Name = name;
            Content = content;
            _isContentVisible = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
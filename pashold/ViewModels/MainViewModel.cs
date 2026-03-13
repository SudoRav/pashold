using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using pashold.Models;
using pashold.Services;
using pashold.Views;

namespace pashold.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public static string EncryptionKey { get; set; } = "default"; // для теста, можно заменить на ключ пользователя

        public ObservableCollection<ProgramFile> JsonFiles { get; set; } = new ObservableCollection<ProgramFile>();

        private ProgramFile _selectedJsonFile;
        public ProgramFile SelectedJsonFile
        {
            get => _selectedJsonFile;
            set
            {
                if (_selectedJsonFile == value) return;

                // отписка старой коллекции
                if (_selectedJsonFile != null && _selectedJsonFile.Blocks != null)
                {
                    foreach (var block in _selectedJsonFile.Blocks)
                        block.PasswordItems.CollectionChanged -= PasswordItems_CollectionChanged;
                    _selectedJsonFile.Blocks.CollectionChanged -= Blocks_CollectionChanged;
                }

                _selectedJsonFile = value;
                OnPropertyChanged(nameof(SelectedJsonFile));

                if (_selectedJsonFile != null)
                {
                    // Запрос ключа
                    var keyWindow = new AskKeyWindow { Owner = Application.Current.MainWindow };
                    if (keyWindow.ShowDialog() == true)
                    {
                        EncryptionKey = keyWindow.Key;

                        // Проверим, можем ли расшифровать хотя бы один блок
                        bool validKey = true;
                        foreach (var block in _selectedJsonFile.Blocks)
                        {
                            if (CryptoService.SafeDecrypt(block.EncryptedName, EncryptionKey) == null)
                            {
                                validKey = false;
                                break;
                            }
                        }

                        if (!validKey)
                        {
                            SelectedJsonFile = null;
                            return;
                        }
                    }
                    else
                    {
                        SelectedJsonFile = null;
                        return;
                    }

                    // теперь Blocks будут дешифроваться корректно
                    if (_selectedJsonFile.Blocks == null)
                        _selectedJsonFile.Blocks = new ObservableCollection<Block>();

                    Blocks = _selectedJsonFile.Blocks;

                    foreach (var block in Blocks)
                        block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;

                    Blocks.CollectionChanged += Blocks_CollectionChanged;
                }

                OnPropertyChanged(nameof(Blocks));
            }
        }

        public string JsonFolderPath { get; set; }
        public ObservableCollection<Block> Blocks { get; set; } = new ObservableCollection<Block>();

        // Команды
        public ICommand BrowseFolderCommand { get; }
        public ICommand CreateProgramFileCommand { get; }
        public ICommand AddBlockCommand { get; }
        public ICommand DeleteBlockCommand { get; }
        public ICommand AddPasswordCommand { get; }
        public ICommand DeletePasswordCommand { get; }
        public ICommand CopyPasswordCommand { get; }

        public MainViewModel()
        {
            BrowseFolderCommand = new RelayCommand<object>(_ => BrowseFolder());
            CreateProgramFileCommand = new RelayCommand<object>(_ => CreateProgramFile());
            AddBlockCommand = new RelayCommand<object>(_ => AddBlock());
            DeleteBlockCommand = new RelayCommand<Block>(DeleteBlock);
            AddPasswordCommand = new RelayCommand<Block>(AddPassword);
            DeletePasswordCommand = new RelayCommand<PasswordItem>(DeletePassword);
            CopyPasswordCommand = new RelayCommand<PasswordItem>(CopyPassword);

            // автозагрузка последнего пути
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastJsonFolder) && Directory.Exists(Properties.Settings.Default.LastJsonFolder))
            {
                JsonFolderPath = Properties.Settings.Default.LastJsonFolder;
                RefreshJsonFiles();
            }
        }

        #region JSON Файлы
        private void BrowseFolder()
        {
            var dlg = new CommonOpenFileDialog();
            dlg.IsFolderPicker = true;

            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                JsonFolderPath = dlg.FileName;
                Properties.Settings.Default.LastJsonFolder = JsonFolderPath;
                Properties.Settings.Default.Save();

                RefreshJsonFiles();
            }
        }

        private void RefreshJsonFiles()
        {
            JsonFiles.Clear();
            if (!Directory.Exists(JsonFolderPath)) return;

            foreach (var file in Directory.GetFiles(JsonFolderPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var data = JsonSerializer.Deserialize<ProgramFile>(json);
                    if (data != null && !string.IsNullOrEmpty(data.OriginalName))
                    {
                        data.FilePath = file;
                        if (data.Blocks == null)
                            data.Blocks = new ObservableCollection<Block>();
                        JsonFiles.Add(data);
                    }
                }
                catch
                {
                    // игнорируем некорректные файлы
                }
            }
        }

        private void CreateProgramFile()
        {
            if (string.IsNullOrEmpty(JsonFolderPath))
            {
                MessageBox.Show("Сначала выберите папку для JSON.", "Ошибка");
                return;
            }

            var dlg = new AddProgramFileWindow { Owner = Application.Current.MainWindow };
            if (dlg.ShowDialog() == true)
            {
                var pf = dlg.ProgramFile;

                pf.FilePath = Path.Combine(JsonFolderPath, pf.OriginalName + ".json");
                pf.Blocks = new ObservableCollection<Block>();

                EncryptionKey = dlg.Key; // ключ для шифрования

                string json = JsonSerializer.Serialize(pf, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(pf.FilePath, json);

                RefreshJsonFiles();
                //SelectedJsonFile = pf;
            }
        }
        #endregion

        #region Блоки и пароли
        private void AddBlock()
        {
            var block = new Block("", "");
            Blocks.Add(block);
            block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;
            SaveCurrentJson();
        }

        private void DeleteBlock(Block block)
        {
            if (block != null)
            {
                Blocks.Remove(block);
                SaveCurrentJson();
            }
        }

        private void AddPassword(Block block)
        {
            if (block == null) return;

            var window = new AddPasswordWindow { Owner = Application.Current.MainWindow };
            if (window.ShowDialog() == true && window.Password != null)
            {
                var password = new PasswordItem(window.Password.Name, window.Password.Content);
                block.PasswordItems.Add(password);
                SaveCurrentJson();
            }
        }

        private void DeletePassword(PasswordItem password)
        {
            if (password == null) return;
            foreach (var block in Blocks)
            {
                if (block.PasswordItems.Contains(password))
                {
                    block.PasswordItems.Remove(password);
                    SaveCurrentJson();
                    break;
                }
            }
        }

        private void CopyPassword(PasswordItem password)
        {
            if (password != null)
            {
                Clipboard.SetText(password.Content); // автоматически расшифровывает
                MessageBox.Show($"Пароль '{password.Name}' скопирован в буфер обмена!", "Скопировано");
            }
        }

        private void Blocks_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (Block block in e.NewItems)
                    block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;

            if (e.OldItems != null)
                foreach (Block block in e.OldItems)
                    block.PasswordItems.CollectionChanged -= PasswordItems_CollectionChanged;

            SaveCurrentJson();
        }

        private void PasswordItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SaveCurrentJson();
        }

        private void SaveCurrentJson()
        {
            if (SelectedJsonFile == null) return;

            try
            {
                SelectedJsonFile.Blocks = Blocks;
                string json = JsonSerializer.Serialize(SelectedJsonFile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SelectedJsonFile.FilePath, json);
            }
            catch
            {
                MessageBox.Show("Ошибка при автоматическом сохранении JSON файла.", "Ошибка");
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        #endregion
    }
}
using Microsoft.WindowsAPICodePack.Dialogs;
using pashold.Models;
using pashold.Services;
using pashold.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace pashold.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Статическая ссылка для автосохранения из моделей
        private static MainViewModel _instance;
        public static string EncryptionKey { get; set; }

        private readonly Dictionary<string, string> _fileKeys = new Dictionary<string, string>();
        public ObservableCollection<ProgramFile> JsonFiles { get; set; } = new ObservableCollection<ProgramFile>();
        public bool IsFileSelected => SelectedJsonFile != null;

        private ProgramFile _selectedJsonFile;
        public ProgramFile SelectedJsonFile
        {
            get => _selectedJsonFile;
            set
            {
                if (_selectedJsonFile == value)
                    return;

                // Отписка от старых событий
                if (_selectedJsonFile != null && _selectedJsonFile.Blocks != null)
                {
                    foreach (var block in _selectedJsonFile.Blocks)
                        block.PasswordItems.CollectionChanged -= PasswordItems_CollectionChanged;

                    _selectedJsonFile.Blocks.CollectionChanged -= Blocks_CollectionChanged;
                }

                _selectedJsonFile = value;
                OnPropertyChanged(nameof(SelectedJsonFile));

                if (_selectedJsonFile == null)
                    return;

                string filePath = _selectedJsonFile.FilePath;

                // Получение ключа
                if (_fileKeys.ContainsKey(filePath))
                {
                    EncryptionKey = _fileKeys[filePath];
                }
                else
                {
                    var keyWindow = new AskKeyWindow
                    {
                        Owner = Application.Current.MainWindow,
                        KeyValidator = enteredKey => IsValidKey(_selectedJsonFile, enteredKey)
                    };
                    if (keyWindow.ShowDialog() == true)
                    {
                        EncryptionKey = keyWindow.Key;
                        _fileKeys[filePath] = EncryptionKey;

                        OnPropertyChanged(nameof(IsFileSelected));
                    }
                    else
                    {
                        _selectedJsonFile = null;
                        OnPropertyChanged(nameof(SelectedJsonFile));
                        return;
                    }
                }

                if (_selectedJsonFile.Blocks == null)
                    _selectedJsonFile.Blocks = new ObservableCollection<Block>();

                Blocks = _selectedJsonFile.Blocks;

                foreach (var block in Blocks)
                {
                    block.PropertyChanged += Block_PropertyChanged;
                    block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;

                    foreach (var pass in block.PasswordItems)
                        pass.PropertyChanged += Password_PropertyChanged;
                }

                Blocks.CollectionChanged += Blocks_CollectionChanged;

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
        public ICommand MultiCopyPasswordCommand { get; }
        public ICommand CopyPasswordCommand { get; }
        public ICommand ShowPasswordQRCodeCommand { get; }
        public ICommand MovePasswordUp { get; }
        public ICommand MovePasswordDown { get; }
        public ICommand MoveBlockUp { get; }
        public ICommand MoveBlockDown { get; }
        public ICommand ToggleBlockExpansionCommand { get; }

        public ICommand ShowPasswordQRCodeCommand { get; }

        // Конструктор
        public MainViewModel()
        {
            _instance = this;

            BrowseFolderCommand = new RelayCommand<object>(_ => BrowseFolder());
            CreateProgramFileCommand = new RelayCommand<object>(_ => CreateProgramFile());
            AddBlockCommand = new RelayCommand<object>(_ => AddBlock());
            DeleteBlockCommand = new RelayCommand<Block>(DeleteBlock);
            AddPasswordCommand = new RelayCommand<Block>(AddPassword);
            DeletePasswordCommand = new RelayCommand<PasswordItem>(DeletePassword);
            MultiCopyPasswordCommand = new RelayCommand<Block>(MultiCopyPassword);
            CopyPasswordCommand = new RelayCommand<PasswordItem>(CopyPassword);
            ShowPasswordQRCodeCommand = new RelayCommand<PasswordItem>(ShowPasswordQRCode);
            MovePasswordUp = new RelayCommand<PasswordItem>(MovePasswordItemUp);
            MovePasswordDown = new RelayCommand<PasswordItem>(MovePasswordItemDown);
            MoveBlockUp = new RelayCommand<Block>(MoveBlockItemUp);
            MoveBlockDown = new RelayCommand<Block>(MoveBlockItemDown);
            ToggleBlockExpansionCommand = new RelayCommand<Block>(ToggleBlockExpansion);
            ShowPasswordQRCodeCommand = new RelayCommand<Block>(ShowPasswordQRCode);

            // Автозагрузка последнего пути
            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastJsonFolder) && Directory.Exists(Properties.Settings.Default.LastJsonFolder))
            {
                JsonFolderPath = Properties.Settings.Default.LastJsonFolder;
                RefreshJsonFiles();
            }

            foreach (var block in Blocks)
            {
                block.PropertyChanged += Block_PropertyChanged;
                block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;

                foreach (var pass in block.PasswordItems)
                    pass.PropertyChanged += Password_PropertyChanged;
            }
        }

        public static void SaveCurrentJsonStatic() => _instance?.SaveCurrentJson();

        private bool IsValidKey(ProgramFile programFile, string key)
        {
            if (programFile?.Blocks == null)
                return true;

            foreach (var block in programFile.Blocks)
            {
                if (!CanDecrypt(block.EncryptedName, key) || !CanDecrypt(block.EncryptedDescription, key))
                    return false;

                if (block.PasswordItems == null)
                    continue;

                foreach (var password in block.PasswordItems)
                {
                    if (!CanDecrypt(password.EncryptedName, key) || !CanDecrypt(password.EncryptedContent, key))
                        return false;
                }
            }

            return true;
        }

        private bool CanDecrypt(string encryptedText, string key)
        {
            return string.IsNullOrEmpty(encryptedText) || CryptoService.TryDecrypt(encryptedText, key, out _);
        }

        private void Block_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {   
            if (e.PropertyName == "Name" || e.PropertyName == "Description")
                SaveCurrentJson();
        }

        private void Password_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name" || e.PropertyName == "Content")
                SaveCurrentJson();
        }

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
                EncryptionKey = null;
            }
        }

        private void AddBlock()
        {
            var block = new Block("", "");

            block.PropertyChanged += Block_PropertyChanged;
            block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;

            Blocks.Add(block);
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

            if (window.ShowDialog() == true)
            {
                //var password = new PasswordItem(window.Password.Name, window.Password.Content);
                var password = window.Password;
                password.PropertyChanged += Password_PropertyChanged;

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

        private void MoveBlockItemUp(Block block)
        {
            MoveBlockItem(block, -1);
        }

        private void MoveBlockItemDown(Block block)
        {
            MoveBlockItem(block, 1);
        }

        private void MoveBlockItem(Block block, int direction)
        {
            if (block == null) return;

            int currentIndex = Blocks.IndexOf(block);
            if (currentIndex < 0)
                return;

            int newIndex = currentIndex + direction;
            if (newIndex < 0 || newIndex >= Blocks.Count)
                return;

            Blocks.Move(currentIndex, newIndex);
            SaveCurrentJson();
        }

        private void ToggleBlockExpansion(Block block)
        {
            block?.ToggleExpansion();
        }

        private void ShowPasswordQRCode(Block block)
        {

        }

        private void MovePasswordItemUp(PasswordItem password)
        {
            MovePasswordItem(password, -1);
        }

        private void MovePasswordItemDown(PasswordItem password)
        {
            MovePasswordItem(password, 1);
        }

        private void MovePasswordItem(PasswordItem password, int direction)
        {
            if (password == null) return;

            foreach (var block in Blocks)
            {
                int currentIndex = block.PasswordItems.IndexOf(password);
                if (currentIndex < 0)
                    continue;

                int newIndex = currentIndex + direction;
                if (newIndex < 0 || newIndex >= block.PasswordItems.Count)
                    return;

                block.PasswordItems.Move(currentIndex, newIndex);
                SaveCurrentJson();
                return;
            }
        }

        private void MultiCopyPassword(Block block)
        {
            if (block == null)
                return;

            var passwords = block.PasswordItems?
                .Reverse()
                .Select(password => password.GetDecryptedContent())
                .Where(text => !string.IsNullOrEmpty(text))
                .ToList() ?? new List<string>();

            TryCopyTextToClipboard("--------------------");
            System.Threading.Thread.Sleep(230);

            foreach (string text in passwords)
            {
                if (!TryCopyTextToClipboard(text))
                {
                    MessageBox.Show("Не удалось скопировать все пароли в буфер обмена.", "Ошибка");
                    return;
                }

                System.Threading.Thread.Sleep(230);
            }
        }

        private void CopyPassword(PasswordItem password)
        {
            if (password == null)
                return;

            string text = password.GetDecryptedContent();
            if (string.IsNullOrEmpty(text))
                return;

            if (!TryCopyTextToClipboard(text))
                MessageBox.Show("Не удалось скопировать пароль в буфер обмена.", "Ошибка");
        }


        private void ShowPasswordQRCode(PasswordItem password)
        {
            if (password == null || string.IsNullOrEmpty(password.EncryptedContent))
                return;

            try
            {
                var window = new QRCodeWindow(password.EncryptedContent)
                {
                    Owner = Application.Current.MainWindow
                };
                window.ShowDialog();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
        }

        private bool TryCopyTextToClipboard(string text)
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true);
                    return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            }

            return false;
        }

        private void Blocks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (Block block in e.NewItems)
                {
                    block.PropertyChanged += Block_PropertyChanged;
                    block.PasswordItems.CollectionChanged += PasswordItems_CollectionChanged;
                }

            if (e.OldItems != null)
                foreach (Block block in e.OldItems)
                    block.PasswordItems.CollectionChanged -= PasswordItems_CollectionChanged;

            SaveCurrentJson();
        }

        private void PasswordItems_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (PasswordItem item in e.NewItems)
                    item.PropertyChanged += Password_PropertyChanged;
            }

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

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
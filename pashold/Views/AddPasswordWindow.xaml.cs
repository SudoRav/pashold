using pashold.Models;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace pashold
{
    public partial class AddPasswordWindow : Window
    {
        public PasswordItem Password { get; private set; }  // сюда будет сохранён результат

        public AddPasswordWindow()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text) || string.IsNullOrWhiteSpace(ContentTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, заполните оба поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Password = new PasswordItem
            {
                Name = NameTextBox.Text,
                Content = ContentTextBox.Text
            };

            DialogResult = true;  // закрывает окно и возвращает true
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // просто закрываем окно
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%^&*()-_=+[]{}<>?/|";

            int lengthgen = int.Parse(LenghtTextBox.Text);

            StringBuilder password = new StringBuilder(lengthgen);
            byte[] randomBytes = new byte[4];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < lengthgen; i++)
                {
                    rng.GetBytes(randomBytes);
                    uint num = BitConverter.ToUInt32(randomBytes, 0);
                    password.Append(chars[(int)(num % (uint)chars.Length)]);
                }
            }

            ContentTextBox.Text = password.ToString();
        }
    }
}
using System.Windows;
using pashold.Models;

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
    }
}
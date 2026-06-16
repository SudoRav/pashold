using System.Windows;

namespace pashold.Views
{
    public partial class AskKeyWindow : Window
    {
        public string Key { get; private set; }

        public AskKeyWindow()
        {
            InitializeComponent();

            tbKey.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbKey.Password))
            {
                MessageBox.Show("Введите ключ.");
                return;
            }

            Key = tbKey.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
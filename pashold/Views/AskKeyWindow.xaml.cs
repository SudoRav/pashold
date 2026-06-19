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

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Ok_Click(sender, null);
                e.Handled = true;
            }

            if (e.Key == System.Windows.Input.Key.Escape)
                DialogResult = false;
        }
    }
}
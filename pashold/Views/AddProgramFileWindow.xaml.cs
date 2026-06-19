using System.Windows;
using pashold.Models;

namespace pashold
{
    public partial class AddProgramFileWindow : Window
    {
        public ProgramFile ProgramFile { get; private set; }
        public string Key { get; private set; }

        public AddProgramFileWindow()
        {
            InitializeComponent();

            tbName.Focus();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
                MessageBox.Show("Введите название файла.");
                return;
            }

            if (string.IsNullOrWhiteSpace(tbKey.Password))
            {
                MessageBox.Show("Введите ключ шифрования.");
                return;
            }

            ProgramFile = new ProgramFile
            {
                OriginalName = tbName.Text,
            };

            Key = tbKey.Password;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && tbName.IsFocused)
            {
                tbKey.Focus();
                return;
            }

            if (e.Key == System.Windows.Input.Key.Enter && tbKey.IsFocused)
            {
                Create_Click(sender, e);
                return;
            }

            if (e.Key == System.Windows.Input.Key.Escape)
                DialogResult = false;
        }
    }
}
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
    }
}
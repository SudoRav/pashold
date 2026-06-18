using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using pashold.Models;

namespace pashold
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            btn_AddBlock.IsEnabled = false;
        }

        private void PasswordBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = true; // раскрываем пароль
                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
                tb.SelectAll();
            }
        }

        private void PasswordBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = false; // скрываем пароль
                //tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }
        }
    }
}
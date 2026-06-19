using pashold.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace pashold
{
    public partial class MainWindow : Window
    {
        private bool isshowpas = false;
        public MainWindow()
        {
            InitializeComponent();

            btn_ShowPassword.Content = "Скрывать пароль";
        }

        private void PasswordBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = true;
                Clipboard.SetText(passwordItem.Content);

                if (!isshowpas)
                    passwordItem.IsContentVisible = false;

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

        private void btn_ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            isshowpas = !isshowpas;

            if(isshowpas)
                btn_ShowPassword.Content = "Показывать пароль";
            else
                btn_ShowPassword.Content = "Скрывать пароль";
        }
    }
}
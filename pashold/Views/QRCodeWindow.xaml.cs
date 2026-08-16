using pashold.Services;
using System;
using System.Windows;
using System.Windows.Input;

namespace pashold.Views
{
    public partial class QRCodeWindow : Window
    {
        public QRCodeWindow(string encryptedContent)
        {
            InitializeComponent();
            imgQrCode.Source = QrCodeService.CreateBitmap(encryptedContent);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
        }
    }
}

using pashold.Services;
using QRCoder.Core;
using QRCoder.Core.Generators;
using QRCoder.Core.Renderers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace pashold.Views
{
    public partial class QRCodeWindow : Window
    {
        public QRCodeWindow(string encryptedContent)
        {
            InitializeComponent();

            // Генерируем QR-код
            imgQrCode.Source = GenerateQrCode(encryptedContent);
        }

        private BitmapImage GenerateQrCode(string text)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);

            // Генерируем PNG в виде массива байт
            using var png = new PngByteQRCode(data);
            byte[] qrCodeBytes = png.GetGraphic(10); // 10 - размер пикселя

            // Конвертируем байты в BitmapImage для WPF
            using var stream = new MemoryStream(qrCodeBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // Делаем изображение потокобезопасным

            return bitmap;
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
            // Раскомментируйте, если нужно закрывать окно при потере фокуса
            // Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Activate();
            Focus();
        }
    }
}
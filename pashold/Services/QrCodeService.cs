using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace pashold.Services
{
    public static class QrCodeService
    {
        private static readonly int[] DataCodewords = { 0, 19, 34, 55, 80, 108 };
        private static readonly int[] ErrorCodewords = { 0, 7, 10, 15, 20, 26 };
        private static readonly int[] ByteCapacities = { 0, 17, 32, 53, 78, 106 };

        public static BitmapSource CreateBitmap(string text, int pixelsPerModule = 8, int quietZoneModules = 4)
        {
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("QR-код нельзя сформировать из пустой строки.", nameof(text));

            byte[] data = Encoding.UTF8.GetBytes(text);
            int version = 1;
            while (version < ByteCapacities.Length && data.Length > ByteCapacities[version])
                version++;

            if (version >= ByteCapacities.Length)
                throw new ArgumentException("Строка слишком длинная для отображения в QR-коде.", nameof(text));

            bool[,] modules = BuildModules(data, version);
            return Render(modules, pixelsPerModule, quietZoneModules);
        }

        private static bool[,] BuildModules(byte[] data, int version)
        {
            int size = 17 + version * 4;
            var modules = new bool[size, size];
            var reserved = new bool[size, size];

            DrawFinder(modules, reserved, 0, 0);
            DrawFinder(modules, reserved, size - 7, 0);
            DrawFinder(modules, reserved, 0, size - 7);
            DrawTiming(modules, reserved);
            DrawDarkModule(modules, reserved, version);
            DrawFormatBits(modules, reserved, 0);

            byte[] codewords = CreateCodewords(data, version);
            DrawData(modules, reserved, codewords);
            ApplyMask(modules, reserved);
            DrawFormatBits(modules, reserved, 0);

            return modules;
        }

        private static byte[] CreateCodewords(byte[] data, int version)
        {
            var bits = new List<int>();
            AppendBits(bits, 0b0100, 4);
            AppendBits(bits, data.Length, 8);
            foreach (byte value in data)
                AppendBits(bits, value, 8);

            int dataBits = DataCodewords[version] * 8;
            AppendBits(bits, 0, Math.Min(4, dataBits - bits.Count));
            while (bits.Count % 8 != 0)
                bits.Add(0);

            var bytes = new List<byte>();
            for (int i = 0; i < bits.Count; i += 8)
            {
                int value = 0;
                for (int j = 0; j < 8; j++)
                    value = (value << 1) | bits[i + j];
                bytes.Add((byte)value);
            }

            for (byte pad = 0xEC; bytes.Count < DataCodewords[version]; pad = pad == 0xEC ? (byte)0x11 : (byte)0xEC)
                bytes.Add(pad);

            byte[] ec = ReedSolomon(bytes.ToArray(), ErrorCodewords[version]);
            bytes.AddRange(ec);
            return bytes.ToArray();
        }

        private static void AppendBits(List<int> bits, int value, int count)
        {
            for (int i = count - 1; i >= 0; i--)
                bits.Add((value >> i) & 1);
        }

        private static void DrawFinder(bool[,] modules, bool[,] reserved, int left, int top)
        {
            int size = modules.GetLength(0);
            for (int y = -1; y <= 7; y++)
            for (int x = -1; x <= 7; x++)
            {
                int xx = left + x;
                int yy = top + y;
                if (xx < 0 || yy < 0 || xx >= size || yy >= size)
                    continue;

                reserved[xx, yy] = true;
                modules[xx, yy] = x >= 0 && x <= 6 && y >= 0 && y <= 6 &&
                    (x == 0 || x == 6 || y == 0 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4));
            }
        }

        private static void DrawTiming(bool[,] modules, bool[,] reserved)
        {
            int size = modules.GetLength(0);
            for (int i = 8; i < size - 8; i++)
            {
                bool value = i % 2 == 0;
                modules[i, 6] = value; reserved[i, 6] = true;
                modules[6, i] = value; reserved[6, i] = true;
            }
        }

        private static void DrawDarkModule(bool[,] modules, bool[,] reserved, int version)
        {
            int y = 4 * version + 9;
            modules[8, y] = true;
            reserved[8, y] = true;
        }

        private static void DrawFormatBits(bool[,] modules, bool[,] reserved, int mask)
        {
            int size = modules.GetLength(0);
            int bits = GetFormatBits(mask);
            for (int i = 0; i < 15; i++)
            {
                bool value = ((bits >> i) & 1) != 0;
                int x1, y1;
                if (i < 6) { x1 = 8; y1 = i; }
                else if (i < 8) { x1 = 8; y1 = i + 1; }
                else { x1 = 14 - i; y1 = 8; }
                modules[x1, y1] = value; reserved[x1, y1] = true;

                int x2, y2;
                if (i < 8) { x2 = size - 1 - i; y2 = 8; }
                else { x2 = 8; y2 = size - 15 + i; }
                modules[x2, y2] = value; reserved[x2, y2] = true;
            }
        }

        private static int GetFormatBits(int mask)
        {
            int data = (1 << 3) | mask;
            int rem = data << 10;
            const int generator = 0x537;
            for (int i = 14; i >= 10; i--)
                if (((rem >> i) & 1) != 0)
                    rem ^= generator << (i - 10);
            return ((data << 10) | rem) ^ 0x5412;
        }

        private static void DrawData(bool[,] modules, bool[,] reserved, byte[] codewords)
        {
            int size = modules.GetLength(0);
            int bitIndex = 0;
            int totalBits = codewords.Length * 8;
            int direction = -1;

            for (int right = size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right--;
                for (int vert = 0; vert < size; vert++)
                {
                    int y = direction == -1 ? size - 1 - vert : vert;
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        if (reserved[x, y]) continue;
                        bool bit = bitIndex < totalBits && ((codewords[bitIndex / 8] >> (7 - bitIndex % 8)) & 1) != 0;
                        modules[x, y] = bit;
                        bitIndex++;
                    }
                }
                direction = -direction;
            }
        }

        private static void ApplyMask(bool[,] modules, bool[,] reserved)
        {
            int size = modules.GetLength(0);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                if (!reserved[x, y] && (x + y) % 2 == 0)
                    modules[x, y] = !modules[x, y];
        }

        private static byte[] ReedSolomon(byte[] data, int degree)
        {
            byte[] generator = { 1 };
            for (int i = 0; i < degree; i++)
                generator = Multiply(generator, new[] { (byte)1, Exp(i) });

            byte[] result = new byte[degree];
            foreach (byte value in data)
            {
                byte factor = (byte)(value ^ result[0]);
                Array.Copy(result, 1, result, 0, degree - 1);
                result[degree - 1] = 0;
                for (int i = 0; i < degree; i++)
                    result[i] ^= Multiply(generator[i + 1], factor);
            }
            return result;
        }

        private static byte[] Multiply(byte[] left, byte[] right)
        {
            byte[] result = new byte[left.Length + right.Length - 1];
            for (int i = 0; i < left.Length; i++)
            for (int j = 0; j < right.Length; j++)
                result[i + j] ^= Multiply(left[i], right[j]);
            return result;
        }

        private static byte Multiply(byte x, byte y)
        {
            if (x == 0 || y == 0) return 0;
            return Exp(Log(x) + Log(y));
        }

        private static byte Exp(int value)
        {
            int result = 1;
            for (int i = 0; i < value % 255; i++)
            {
                result <<= 1;
                if (result >= 0x100)
                    result ^= 0x11D;
            }
            return (byte)result;
        }

        private static int Log(byte value)
        {
            int result = 1;
            for (int i = 0; i < 255; i++)
            {
                if (result == value) return i;
                result <<= 1;
                if (result >= 0x100)
                    result ^= 0x11D;
            }
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        private static BitmapSource Render(bool[,] modules, int scale, int quietZone)
        {
            int moduleCount = modules.GetLength(0);
            int size = (moduleCount + quietZone * 2) * scale;
            var visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(Brushes.White, null, new Rect(0, 0, size, size));
                for (int y = 0; y < moduleCount; y++)
                for (int x = 0; x < moduleCount; x++)
                    if (modules[x, y])
                        context.DrawRectangle(Brushes.Black, null, new Rect((x + quietZone) * scale, (y + quietZone) * scale, scale, scale));
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }
    }
}

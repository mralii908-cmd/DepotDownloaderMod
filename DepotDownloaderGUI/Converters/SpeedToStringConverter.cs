using System;
using System.Globalization;
using System.Windows.Data;

namespace DepotDownloaderGUI
{
    public class SpeedToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double speed)
            {
                return FormatSpeed(speed);
            }

            return "0 B/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static string FormatSpeed(double bytesPerSecond)
        {
            string[] sizes = { "B/s", "KB/s", "MB/s", "GB/s" };
            double speed = bytesPerSecond;
            int order = 0;
            while (speed >= 1024 && order < sizes.Length - 1)
            {
                order++;
                speed = speed / 1024;
            }
            return $"{speed:0.#} {sizes[order]}";
        }
    }
}

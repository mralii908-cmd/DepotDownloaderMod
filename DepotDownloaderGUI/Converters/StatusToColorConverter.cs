using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using DepotDownloaderGUI.Models;

namespace DepotDownloaderGUI
{
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileStatus fileStatus)
            {
                return fileStatus switch
                {
                    FileStatus.Pending => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    FileStatus.Downloading => new SolidColorBrush(Color.FromRgb(14, 99, 156)),
                    FileStatus.Completed => new SolidColorBrush(Color.FromRgb(78, 201, 176)),
                    FileStatus.Verified => new SolidColorBrush(Color.FromRgb(78, 201, 176)),
                    FileStatus.Failed => new SolidColorBrush(Color.FromRgb(244, 135, 113)),
                    FileStatus.HashMismatch => new SolidColorBrush(Color.FromRgb(255, 215, 0)),
                    FileStatus.Skipped => new SolidColorBrush(Color.FromRgb(128, 128, 128)),
                    _ => new SolidColorBrush(Color.FromRgb(204, 204, 204))
                };
            }

            return new SolidColorBrush(Color.FromRgb(204, 204, 204));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

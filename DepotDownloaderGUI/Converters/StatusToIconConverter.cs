using System;
using System.Globalization;
using System.Windows.Data;
using DepotDownloaderGUI.Models;

namespace DepotDownloaderGUI
{
    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus jobStatus)
            {
                return jobStatus switch
                {
                    JobStatus.Queued => "⏸",
                    JobStatus.Downloading => "▶",
                    JobStatus.Paused => "⏸",
                    JobStatus.Completed => "✓",
                    JobStatus.Failed => "✗",
                    JobStatus.Verifying => "🔍",
                    _ => "?"
                };
            }

            if (value is FileStatus fileStatus)
            {
                return fileStatus switch
                {
                    FileStatus.Pending => "⏸",
                    FileStatus.Downloading => "▶",
                    FileStatus.Completed => "✓",
                    FileStatus.Verified => "✓",
                    FileStatus.Failed => "✗",
                    FileStatus.HashMismatch => "⚠",
                    FileStatus.Skipped => "⊘",
                    _ => "?"
                };
            }

            if (value is bool isFolder && isFolder)
            {
                return "📁";
            }

            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

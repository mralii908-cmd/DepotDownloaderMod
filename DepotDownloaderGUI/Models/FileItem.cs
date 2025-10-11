using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DepotDownloaderGUI.Models
{
    public enum FileStatus
    {
        Pending,
        Downloading,
        Completed,
        Verified,
        Failed,
        HashMismatch,
        Skipped
    }

    public partial class FileItem : ObservableObject
    {
        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _filePath = string.Empty;

        [ObservableProperty]
        private long _fileSize;

        [ObservableProperty]
        private FileStatus _status;

        [ObservableProperty]
        private string? _sourceHash;

        [ObservableProperty]
        private string? _targetHash;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private bool _isFolder;

        public string FormattedSize => FormatBytes(FileSize);

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        partial void OnFileSizeChanged(long value)
        {
            OnPropertyChanged(nameof(FormattedSize));
        }
    }
}

using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DepotDownloaderGUI.Models
{
    public enum JobStatus
    {
        Queued,
        Downloading,
        Paused,
        Completed,
        Failed,
        Verifying
    }

    public partial class DownloadJob : ObservableObject
    {
        [ObservableProperty]
        private string _id = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _sourcePath = string.Empty;

        [ObservableProperty]
        private string _targetPath = string.Empty;

        [ObservableProperty]
        private JobStatus _status;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private long _totalSize;

        [ObservableProperty]
        private long _downloadedSize;

        [ObservableProperty]
        private double _speed; // bytes per second

        [ObservableProperty]
        private int _totalFiles;

        [ObservableProperty]
        private int _processedFiles;

        [ObservableProperty]
        private ObservableCollection<FileItem> _files = new();

        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private TimeSpan? _estimatedTimeRemaining;

        [ObservableProperty]
        private uint _appId;

        [ObservableProperty]
        private string _branch = "public";

        [ObservableProperty]
        private ObservableCollection<DepotManifestEntry> _depotManifests = new();

        [ObservableProperty]
        private bool _verifyFiles = true;

        [ObservableProperty]
        private bool _downloadAllPlatforms = false;

        [ObservableProperty]
        private bool _downloadAllLanguages = false;

        [ObservableProperty]
        private string _depotKeyFile = string.Empty;
    }
}

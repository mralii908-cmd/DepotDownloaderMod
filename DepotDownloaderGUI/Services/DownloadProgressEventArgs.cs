using System;

namespace DepotDownloaderGUI.Services
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public double Progress { get; set; }
        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Speed { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFiles { get; set; }
        public string CurrentFile { get; set; }
        public long CurrentFileSize { get; set; }
        public TimeSpan? EstimatedTimeRemaining { get; set; }
    }

    public class DownloadStatusEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class DownloadCompletedEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; }
    }

    public class FileVerificationEventArgs : EventArgs
    {
        public string JobId { get; set; }
        public string FileName { get; set; }
        public bool Success { get; set; }
        public string SourceHash { get; set; }
        public string TargetHash { get; set; }
    }

    public class LogMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}

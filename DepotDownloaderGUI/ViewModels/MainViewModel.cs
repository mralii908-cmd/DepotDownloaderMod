using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DepotDownloaderGUI.Models;
using DepotDownloaderGUI.Services;
using DepotDownloaderGUI.Views;

namespace DepotDownloaderGUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DepotDownloadService _downloadService;
        private bool _isLoggedIn = false;

        [ObservableProperty]
        private ObservableCollection<DownloadJob> _downloadJobs = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(StartDownloadCommand))]
        [NotifyCanExecuteChangedFor(nameof(PauseJobCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResumeJobCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopJobCommand))]
        private DownloadJob? _selectedJob;

        [ObservableProperty]
        private string _logText = string.Empty;

        [ObservableProperty]
        private bool _autoScroll = true;

        [ObservableProperty]
        private bool _unattendedMode = false;

        [ObservableProperty]
        private bool _verifyMode = true;

        public MainViewModel()
        {
            _downloadService = DepotDownloadService.Instance;

            // Subscribe to events
            _downloadService.ProgressChanged += OnProgressChanged;
            _downloadService.StatusChanged += OnStatusChanged;
            _downloadService.DownloadCompleted += OnDownloadCompleted;
            _downloadService.FileVerified += OnFileVerified;
            _downloadService.LogMessage += OnLogMessage;
        }

        partial void OnSelectedJobChanged(DownloadJob? oldValue, DownloadJob? newValue)
        {
            // Unsubscribe from old job
            if (oldValue != null)
            {
                oldValue.PropertyChanged -= SelectedJob_PropertyChanged;
            }

            // Subscribe to new job
            if (newValue != null)
            {
                newValue.PropertyChanged += SelectedJob_PropertyChanged;
            }
        }

        private void SelectedJob_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // When job status changes, update command states
            if (e.PropertyName == nameof(DownloadJob.Status))
            {
                StartDownloadCommand.NotifyCanExecuteChanged();
                PauseJobCommand.NotifyCanExecuteChanged();
                ResumeJobCommand.NotifyCanExecuteChanged();
                StopJobCommand.NotifyCanExecuteChanged();
            }
        }

        public async System.Threading.Tasks.Task InitializeAsync()
        {
            await LoginAnonymousAsync();
        }

        private async System.Threading.Tasks.Task LoginAnonymousAsync()
        {
            LogInfo("Logging in anonymously...");

            var success = await _downloadService.InitializeAsync(null, null);

            if (success)
            {
                _isLoggedIn = true;
                LogInfo("Anonymous login successful! Ready to download.");
            }
            else
            {
                LogInfo("Anonymous login failed.");
                MessageBox.Show("Failed to connect to Steam. Please check your internet connection and try again.",
                    "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async void AddJob()
        {
            if (!_isLoggedIn)
            {
                MessageBox.Show("Please login first.", "Not Logged In",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new NewDownloadDialog
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var jobId = Guid.NewGuid().ToString();

                    // Parse manifest files to get total size and file count
                    long totalSize = 0;
                    int totalFiles = 0;

                    if (dialog.DepotManifests != null)
                    {
                        foreach (var entry in dialog.DepotManifests)
                        {
                            if (!string.IsNullOrEmpty(entry.ManifestFile))
                            {
                                var (size, count) = DepotDownloadService.ParseManifestFile(entry.ManifestFile);
                                totalSize += size;
                                totalFiles += count;
                            }
                        }
                    }

                    var job = new DownloadJob
                    {
                        Id = jobId,
                        Name = $"App {dialog.AppId}",
                        AppId = dialog.AppId,
                        Branch = dialog.Branch ?? "public",
                        TargetPath = string.IsNullOrEmpty(dialog.TargetDirectory) ? "depots" : dialog.TargetDirectory,
                        SourcePath = $"Steam App {dialog.AppId}",
                        Status = JobStatus.Queued,
                        VerifyFiles = dialog.VerifyFiles,
                        DownloadAllPlatforms = dialog.DownloadAllPlatforms,
                        DownloadAllLanguages = dialog.DownloadAllLanguages,
                        DepotKeyFile = dialog.DepotKeyFile ?? string.Empty,
                        DepotManifests = new ObservableCollection<DepotManifestEntry>(dialog.DepotManifests ?? new ObservableCollection<DepotManifestEntry>()),
                        Progress = 0,
                        TotalSize = totalSize,
                        TotalFiles = totalFiles
                    };

                    if (totalSize > 0)
                    {
                        LogInfo($"Parsed manifests: {totalFiles} files, total size: {totalSize / (1024.0 * 1024.0):F2} MB");
                    }

                DownloadJobs.Add(job);
                SelectedJob = job;

                LogInfo($"Starting download for App {dialog.AppId}...");
                LogInfo($"Branch: {dialog.Branch ?? "public"}");

                if (dialog.DepotManifests != null && dialog.DepotManifests.Count > 0)
                {
                    LogInfo($"Configured {dialog.DepotManifests.Count} depot/manifest entries:");
                    foreach (var entry in dialog.DepotManifests)
                    {
                        if (entry != null)
                        {
                            var details = $"  Depot: {entry.DepotId?.ToString() ?? "all"}";
                            if (entry.ManifestId.HasValue)
                                details += $", Manifest ID: {entry.ManifestId}";
                            if (!string.IsNullOrEmpty(entry.ManifestFile))
                                details += $", Manifest File: {System.IO.Path.GetFileName(entry.ManifestFile)}";
                            LogInfo(details);
                        }
                    }
                }
                else
                {
                    LogInfo("No specific depots configured - will download all depots for this app");
                }

                // Start the download
                job.Status = JobStatus.Downloading;
                job.StartTime = DateTime.Now;

                await _downloadService.StartDownloadAsync(
                    jobId,
                    dialog.AppId,
                    dialog.DepotManifests ?? new ObservableCollection<DepotManifestEntry>(),
                    dialog.Branch ?? "public",
                    string.IsNullOrEmpty(dialog.TargetDirectory) ? "depots" : dialog.TargetDirectory,
                    dialog.VerifyFiles,
                    dialog.DownloadAllPlatforms,
                    dialog.DownloadAllLanguages,
                    string.IsNullOrEmpty(dialog.DepotKeyFile) ? null : dialog.DepotKeyFile
                );
                }
                catch (Exception ex)
                {
                    LogInfo($"Error starting download: {ex.Message}");
                    LogInfo($"Stack trace: {ex.StackTrace}");
                    MessageBox.Show($"Failed to start download:\n\n{ex.Message}\n\nDetails logged in the console.",
                        "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand(CanExecute = nameof(CanPauseJob))]
        private void PauseJob()
        {
            if (SelectedJob != null && SelectedJob.Status == JobStatus.Downloading)
            {
                _downloadService.PauseDownload(SelectedJob.Id);
                SelectedJob.Status = JobStatus.Paused;
                LogInfo($"Paused job: {SelectedJob.Name}");
            }
        }

        private bool CanPauseJob()
        {
            return SelectedJob != null && SelectedJob.Status == JobStatus.Downloading;
        }

        [RelayCommand(CanExecute = nameof(CanResumeJob))]
        private async void ResumeJob()
        {
            if (SelectedJob == null || SelectedJob.Status != JobStatus.Paused)
                return;

            if (!_isLoggedIn)
            {
                MessageBox.Show("Please login first.", "Not Logged In",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                LogInfo($"Resuming job: {SelectedJob.Name}");
                SelectedJob.Status = JobStatus.Downloading;

                await _downloadService.StartDownloadAsync(
                    SelectedJob.Id,
                    SelectedJob.AppId,
                    SelectedJob.DepotManifests,
                    SelectedJob.Branch,
                    SelectedJob.TargetPath,
                    SelectedJob.VerifyFiles,
                    SelectedJob.DownloadAllPlatforms,
                    SelectedJob.DownloadAllLanguages,
                    string.IsNullOrEmpty(SelectedJob.DepotKeyFile) ? null : SelectedJob.DepotKeyFile
                );
            }
            catch (Exception ex)
            {
                LogInfo($"Error resuming download: {ex.Message}");
                MessageBox.Show($"Failed to resume download:\n\n{ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanResumeJob()
        {
            return SelectedJob != null && SelectedJob.Status == JobStatus.Paused;
        }

        [RelayCommand(CanExecute = nameof(CanStopJob))]
        private void StopJob()
        {
            if (SelectedJob != null)
            {
                _downloadService.StopDownload(SelectedJob.Id);
                SelectedJob.Status = JobStatus.Failed;
                LogInfo($"Stopped job: {SelectedJob.Name}");
            }
        }

        private bool CanStopJob()
        {
            return SelectedJob != null && (SelectedJob.Status == JobStatus.Downloading || SelectedJob.Status == JobStatus.Paused);
        }

        [RelayCommand]
        private void SkipFile()
        {
            // TODO: Implement skip file functionality
            LogInfo("Skip file not yet implemented");
        }

        [RelayCommand(CanExecute = nameof(CanStartDownload))]
        private async void StartDownload()
        {
            if (SelectedJob == null || SelectedJob.Status != JobStatus.Queued)
                return;

            if (!_isLoggedIn)
            {
                MessageBox.Show("Please login first.", "Not Logged In",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var job = SelectedJob;
                LogInfo($"Starting download for {job.Name}...");
                LogInfo($"Branch: {job.Branch}");

                if (job.DepotManifests != null && job.DepotManifests.Count > 0)
                {
                    LogInfo($"Configured {job.DepotManifests.Count} depot/manifest entries:");
                    foreach (var entry in job.DepotManifests)
                    {
                        if (entry != null)
                        {
                            var details = $"  Depot: {entry.DepotId?.ToString() ?? "all"}";
                            if (entry.ManifestId.HasValue)
                                details += $", Manifest ID: {entry.ManifestId}";
                            if (!string.IsNullOrEmpty(entry.ManifestFile))
                                details += $", Manifest File: {System.IO.Path.GetFileName(entry.ManifestFile)}";
                            LogInfo(details);
                        }
                    }
                }

                // Start the download
                job.Status = JobStatus.Downloading;
                job.StartTime = DateTime.Now;

                await _downloadService.StartDownloadAsync(
                    job.Id,
                    job.AppId,
                    job.DepotManifests,
                    job.Branch,
                    job.TargetPath,
                    job.VerifyFiles,
                    job.DownloadAllPlatforms,
                    job.DownloadAllLanguages,
                    string.IsNullOrEmpty(job.DepotKeyFile) ? null : job.DepotKeyFile
                );
            }
            catch (Exception ex)
            {
                LogInfo($"Error starting download: {ex.Message}");
                MessageBox.Show($"Failed to start download:\n{ex.Message}",
                    "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanStartDownload()
        {
            return SelectedJob != null && SelectedJob.Status == JobStatus.Queued;
        }

        private void OnProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var job = DownloadJobs.FirstOrDefault(j => j.Id == e.JobId);
                if (job != null)
                {
                    job.Progress = e.Progress;
                    job.DownloadedSize = e.DownloadedBytes;
                    job.TotalSize = e.TotalBytes;
                    job.Speed = e.Speed;
                    job.ProcessedFiles = e.ProcessedFiles;
                    job.TotalFiles = e.TotalFiles;
                    job.EstimatedTimeRemaining = e.EstimatedTimeRemaining;

                    if (!string.IsNullOrEmpty(e.CurrentFile))
                    {
                        // Add or update file in the job's file list
                        var existingFile = job.Files.FirstOrDefault(f => f.FileName == e.CurrentFile);
                        if (existingFile == null)
                        {
                            job.Files.Add(new FileItem
                            {
                                FileName = e.CurrentFile,
                                FileSize = e.CurrentFileSize,
                                Status = FileStatus.Downloading
                            });
                        }
                        else
                        {
                            existingFile.FileSize = e.CurrentFileSize;
                            existingFile.Status = FileStatus.Downloading;
                        }
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnStatusChanged(object sender, DownloadStatusEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var job = DownloadJobs.FirstOrDefault(j => j.Id == e.JobId);
                if (job != null)
                {
                    LogInfo($"[{job.Name}] {e.Message}");
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnDownloadCompleted(object sender, DownloadCompletedEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var job = DownloadJobs.FirstOrDefault(j => j.Id == e.JobId);
                if (job != null)
                {
                    job.Status = e.Success ? JobStatus.Completed : JobStatus.Failed;

                    if (e.Success)
                    {
                        job.Progress = 100;
                        LogInfo($"Download completed: {job.Name}");
                        MessageBox.Show($"Download completed successfully!\n\nApp: {job.Name}\nTarget: {job.TargetPath}",
                            "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        LogInfo($"Download failed: {job.Name} - {e.Error}");
                        MessageBox.Show($"Download failed: {e.Error}",
                            "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        private void OnFileVerified(object sender, FileVerificationEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var job = DownloadJobs.FirstOrDefault(j => j.Id == e.JobId);
                if (job != null)
                {
                    var file = job.Files.FirstOrDefault(f => f.FileName == e.FileName);
                    if (file != null)
                    {
                        file.Status = e.Success ? FileStatus.Verified : FileStatus.HashMismatch;
                        file.SourceHash = e.SourceHash;
                        file.TargetHash = e.TargetHash;
                    }
                }
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnLogMessage(object sender, LogMessageEventArgs e)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LogInfo(e.Message);
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private void LogInfo(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            LogText += $"[{timestamp}] {message}\n";
        }

        [RelayCommand]
        private void SaveConfiguration()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json",
                    FileName = "download_configs.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var configs = new SavedConfigurations();

                    // Convert current jobs to configurations
                    foreach (var job in DownloadJobs)
                    {
                        var config = new DownloadConfiguration
                        {
                            AppId = job.AppId,
                            Branch = job.Branch,
                            TargetDirectory = job.TargetPath,
                            VerifyFiles = job.VerifyFiles,
                            DownloadAllPlatforms = job.DownloadAllPlatforms,
                            DownloadAllLanguages = job.DownloadAllLanguages,
                            DepotKeyFile = job.DepotKeyFile,
                            DepotManifests = new System.Collections.Generic.List<DepotManifestEntryData>()
                        };

                        foreach (var entry in job.DepotManifests)
                        {
                            config.DepotManifests.Add(new DepotManifestEntryData
                            {
                                DepotId = entry.DepotId,
                                ManifestId = entry.ManifestId,
                                ManifestFile = entry.ManifestFile
                            });
                        }

                        configs.Configurations.Add(config);
                    }

                    var json = System.Text.Json.JsonSerializer.Serialize(configs, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    });

                    System.IO.File.WriteAllText(dialog.FileName, json);
                    LogInfo($"Configuration saved to: {dialog.FileName}");
                    MessageBox.Show($"Configuration saved successfully to:\n{dialog.FileName}",
                        "Save Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogInfo($"Error saving configuration: {ex.Message}");
                MessageBox.Show($"Failed to save configuration:\n{ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void LoadConfiguration()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = ".json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = System.IO.File.ReadAllText(dialog.FileName);
                    var configs = System.Text.Json.JsonSerializer.Deserialize<SavedConfigurations>(json);

                    if (configs == null || configs.Configurations == null)
                    {
                        MessageBox.Show("Invalid configuration file.", "Load Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Clear existing jobs
                    DownloadJobs.Clear();

                    // Load configurations as new jobs
                    foreach (var config in configs.Configurations)
                    {
                        var job = new DownloadJob
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = $"App {config.AppId}",
                            AppId = config.AppId,
                            Branch = config.Branch,
                            TargetPath = config.TargetDirectory,
                            SourcePath = $"Steam App {config.AppId}",
                            Status = JobStatus.Queued,
                            VerifyFiles = config.VerifyFiles,
                            DownloadAllPlatforms = config.DownloadAllPlatforms,
                            DownloadAllLanguages = config.DownloadAllLanguages,
                            DepotKeyFile = config.DepotKeyFile,
                            DepotManifests = new ObservableCollection<DepotManifestEntry>(),
                            Progress = 0
                        };

                        foreach (var entry in config.DepotManifests)
                        {
                            job.DepotManifests.Add(new DepotManifestEntry
                            {
                                DepotId = entry.DepotId,
                                ManifestId = entry.ManifestId,
                                ManifestFile = entry.ManifestFile
                            });
                        }

                        DownloadJobs.Add(job);
                    }

                    LogInfo($"Loaded {configs.Configurations.Count} configuration(s) from: {dialog.FileName}");
                    MessageBox.Show($"Loaded {configs.Configurations.Count} configuration(s) successfully!",
                        "Load Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogInfo($"Error loading configuration: {ex.Message}");
                MessageBox.Show($"Failed to load configuration:\n{ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Shutdown()
        {
            _downloadService.Shutdown();
        }
    }
}

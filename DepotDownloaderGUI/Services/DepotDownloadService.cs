using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DepotDownloader;

namespace DepotDownloaderGUI.Services
{
    public class DepotDownloadService
    {
        private static DepotDownloadService _instance;
        public static DepotDownloadService Instance => _instance ??= new DepotDownloadService();

        // Events
        public event EventHandler<DownloadProgressEventArgs> ProgressChanged;
        public event EventHandler<DownloadStatusEventArgs> StatusChanged;
        public event EventHandler<DownloadCompletedEventArgs> DownloadCompleted;
        public event EventHandler<FileVerificationEventArgs> FileVerified;
        public event EventHandler<LogMessageEventArgs> LogMessage;

        private bool _isInitialized = false;
        private static bool _configInitialized = false;
        private readonly Dictionary<string, CancellationTokenSource> _activeTasks = new();
        private readonly Dictionary<string, Dictionary<string, long>> _fileMetadata = new(); // jobId -> (fileName -> fileSize)

        private DepotDownloadService()
        {
        }

        public async Task<bool> InitializeAsync(string username, string password)
        {
            if (_isInitialized)
                return true;

            LogInfo("Initializing Steam session...");

            try
            {
                // Initialize account settings store and config only once
                if (!_configInitialized)
                {
                    DepotDownloader.AccountSettingsStore.LoadFromFile("account.config");

                    // Initialize DepotConfigStore
                    var configPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "DepotDownloader");
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine(configPath, ".DepotDownloader"));
                    DepotDownloader.DepotConfigStore.LoadFromFile(System.IO.Path.Combine(configPath, ".DepotDownloader", "depot.config"));

                    // Initialize ContentDownloader config with defaults
                    ContentDownloader.Config.MaxDownloads = 8;
                    ContentDownloader.Config.CellID = 0;
                    ContentDownloader.Config.DownloadManifestOnly = false;
                    ContentDownloader.Config.RememberPassword = false;
                    ContentDownloader.Config.UseQrCode = false;
                    ContentDownloader.Config.SkipAppConfirmation = true;
                    ContentDownloader.Config.UsingFileList = false;
                    ContentDownloader.Config.FilesToDownload = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    ContentDownloader.Config.FilesToDownloadRegex = new System.Collections.Generic.List<System.Text.RegularExpressions.Regex>();

                    _configInitialized = true;
                }

                var result = await Task.Run(() => ContentDownloader.InitializeSteam3(username, password));

                if (result)
                {
                    _isInitialized = true;
                    LogInfo("Steam session initialized successfully");
                }
                else
                {
                    LogInfo("Failed to initialize Steam session");
                }

                return result;
            }
            catch (Exception ex)
            {
                LogInfo($"Error initializing Steam: {ex.Message}");
                return false;
            }
        }

        public async Task<string> StartDownloadAsync(
            string jobId,
            uint appId,
            System.Collections.ObjectModel.ObservableCollection<DepotDownloaderGUI.Models.DepotManifestEntry> depotManifests,
            string branch = "public",
            string targetDirectory = null,
            bool verifyFiles = true,
            bool downloadAllPlatforms = false,
            bool downloadAllLanguages = false,
            string depotKeyFile = null)
        {
            if (!_isInitialized)
            {
                LogInfo("Steam session not initialized. Please login first.");
                return null;
            }

            LogInfo($"Starting/resuming download {jobId}...");

            // If there's an existing task (from pause), clean it up first
            if (_activeTasks.TryGetValue(jobId, out var oldCts))
            {
                LogInfo($"Cleaning up previous download session for {jobId}");
                oldCts.Dispose();
                _activeTasks.Remove(jobId);
            }

            var cts = new CancellationTokenSource();
            _activeTasks[jobId] = cts;

            // Set the external cancellation token for ContentDownloader to use
            ContentDownloader.ExternalCancellationTokenSource = cts;

            // Subscribe to detailed logging from ContentDownloader
            ContentDownloader.LogMessage += OnContentDownloaderLog;

            LogInfo($"Download session initialized for {jobId}");

            // Extract file metadata from manifests for file size display
            var jobMetadata = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            if (depotManifests != null)
            {
                foreach (var entry in depotManifests)
                {
                    if (!string.IsNullOrEmpty(entry.ManifestFile))
                    {
                        var manifestMetadata = ExtractFileMetadata(entry.ManifestFile);
                        foreach (var kvp in manifestMetadata)
                        {
                            jobMetadata[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            _fileMetadata[jobId] = jobMetadata;

            // Speed tracking variables
            var lastUpdateTime = DateTime.Now;
            var lastUiUpdateTime = DateTime.Now;
            ulong lastDownloadedBytes = 0;
            double currentSpeed = 0;

            // Subscribe to progress events
            EventHandler<DepotDownloader.DownloadProgressEventArgs> progressHandler = (sender, e) =>
            {
                var now = DateTime.Now;
                var timeDiff = (now - lastUpdateTime).TotalSeconds;

                // Calculate speed if enough time has passed
                if (timeDiff > 0 && e.DownloadedBytes > lastDownloadedBytes)
                {
                    var bytesDiff = e.DownloadedBytes - lastDownloadedBytes;
                    currentSpeed = bytesDiff / timeDiff;
                    lastUpdateTime = now;
                    lastDownloadedBytes = e.DownloadedBytes;
                }

                // Throttle UI updates to every 100ms to prevent overwhelming the UI thread
                var uiTimeDiff = (now - lastUiUpdateTime).TotalMilliseconds;
                if (uiTimeDiff < 100)
                    return;

                lastUiUpdateTime = now;

                // Get file size from metadata if available
                long fileSize = 0;
                if (!string.IsNullOrEmpty(e.CurrentFile) && _fileMetadata.TryGetValue(jobId, out var metadata))
                {
                    metadata.TryGetValue(e.CurrentFile, out fileSize);
                }

                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    JobId = jobId,
                    Progress = e.Progress,
                    DownloadedBytes = (long)e.DownloadedBytes,
                    TotalBytes = (long)e.TotalBytes,
                    Speed = currentSpeed,
                    ProcessedFiles = e.ProcessedFiles,
                    TotalFiles = e.TotalFiles,
                    CurrentFile = e.CurrentFile,
                    CurrentFileSize = fileSize
                });
            };

            ContentDownloader.ProgressUpdated += progressHandler;

            try
            {
                // Configure the download
                ContentDownloader.Config.InstallDirectory = string.IsNullOrEmpty(targetDirectory) ? "depots" : targetDirectory;
                ContentDownloader.Config.VerifyAll = verifyFiles;
                ContentDownloader.Config.DownloadAllPlatforms = downloadAllPlatforms;
                ContentDownloader.Config.DownloadAllArchs = downloadAllPlatforms; // Use same as platforms
                ContentDownloader.Config.DownloadAllLanguages = downloadAllLanguages;

                // Load depot keys if provided (only on first run, not on resume)
                if (!string.IsNullOrEmpty(depotKeyFile) && System.IO.File.Exists(depotKeyFile))
                {
                    try
                    {
                        var keyLines = System.IO.File.ReadAllLines(depotKeyFile);
                        var newKeysAdded = 0;
                        foreach (var line in keyLines)
                        {
                            if (DepotDownloader.DepotKeyStore.AddKey(line))
                                newKeysAdded++;
                        }

                        if (newKeysAdded > 0)
                        {
                            LogInfo($"Loaded {newKeysAdded} new depot keys from: {System.IO.Path.GetFileName(depotKeyFile)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogInfo($"Warning: Failed to load depot keys: {ex.Message}");
                    }
                }

                LogInfo($"Starting download for App ID: {appId}");
                StatusChanged?.Invoke(this, new DownloadStatusEventArgs
                {
                    JobId = jobId,
                    Status = "Downloading",
                    Message = $"Preparing to download App {appId}"
                });

                var depotManifestIds = new List<(uint depotId, ulong manifestId)>();

                // Process depot/manifest entries - download each depot separately if manifest files are provided
                if (depotManifests != null && depotManifests.Count > 0)
                {
                    foreach (var entry in depotManifests)
                    {
                        if (entry.DepotId.HasValue)
                        {
                            ulong manifestId = entry.ManifestId ?? ContentDownloader.INVALID_MANIFEST_ID;

                            // If manifest file is provided, download this depot separately with the manifest file
                            if (!string.IsNullOrEmpty(entry.ManifestFile) && System.IO.File.Exists(entry.ManifestFile))
                            {
                                LogInfo($"Starting depot {entry.DepotId} download...");

                                // Set manifest file config for this depot
                                ContentDownloader.Config.UseManifestFile = true;
                                ContentDownloader.Config.ManifestFile = entry.ManifestFile;

                                var singleDepotList = new List<(uint depotId, ulong manifestId)>
                                {
                                    (entry.DepotId.Value, manifestId)
                                };

                                LogInfo($"Calling DownloadAppAsync for depot {entry.DepotId}...");
                                await ContentDownloader.DownloadAppAsync(
                                    appId,
                                    singleDepotList,
                                    branch ?? ContentDownloader.DEFAULT_BRANCH,
                                    null, // os
                                    null, // arch
                                    null, // language
                                    false,
                                    false
                                );

                                // Reset manifest file config
                                ContentDownloader.Config.UseManifestFile = false;
                                ContentDownloader.Config.ManifestFile = null;
                            }
                            else
                            {
                                // No manifest file, download using manifest ID
                                LogInfo($"Downloading depot {entry.DepotId} using manifest ID: {manifestId}");

                                var singleDepotList = new List<(uint depotId, ulong manifestId)>
                                {
                                    (entry.DepotId.Value, manifestId)
                                };

                                await ContentDownloader.DownloadAppAsync(
                                    appId,
                                    singleDepotList,
                                    branch ?? ContentDownloader.DEFAULT_BRANCH,
                                    null, // os
                                    null, // arch
                                    null, // language
                                    false,
                                    false
                                );
                            }
                        }
                    }
                }
                else
                {
                    // No specific depots configured, download all
                    await ContentDownloader.DownloadAppAsync(
                        appId,
                        new List<(uint depotId, ulong manifestId)>(),
                        branch ?? ContentDownloader.DEFAULT_BRANCH,
                        null, // os
                        null, // arch
                        null, // language
                        false,
                        false
                    );
                }

                if (!cts.Token.IsCancellationRequested)
                {
                    LogInfo($"Download completed for App ID: {appId}");
                    DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                    {
                        JobId = jobId,
                        Success = true
                    });
                }

                return jobId;
            }
            catch (TaskCanceledException)
            {
                // Download was cancelled/paused - this is normal
                LogInfo($"Download cancelled for job: {jobId}");
                return jobId;
            }
            catch (OperationCanceledException)
            {
                // Download was cancelled/paused - this is normal
                LogInfo($"Download cancelled for job: {jobId}");
                return jobId;
            }
            catch (Exception ex)
            {
                LogInfo($"Download failed: {ex.Message}");
                LogInfo($"Exception type: {ex.GetType().Name}");
                LogInfo($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    LogInfo($"Inner exception: {ex.InnerException.Message}");
                    LogInfo($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs
                {
                    JobId = jobId,
                    Success = false,
                    Error = ex.Message
                });
                return null;
            }
            finally
            {
                ContentDownloader.ProgressUpdated -= progressHandler;
                ContentDownloader.LogMessage -= OnContentDownloaderLog;
                ContentDownloader.ExternalCancellationTokenSource = null;
                _activeTasks.Remove(jobId);
                _fileMetadata.Remove(jobId);
            }
        }

        public void PauseDownload(string jobId)
        {
            if (_activeTasks.TryGetValue(jobId, out var cts))
            {
                LogInfo($"Pausing download {jobId}...");
                cts.Cancel();
                // Don't remove from _activeTasks - keep it for resume
                LogInfo($"Pause signal sent for {jobId}");
            }
        }

        public void StopDownload(string jobId)
        {
            if (_activeTasks.TryGetValue(jobId, out var cts))
            {
                LogInfo($"Stopping download {jobId}");
                cts.Cancel();
                _activeTasks.Remove(jobId);
                _fileMetadata.Remove(jobId);
            }
        }

        public void Shutdown()
        {
            if (_isInitialized)
            {
                LogInfo("Shutting down Steam session...");

                // Cancel all active downloads
                foreach (var cts in _activeTasks.Values)
                {
                    cts.Cancel();
                }
                _activeTasks.Clear();

                ContentDownloader.ShutdownSteam3();
                _isInitialized = false;

                LogInfo("Steam session shut down");
            }
        }

        public static (long totalSize, int fileCount) ParseManifestFile(string manifestFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(manifestFilePath) || !System.IO.File.Exists(manifestFilePath))
                    return (0, 0);

                var manifest = DepotDownloader.ProtoManifest.LoadFromFile(manifestFilePath, out _);
                if (manifest == null || manifest.Files == null)
                    return (0, 0);

                long totalSize = 0;
                int fileCount = 0;

                foreach (var file in manifest.Files)
                {
                    if (!file.Flags.HasFlag(SteamKit2.EDepotFileFlag.Directory))
                    {
                        totalSize += (long)file.TotalSize;
                        fileCount++;
                    }
                }

                return (totalSize, fileCount);
            }
            catch
            {
                return (0, 0);
            }
        }

        private Dictionary<string, long> ExtractFileMetadata(string manifestFilePath)
        {
            var metadata = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (string.IsNullOrEmpty(manifestFilePath))
                    return metadata;

                if (!System.IO.File.Exists(manifestFilePath))
                    return metadata;

                // Load using SteamKit2 DepotManifest (for decrypted .manifest files)
                var manifest = SteamKit2.DepotManifest.LoadFromFile(manifestFilePath);

                if (manifest == null || manifest.Files == null)
                    return metadata;

                foreach (var file in manifest.Files)
                {
                    if (!file.Flags.HasFlag(SteamKit2.EDepotFileFlag.Directory))
                    {
                        // Store original path as-is
                        metadata[file.FileName] = (long)file.TotalSize;
                    }
                }
            }
            catch
            {
                // Silently handle manifest parsing errors
            }

            return metadata;
        }

        // Helper methods for events
        public void ReportProgress(string jobId, double progress, long downloadedBytes, long totalBytes, double speed, int processedFiles, int totalFiles, string currentFile = null)
        {
            ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
            {
                JobId = jobId,
                Progress = progress,
                DownloadedBytes = downloadedBytes,
                TotalBytes = totalBytes,
                Speed = speed,
                ProcessedFiles = processedFiles,
                TotalFiles = totalFiles,
                CurrentFile = currentFile
            });
        }

        public void ReportFileVerification(string jobId, string fileName, bool success, string sourceHash, string targetHash)
        {
            FileVerified?.Invoke(this, new FileVerificationEventArgs
            {
                JobId = jobId,
                FileName = fileName,
                Success = success,
                SourceHash = sourceHash,
                TargetHash = targetHash
            });
        }

        private void LogInfo(string message)
        {
            LogMessage?.Invoke(this, new LogMessageEventArgs
            {
                Message = message
            });
        }

        private void OnContentDownloaderLog(object sender, string message)
        {
            LogInfo(message);
        }
    }
}

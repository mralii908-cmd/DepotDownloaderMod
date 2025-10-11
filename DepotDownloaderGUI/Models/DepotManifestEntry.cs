using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DepotDownloaderGUI.Models
{
    public class DepotManifestEntry : INotifyPropertyChanged
    {
        private uint? _depotId;
        private ulong? _manifestId;
        private string _manifestFile = string.Empty;

        public uint? DepotId
        {
            get => _depotId;
            set
            {
                _depotId = value;
                OnPropertyChanged();
            }
        }

        public ulong? ManifestId
        {
            get => _manifestId;
            set
            {
                _manifestId = value;
                OnPropertyChanged();
            }
        }

        public string ManifestFile
        {
            get => _manifestFile;
            set
            {
                _manifestFile = value;
                OnPropertyChanged();

                // Auto-parse depot ID and manifest ID from filename
                if (!string.IsNullOrEmpty(value))
                {
                    ParseManifestFilename(value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ParseManifestFilename(string filePath)
        {
            try
            {
                // Extract filename without extension
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                // Expected format: depotId_manifestId.manifest
                var parts = fileName.Split('_');
                if (parts.Length == 2)
                {
                    if (uint.TryParse(parts[0], out uint depotId))
                    {
                        DepotId = depotId;
                    }

                    if (ulong.TryParse(parts[1], out ulong manifestId))
                    {
                        ManifestId = manifestId;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }
    }
}

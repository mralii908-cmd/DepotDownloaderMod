using System.Collections.Generic;

namespace DepotDownloaderGUI.Models
{
    public class DownloadConfiguration
    {
        public uint AppId { get; set; }
        public string Branch { get; set; } = "public";
        public string TargetDirectory { get; set; } = string.Empty;
        public bool VerifyFiles { get; set; } = true;
        public bool DownloadAllPlatforms { get; set; } = false;
        public bool DownloadAllLanguages { get; set; } = false;
        public string DepotKeyFile { get; set; } = string.Empty;
        public List<DepotManifestEntryData> DepotManifests { get; set; } = new();
    }

    public class DepotManifestEntryData
    {
        public uint? DepotId { get; set; }
        public ulong? ManifestId { get; set; }
        public string ManifestFile { get; set; } = string.Empty;
    }

    public class SavedConfigurations
    {
        public List<DownloadConfiguration> Configurations { get; set; } = new();
    }
}

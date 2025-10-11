using System.Collections.ObjectModel;
using System.Windows;
using Ookii.Dialogs.Wpf;
using Microsoft.Win32;
using DepotDownloaderGUI.Models;

namespace DepotDownloaderGUI.Views
{
    public partial class NewDownloadDialog : Window
    {
        public uint AppId { get; private set; }
        public string Branch { get; private set; } = "public";
        public string TargetDirectory { get; private set; } = string.Empty;
        public string DepotKeyFile { get; private set; } = string.Empty;
        public ObservableCollection<DepotManifestEntry> DepotManifests { get; private set; }
        public bool VerifyFiles { get; private set; }
        public bool DownloadAllPlatforms { get; private set; }
        public bool DownloadAllLanguages { get; private set; }
        public bool Success { get; private set; }

        public NewDownloadDialog()
        {
            InitializeComponent();
            DepotManifests = new ObservableCollection<DepotManifestEntry>();
            DepotsDataGrid.ItemsSource = DepotManifests;
        }

        private void AddDepot_Click(object sender, RoutedEventArgs e)
        {
            DepotManifests.Add(new DepotManifestEntry());
        }

        private void RemoveDepot_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DepotManifestEntry entry)
            {
                DepotManifests.Remove(entry);
            }
        }

        private void BrowseManifestFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DepotManifestEntry entry)
            {
                var dialog = new OpenFileDialog
                {
                    Title = "Select Manifest File",
                    Filter = "Manifest Files (*.manifest)|*.manifest|All Files (*.*)|*.*",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(this) == true)
                {
                    entry.ManifestFile = dialog.FileName;
                }
            }
        }

        private void BrowseDepotKeyButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Depot Key File",
                Filter = "Key Files (*.key)|*.key|All Files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                DepotKeyFileTextBox.Text = dialog.FileName;
            }
        }

        private void BrowseTargetButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select target directory for download",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                TargetDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate App ID
            if (string.IsNullOrWhiteSpace(AppIdTextBox.Text) ||
                !uint.TryParse(AppIdTextBox.Text, out uint appId))
            {
                MessageBox.Show("Please enter a valid App ID.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AppId = appId;
            Branch = string.IsNullOrWhiteSpace(BranchTextBox?.Text) ? "public" : BranchTextBox.Text;
            TargetDirectory = TargetDirectoryTextBox?.Text ?? string.Empty;
            DepotKeyFile = DepotKeyFileTextBox?.Text ?? string.Empty;
            VerifyFiles = VerifyFilesCheckBox?.IsChecked ?? true;
            DownloadAllPlatforms = DownloadAllPlatformsCheckBox?.IsChecked ?? false;
            DownloadAllLanguages = DownloadAllLanguagesCheckBox?.IsChecked ?? false;

            Success = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Success = false;
            DialogResult = false;
            Close();
        }
    }
}

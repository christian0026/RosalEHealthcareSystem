using Microsoft.Win32;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class BackupRestoreTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly BackupService _backupService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _isBackupInProgress = false;
        private bool _isRestoreInProgress = false;

        public BackupRestoreTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _backupService = new BackupService(_db);
            _activityLogService = new ActivityLogService(_db);
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;

            try
            {
                // Load backup configuration
                TxtBackupLocation.Text = _settingsService.GetBackupLocation();
                SelectComboBoxItem(CmbRetentionCount, _settingsService.GetBackupRetentionCount().ToString());

                // Load statistics
                LoadBackupStats();

                // Load backup history
                LoadBackupHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadBackupStats()
        {
            try
            {
                var lastBackup = _backupService.GetLastSuccessfulBackup();
                if (lastBackup != null)
                {
                    TxtLastBackupInfo.Text = $"Last Backup: {lastBackup.CompletedAt:MMMM dd, yyyy 'at' hh:mm tt}";
                }
                else
                {
                    TxtLastBackupInfo.Text = "Last Backup: Never";
                }

                int totalBackups = _backupService.GetBackupCount();
                long totalSize = _backupService.GetTotalBackupSize();
                string sizeFormatted = FormatFileSize(totalSize);

                TxtBackupStats.Text = $"Total backups: {totalBackups} | Total size: {sizeFormatted}";
            }
            catch (Exception ex)
            {
                TxtLastBackupInfo.Text = "Last Backup: Unknown";
                TxtBackupStats.Text = $"Error loading stats: {ex.Message}";
            }
        }

        private void LoadBackupHistory()
        {
            try
            {
                var history = _backupService.GetBackupHistoryPaged(1, 20).ToList();

                if (history.Any())
                {
                    BackupHistoryList.ItemsSource = history;
                    BackupHistoryList.Visibility = Visibility.Visible;
                    EmptyHistoryPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    BackupHistoryList.ItemsSource = null;
                    BackupHistoryList.Visibility = Visibility.Collapsed;
                    EmptyHistoryPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backup history: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectComboBoxItem(ComboBox comboBox, string tagValue)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            if (comboBox.Items.Count > 0)
                comboBox.SelectedIndex = 0;
        }

        private string GetSelectedTag(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item)
            {
                return item.Tag?.ToString() ?? "";
            }
            return "";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }

        #endregion

        #region Configuration

        private void BtnBrowseLocation_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Backup Location",
                SelectedPath = TxtBackupLocation.Text,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TxtBackupLocation.Text = dialog.SelectedPath;
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                // Validate backup location
                string backupLocation = TxtBackupLocation.Text.Trim();
                if (string.IsNullOrEmpty(backupLocation))
                {
                    MessageBox.Show("Please specify a backup location.",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create directory if it doesn't exist
                if (!Directory.Exists(backupLocation))
                {
                    try
                    {
                        Directory.CreateDirectory(backupLocation);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not create backup directory: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Save settings
                _settingsService.SetBackupLocation(backupLocation, currentUser);
                _settingsService.SetBackupRetentionCount(int.Parse(GetSelectedTag(CmbRetentionCount)), currentUser);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated Backup Configuration",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );

                MessageBox.Show("Backup configuration saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Create Backup

        private async void BtnCreateBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_isBackupInProgress)
            {
                MessageBox.Show("A backup is already in progress.",
                    "Please Wait", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                "This will create a full database backup.\n\nDo you want to continue?",
                "Create Backup",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _isBackupInProgress = true;
            BtnCreateBackup.IsEnabled = false;
            BackupProgressPanel.Visibility = Visibility.Visible;

            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                var backupResult = await Task.Run(() =>
                {
                    return _backupService.CreateBackup("Manual", currentUser, (progress, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            BackupProgressBar.Value = progress;
                            TxtBackupPercent.Text = $"{progress}%";
                            TxtBackupProgress.Text = message;
                        });
                    });
                });

                if (backupResult.Success)
                {
                    // Log activity
                    _activityLogService.LogActivity(
                        activityType: "Backup",
                        description: $"Created manual backup: {Path.GetFileName(backupResult.FilePath)}",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    MessageBox.Show(
                        $"Backup created successfully!\n\nFile: {Path.GetFileName(backupResult.FilePath)}\nSize: {FormatFileSize(backupResult.FileSize)}",
                        "Backup Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh stats and history
                    LoadBackupStats();
                    LoadBackupHistory();
                }
                else
                {
                    MessageBox.Show($"Backup failed:\n\n{backupResult.Message}",
                        "Backup Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during backup: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isBackupInProgress = false;
                BtnCreateBackup.IsEnabled = true;
                BackupProgressPanel.Visibility = Visibility.Collapsed;
                BackupProgressBar.Value = 0;
            }
        }

        #endregion

        #region Restore Backup

        private async void BtnRestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (_isRestoreInProgress)
            {
                MessageBox.Show("A restore is already in progress.",
                    "Please Wait", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open file dialog
            var dialog = new OpenFileDialog
            {
                Title = "Select Backup File to Restore",
                Filter = "Backup Files (*.bak)|*.bak|All Files (*.*)|*.*",
                InitialDirectory = _settingsService.GetBackupLocation()
            };

            if (dialog.ShowDialog() != true) return;

            await PerformRestore(dialog.FileName);
        }

        private async void BtnRestoreFromHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BackupHistory backup)
            {
                if (!File.Exists(backup.FilePath))
                {
                    MessageBox.Show("Backup file not found. It may have been moved or deleted.",
                        "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await PerformRestore(backup.FilePath);
            }
        }

        private async Task PerformRestore(string filePath)
        {
            var result = MessageBox.Show(
                $"WARNING: This will replace ALL current data with the backup data.\n\n" +
                $"File: {Path.GetFileName(filePath)}\n\n" +
                "This action CANNOT be undone!\n\n" +
                "The application will close after restore.\n\n" +
                "Are you absolutely sure you want to continue?",
                "Confirm Restore",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Second confirmation
            result = MessageBox.Show(
                "FINAL WARNING!\n\nAll current data will be permanently replaced.\n\nProceed with restore?",
                "Final Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);

            if (result != MessageBoxResult.Yes) return;

            _isRestoreInProgress = true;
            BtnRestoreBackup.IsEnabled = false;
            RestoreProgressPanel.Visibility = Visibility.Visible;

            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                var restoreResult = await Task.Run(() =>
                {
                    return _backupService.RestoreBackup(filePath, currentUser, (progress, message) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            RestoreProgressBar.Value = progress;
                            TxtRestorePercent.Text = $"{progress}%";
                            TxtRestoreProgress.Text = message;
                        });
                    });
                });

                if (restoreResult.Success)
                {
                    MessageBox.Show(
                        "Database restored successfully!\n\nThe application will now close. Please restart it.",
                        "Restore Complete",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Close the application
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show($"Restore failed:\n\n{restoreResult.Message}",
                        "Restore Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during restore: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isRestoreInProgress = false;
                BtnRestoreBackup.IsEnabled = true;
                RestoreProgressPanel.Visibility = Visibility.Collapsed;
                RestoreProgressBar.Value = 0;
            }
        }

        #endregion

        #region Backup History Actions

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BackupHistory backup)
            {
                try
                {
                    string folder = Path.GetDirectoryName(backup.FilePath);
                    if (Directory.Exists(folder))
                    {
                        Process.Start("explorer.exe", folder);
                    }
                    else
                    {
                        MessageBox.Show("Folder not found.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open folder: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BackupHistory backup)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this backup?\n\n{backup.FileName}\n\nThis action cannot be undone.",
                    "Delete Backup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _backupService.DeleteBackup(backup.Id);

                        // Log activity
                        var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                        _activityLogService.LogActivity(
                            activityType: "Delete",
                            description: $"Deleted backup: {backup.FileName}",
                            module: "SystemSettings",
                            performedBy: currentUser,
                            performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                        );

                        // Refresh
                        LoadBackupStats();
                        LoadBackupHistory();

                        MessageBox.Show("Backup deleted successfully.",
                            "Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting backup: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion

        #region Refresh Buttons

        private void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            LoadBackupStats();
        }

        private void BtnRefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            LoadBackupHistory();
        }

        #endregion
    }
}
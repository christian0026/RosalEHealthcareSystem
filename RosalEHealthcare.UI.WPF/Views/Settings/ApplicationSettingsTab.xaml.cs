using Microsoft.Win32;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class ApplicationSettingsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;

        public ApplicationSettingsTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _activityLogService = new ActivityLogService(_db);
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;

            try
            {
                LoadApplicationInfo();
                LoadSystemInfo();
                LoadDeveloperOptions();
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

        private void LoadApplicationInfo()
        {
            // Get version from settings or assembly
            string version = _settingsService.GetApplicationVersion();
            TxtVersion.Text = $"Version {version}";

            // Build date (use assembly date or current year)
            var assemblyDate = File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location);
            TxtBuildDate.Text = $"Built: {assemblyDate:MMMM yyyy}";
        }

        private void LoadSystemInfo()
        {
            // Operating System
            TxtOperatingSystem.Text = Environment.OSVersion.ToString();

            // .NET Version
            TxtDotNetVersion.Text = Environment.Version.ToString();

            // Machine Name
            TxtMachineName.Text = Environment.MachineName;

            // Windows User
            TxtWindowsUser.Text = Environment.UserName;

            // Memory Usage
            var process = Process.GetCurrentProcess();
            long memoryMB = process.WorkingSet64 / (1024 * 1024);
            TxtMemoryUsage.Text = $"{memoryMB} MB";

            // Application Path
            TxtAppPath.Text = AppDomain.CurrentDomain.BaseDirectory;
        }

        private void LoadDeveloperOptions()
        {
            ChkDebugMode.IsChecked = _settingsService.GetDebugModeEnabled();
            ChkEnableAuditLogs.IsChecked = _settingsService.GetEnableAuditLogs();
        }

        #endregion

        #region Developer Options

        private void ChkDebugMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                bool enabled = ChkDebugMode.IsChecked ?? false;

                _settingsService.SetDebugModeEnabled(enabled, currentUser);

                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: $"Debug mode {(enabled ? "enabled" : "disabled")}",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving setting: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkEnableAuditLogs_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;

            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                bool enabled = ChkEnableAuditLogs.IsChecked ?? false;

                _settingsService.SetEnableAuditLogs(enabled, currentUser);

                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: $"Audit logging {(enabled ? "enabled" : "disabled")}",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving setting: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Help & Support

        private void BtnUserManual_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check if manual exists
                string manualPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation", "UserManual.pdf");

                if (File.Exists(manualPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = manualPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        "User manual is not available yet.\n\n" +
                        "Please contact your system administrator for assistance.",
                        "Manual Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening manual: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnContactSupport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var supportInfo = new StringBuilder();
                supportInfo.AppendLine("RosalE Healthcare Support");
                supportInfo.AppendLine("══════════════════════════════");
                supportInfo.AppendLine();
                supportInfo.AppendLine("Email: support@rosalhealthcare.com");
                supportInfo.AppendLine("Phone: +63 (XXX) XXX-XXXX");
                supportInfo.AppendLine("Hours: Monday - Friday, 8:00 AM - 5:00 PM");
                supportInfo.AppendLine();
                supportInfo.AppendLine("When contacting support, please have ready:");
                supportInfo.AppendLine("• Your clinic name and account ID");
                supportInfo.AppendLine("• Description of the issue");
                supportInfo.AppendLine("• Screenshots if applicable");
                supportInfo.AppendLine($"• Application version: {_settingsService.GetApplicationVersion()}");

                MessageBox.Show(supportInfo.ToString(),
                    "Contact Support",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnReportBug_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "To report a bug, please email support@rosalhealthcare.com with:\n\n" +
                "1. Description of the problem\n" +
                "2. Steps to reproduce the issue\n" +
                "3. Expected behavior vs actual behavior\n" +
                "4. Screenshots if applicable\n" +
                "5. Any error messages displayed\n\n" +
                "You can also use the 'Export Diagnostics' button to generate a diagnostic report to attach.",
                "Report a Bug",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnFeatureRequest_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "We value your feedback!\n\n" +
                "To request a new feature, please email support@rosalhealthcare.com with:\n\n" +
                "1. Description of the feature\n" +
                "2. How this feature would help your workflow\n" +
                "3. Any examples or mockups (optional)\n\n" +
                "Our team reviews all feature requests and considers them for future updates.",
                "Feature Request",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        #region Developer Actions

        private void BtnViewErrorLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

                if (Directory.Exists(logPath))
                {
                    Process.Start("explorer.exe", logPath);
                }
                else
                {
                    // Create the directory and inform user
                    Directory.CreateDirectory(logPath);
                    MessageBox.Show(
                        "Log directory has been created.\n\n" +
                        $"Path: {logPath}\n\n" +
                        "Error logs will be saved here when issues occur.",
                        "Logs Directory",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening logs folder: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearCache_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will clear all cached data including:\n\n" +
                "• Settings cache\n" +
                "• Permission cache\n" +
                "• Temporary files\n\n" +
                "The application may be slower temporarily while caches are rebuilt.\n\n" +
                "Do you want to continue?",
                "Clear Cache",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Clear settings cache
                    _settingsService.ClearCache();

                    // Clear any temp files
                    string tempPath = Path.Combine(Path.GetTempPath(), "RosalHealthcare");
                    if (Directory.Exists(tempPath))
                    {
                        Directory.Delete(tempPath, true);
                    }

                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                    _activityLogService.LogActivity(
                        activityType: "Maintenance",
                        description: "Cleared application cache",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    MessageBox.Show("Cache cleared successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing cache: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnExportDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Save Diagnostic Report",
                    Filter = "Text Files (*.txt)|*.txt",
                    FileName = $"RosalHealthcare_Diagnostics_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    var report = new StringBuilder();
                    report.AppendLine("═══════════════════════════════════════════════════════════════");
                    report.AppendLine("          ROSALE HEALTHCARE SYSTEM - DIAGNOSTIC REPORT");
                    report.AppendLine("═══════════════════════════════════════════════════════════════");
                    report.AppendLine();
                    report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    report.AppendLine();

                    // Application Info
                    report.AppendLine("── APPLICATION INFO ──────────────────────────────────────────");
                    report.AppendLine($"Version: {_settingsService.GetApplicationVersion()}");
                    report.AppendLine($"Build Date: {File.GetLastWriteTime(Assembly.GetExecutingAssembly().Location):yyyy-MM-dd}");
                    report.AppendLine($"Install Path: {AppDomain.CurrentDomain.BaseDirectory}");
                    report.AppendLine();

                    // System Info
                    report.AppendLine("── SYSTEM INFO ───────────────────────────────────────────────");
                    report.AppendLine($"Operating System: {Environment.OSVersion}");
                    report.AppendLine($".NET Framework: {Environment.Version}");
                    report.AppendLine($"Machine Name: {Environment.MachineName}");
                    report.AppendLine($"User Name: {Environment.UserName}");
                    report.AppendLine($"Processors: {Environment.ProcessorCount}");
                    report.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
                    report.AppendLine($"64-bit Process: {Environment.Is64BitProcess}");
                    report.AppendLine();

                    // Memory Info
                    report.AppendLine("── MEMORY INFO ───────────────────────────────────────────────");
                    var process = Process.GetCurrentProcess();
                    report.AppendLine($"Working Set: {process.WorkingSet64 / (1024 * 1024)} MB");
                    report.AppendLine($"Peak Working Set: {process.PeakWorkingSet64 / (1024 * 1024)} MB");
                    report.AppendLine($"Private Memory: {process.PrivateMemorySize64 / (1024 * 1024)} MB");
                    report.AppendLine();

                    // Database Info
                    report.AppendLine("── DATABASE INFO ─────────────────────────────────────────────");
                    try
                    {
                        var connectionString = _db.Database.Connection.ConnectionString;
                        var builder = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                        report.AppendLine($"Server: {builder.DataSource}");
                        report.AppendLine($"Database: {builder.InitialCatalog}");
                        report.AppendLine($"Connection State: {_db.Database.Connection.State}");
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine($"Error getting database info: {ex.Message}");
                    }
                    report.AppendLine();

                    // Current User
                    report.AppendLine("── CURRENT USER ──────────────────────────────────────────────");
                    if (SessionManager.CurrentUser != null)
                    {
                        report.AppendLine($"Name: {SessionManager.CurrentUser.FullName}");
                        report.AppendLine($"Role: {SessionManager.CurrentUser.Role}");
                        report.AppendLine($"Username: {SessionManager.CurrentUser.Username}");
                    }
                    else
                    {
                        report.AppendLine("No user logged in");
                    }
                    report.AppendLine();

                    // Settings
                    report.AppendLine("── SETTINGS ──────────────────────────────────────────────────");
                    report.AppendLine($"Debug Mode: {_settingsService.GetDebugModeEnabled()}");
                    report.AppendLine($"Audit Logs: {_settingsService.GetEnableAuditLogs()}");
                    report.AppendLine($"Session Timeout: {_settingsService.GetSessionTimeoutMinutes()} minutes");
                    report.AppendLine($"Max Failed Logins: {_settingsService.GetMaxFailedLoginAttempts()}");
                    report.AppendLine();

                    report.AppendLine("═══════════════════════════════════════════════════════════════");
                    report.AppendLine("                    END OF DIAGNOSTIC REPORT");
                    report.AppendLine("═══════════════════════════════════════════════════════════════");

                    File.WriteAllText(dialog.FileName, report.ToString());

                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                    _activityLogService.LogActivity(
                        activityType: "Export",
                        description: "Exported diagnostic report",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    MessageBox.Show($"Diagnostic report saved to:\n{dialog.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Ask if user wants to open the file
                    var openResult = MessageBox.Show("Would you like to open the report?",
                        "Open Report", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (openResult == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting diagnostics: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetAllSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "WARNING: This will reset ALL system settings to their default values!\n\n" +
                "This includes:\n" +
                "• General Settings\n" +
                "• Notification Settings\n" +
                "• Security Settings\n" +
                "• Backup Settings\n" +
                "• All Role Permissions\n\n" +
                "This action CANNOT be undone!\n\n" +
                "Are you sure you want to continue?",
                "Reset All Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Second confirmation
            result = MessageBox.Show(
                "FINAL WARNING!\n\n" +
                "All settings will be permanently reset to defaults.\n\n" +
                "Are you absolutely sure?",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                // Reset all categories
                _settingsService.ResetCategoryToDefaults("General", currentUser);
                _settingsService.ResetCategoryToDefaults("Notification", currentUser);
                _settingsService.ResetCategoryToDefaults("Security", currentUser);
                _settingsService.ResetCategoryToDefaults("Backup", currentUser);
                _settingsService.ResetCategoryToDefaults("Database", currentUser);
                _settingsService.ResetCategoryToDefaults("Application", currentUser);

                // Reset role permissions
                var rolePermissionService = new RolePermissionService(_db);
                rolePermissionService.ResetAllToDefaults(currentUser);

                _activityLogService.LogActivity(
                    activityType: "Reset",
                    description: "Reset ALL system settings to defaults",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );

                MessageBox.Show(
                    "All settings have been reset to defaults.\n\n" +
                    "Please restart the application for all changes to take effect.",
                    "Reset Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Reload settings
                LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}
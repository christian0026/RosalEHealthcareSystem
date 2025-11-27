using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class DatabaseSettingsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;

        public DatabaseSettingsTab()
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
                LoadDatabaseInfo();
                LoadTableStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading database settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadDatabaseInfo()
        {
            try
            {
                var connectionString = _db.Database.Connection.ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);

                TxtServerName.Text = builder.DataSource;
                TxtDatabaseName.Text = builder.InitialCatalog;
                TxtLastChecked.Text = DateTime.Now.ToString("MMM dd, yyyy HH:mm:ss");

                // Get additional info from database
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // Get database size
                    string sizeSql = @"
                        SELECT 
                            SUM(size * 8 / 1024) AS SizeMB
                        FROM sys.database_files";

                    using (var cmd = new SqlCommand(sizeSql, connection))
                    {
                        var size = cmd.ExecuteScalar();
                        if (size != null && size != DBNull.Value)
                        {
                            decimal sizeMB = Convert.ToDecimal(size);
                            TxtDatabaseSize.Text = sizeMB >= 1024
                                ? $"{sizeMB / 1024:F2} GB"
                                : $"{sizeMB:F2} MB";
                        }
                    }

                    // Get SQL Server version
                    string versionSql = "SELECT @@VERSION";
                    using (var cmd = new SqlCommand(versionSql, connection))
                    {
                        var version = cmd.ExecuteScalar()?.ToString();
                        if (!string.IsNullOrEmpty(version))
                        {
                            // Extract just the version number
                            int idx = version.IndexOf('\n');
                            TxtSqlVersion.Text = idx > 0 ? version.Substring(0, idx).Trim() : version;
                        }
                    }

                    // Get recovery model
                    string recoverySql = @"
                        SELECT recovery_model_desc 
                        FROM sys.databases 
                        WHERE name = DB_NAME()";

                    using (var cmd = new SqlCommand(recoverySql, connection))
                    {
                        var recovery = cmd.ExecuteScalar()?.ToString();
                        TxtRecoveryModel.Text = recovery ?? "Unknown";
                    }
                }

                UpdateConnectionStatus(true);
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                TxtServerName.Text = "Error";
                TxtDatabaseName.Text = ex.Message;
            }
        }

        private void LoadTableStatistics()
        {
            try
            {
                var tableStats = new List<TableStatistic>();
                var connectionString = _db.Database.Connection.ConnectionString;

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT 
                            t.NAME AS TableName,
                            p.rows AS RecordCount,
                            SUM(a.total_pages) * 8 AS TotalSpaceKB
                        FROM sys.tables t
                        INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
                        INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
                        INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
                        WHERE t.is_ms_shipped = 0 AND i.OBJECT_ID > 255
                        GROUP BY t.Name, p.Rows
                        ORDER BY TotalSpaceKB DESC";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                tableStats.Add(new TableStatistic
                                {
                                    TableName = reader["TableName"].ToString(),
                                    RecordCount = Convert.ToInt64(reader["RecordCount"]),
                                    SizeKB = Convert.ToInt64(reader["TotalSpaceKB"])
                                });
                            }
                        }
                    }
                }

                TableStatsList.ItemsSource = tableStats;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading table stats: {ex.Message}");
            }
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionStatusBadge.Style = (Style)FindResource("StatusBadgeConnected");
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                TxtConnectionStatus.Text = "Connected";
                TxtConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
            }
            else
            {
                ConnectionStatusBadge.Style = (Style)FindResource("StatusBadgeDisconnected");
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                TxtConnectionStatus.Text = "Disconnected";
                TxtConnectionStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));
            }
        }

        #endregion

        #region Button Handlers

        private void BtnTestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _db.Database.Connection.Open();
                _db.Database.Connection.Close();

                UpdateConnectionStatus(true);
                TxtLastChecked.Text = DateTime.Now.ToString("MMM dd, yyyy HH:mm:ss");

                MessageBox.Show("Database connection successful!",
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateConnectionStatus(false);
                MessageBox.Show($"Connection failed: {ex.Message}",
                    "Connection Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRefreshStats_Click(object sender, RoutedEventArgs e)
        {
            LoadTableStatistics();
            LoadDatabaseInfo();
        }

        private async void BtnOptimizeDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will reorganize indexes and update statistics to improve database performance.\n\nDo you want to continue?",
                "Optimize Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await RunMaintenanceTask("Optimizing database...", async () =>
            {
                var connectionString = _db.Database.Connection.ConnectionString;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Reorganize all indexes
                    string sql = @"
                        DECLARE @TableName NVARCHAR(255)
                        DECLARE TableCursor CURSOR FOR
                        SELECT name FROM sys.tables WHERE is_ms_shipped = 0

                        OPEN TableCursor
                        FETCH NEXT FROM TableCursor INTO @TableName

                        WHILE @@FETCH_STATUS = 0
                        BEGIN
                            EXEC('ALTER INDEX ALL ON [' + @TableName + '] REORGANIZE')
                            FETCH NEXT FROM TableCursor INTO @TableName
                        END

                        CLOSE TableCursor
                        DEALLOCATE TableCursor

                        -- Update statistics
                        EXEC sp_updatestats";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.CommandTimeout = 3600;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                LogActivity("Optimized database indexes and statistics");
            });

            LoadDatabaseInfo();
            LoadTableStatistics();
        }

        private async void BtnCheckIntegrity_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will check the database for any corruption or integrity issues.\n\nDo you want to continue?",
                "Check Integrity",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            string integrityResult = "";

            await RunMaintenanceTask("Checking database integrity...", async () =>
            {
                var connectionString = _db.Database.Connection.ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string sql = $"DBCC CHECKDB ('{builder.InitialCatalog}') WITH NO_INFOMSGS";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.CommandTimeout = 3600;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                integrityResult = "Database integrity check completed successfully. No issues found.";
                LogActivity("Performed database integrity check");
            });

            MessageBox.Show(integrityResult,
                "Integrity Check Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnShrinkDatabase_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Shrinking the database can temporarily affect performance and may cause index fragmentation.\n\n" +
                "This is typically only needed after deleting large amounts of data.\n\n" +
                "Do you want to continue?",
                "Shrink Database",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await RunMaintenanceTask("Shrinking database...", async () =>
            {
                var connectionString = _db.Database.Connection.ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string sql = $"DBCC SHRINKDATABASE ('{builder.InitialCatalog}', 10)";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        cmd.CommandTimeout = 3600;
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                LogActivity("Shrunk database to reclaim space");
            });

            LoadDatabaseInfo();
            LoadTableStatistics();
        }

        private async void BtnClearTempData_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will clear temporary data and cached information.\n\nDo you want to continue?",
                "Clear Temp Data",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            await RunMaintenanceTask("Clearing temporary data...", async () =>
            {
                // Clear any temp tables or cached data
                // For this application, we'll just clear the query plan cache
                var connectionString = _db.Database.Connection.ConnectionString;

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string sql = "DBCC FREEPROCCACHE; DBCC DROPCLEANBUFFERS;";

                    using (var cmd = new SqlCommand(sql, connection))
                    {
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                LogActivity("Cleared temporary data and cache");
            });
        }

        private async void BtnCleanupLogs_Click(object sender, RoutedEventArgs e)
        {
            int retentionDays = _settingsService.GetLogRetentionDays();

            var result = MessageBox.Show(
                $"This will permanently delete activity logs and login history older than {retentionDays} days.\n\n" +
                "This action cannot be undone. Make sure you have a backup.\n\n" +
                "Do you want to continue?",
                "Cleanup Old Logs",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            int deletedCount = 0;

            await RunMaintenanceTask("Cleaning up old logs...", async () =>
            {
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                // Delete old activity logs
                var oldLogs = _db.ActivityLogs.Where(l => l.PerformedAt < cutoffDate);
                deletedCount = oldLogs.Count();
                _db.ActivityLogs.RemoveRange(oldLogs);

                // Delete old login history
                var oldLogins = _db.LoginHistories.Where(l => l.LoginTime < cutoffDate);
                deletedCount += oldLogins.Count();
                _db.LoginHistories.RemoveRange(oldLogins);

                await _db.SaveChangesAsync();

                LogActivity($"Cleaned up {deletedCount} old log records");
            });

            MessageBox.Show($"Successfully deleted {deletedCount} old log records.",
                "Cleanup Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadTableStatistics();
        }

        private void BtnArchiveRecords_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Archive functionality will export old records to a separate file before deletion.\n\n" +
                "This feature is planned for a future update.",
                "Coming Soon",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void BtnPurgeDeleted_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will permanently remove all soft-deleted records from the database.\n\n" +
                "WARNING: This action CANNOT be undone!\n\n" +
                "Make sure you have a backup before proceeding.\n\n" +
                "Do you want to continue?",
                "Purge Deleted Records",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // Second confirmation
            result = MessageBox.Show(
                "FINAL WARNING!\n\n" +
                "All deleted records will be permanently removed.\n\n" +
                "Are you absolutely sure?",
                "Confirm Purge",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation);

            if (result != MessageBoxResult.Yes) return;

            int purgedCount = 0;

            await RunMaintenanceTask("Purging deleted records...", async () =>
            {
                // Delete users marked as deleted
                var deletedUsers = _db.Users.Where(u => u.Status == "Inactive" || u.Status == "Deleted").ToList();
                purgedCount += deletedUsers.Count();
                _db.Users.RemoveRange(deletedUsers);

                await _db.SaveChangesAsync();

                LogActivity($"Purged {purgedCount} deleted records");
            });

            MessageBox.Show($"Successfully purged {purgedCount} deleted records.",
                "Purge Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            LoadTableStatistics();
        }

        #endregion

        #region Helpers

        private async Task RunMaintenanceTask(string message, Func<Task> task)
        {
            MaintenanceProgressPanel.Visibility = Visibility.Visible;
            TxtMaintenanceProgress.Text = message;
            SetButtonsEnabled(false);

            try
            {
                await task();

                MessageBox.Show("Operation completed successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation failed: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MaintenanceProgressPanel.Visibility = Visibility.Collapsed;
                SetButtonsEnabled(true);
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            BtnOptimizeDatabase.IsEnabled = enabled;
            BtnCheckIntegrity.IsEnabled = enabled;
            BtnShrinkDatabase.IsEnabled = enabled;
            BtnClearTempData.IsEnabled = enabled;
            BtnCleanupLogs.IsEnabled = enabled;
            BtnArchiveRecords.IsEnabled = enabled;
            BtnPurgeDeleted.IsEnabled = enabled;
        }

        private void LogActivity(string description)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                _activityLogService.LogActivity(
                    activityType: "Maintenance",
                    description: description,
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );
            }
            catch { /* Ignore logging errors */ }
        }

        #endregion
    }

    /// <summary>
    /// Helper class for table statistics
    /// </summary>
    public class TableStatistic
    {
        public string TableName { get; set; }
        public long RecordCount { get; set; }
        public long SizeKB { get; set; }

        public string SizeFormatted
        {
            get
            {
                if (SizeKB >= 1024 * 1024)
                    return $"{SizeKB / 1024.0 / 1024.0:F2} GB";
                if (SizeKB >= 1024)
                    return $"{SizeKB / 1024.0:F2} MB";
                return $"{SizeKB} KB";
            }
        }
    }
}
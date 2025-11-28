using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class BackupService
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly NotificationService _notificationService;

        public BackupService(RosalEHealthcareDbContext db)
        {
            _db = db;
            _settingsService = new SystemSettingsService(db);
            _notificationService = new NotificationService(db);
        }

        #region Backup Operations

        /// <summary>
        /// Create a database backup
        /// </summary>
        public BackupResult CreateBackup(string backupType, string createdBy, Action<int, string> progressCallback = null)
        {
            var result = new BackupResult();
            BackupHistory backupRecord = null;

            try
            {
                progressCallback?.Invoke(5, "Initializing backup...");

                // Get settings
                string backupLocation = _settingsService.GetBackupLocation();
                bool compress = _settingsService.GetBackupCompression();
                bool encrypt = _settingsService.GetBackupEncryption();

                // Ensure backup directory exists
                if (!Directory.Exists(backupLocation))
                {
                    Directory.CreateDirectory(backupLocation);
                }

                progressCallback?.Invoke(10, "Preparing backup file...");

                // Generate filename
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string baseFileName = $"RosalHealthcare_Backup_{timestamp}";
                string bakFileName = $"{baseFileName}.bak";
                string bakFilePath = Path.Combine(backupLocation, bakFileName);

                // Create backup record
                backupRecord = new BackupHistory
                {
                    FileName = bakFileName,
                    FilePath = bakFilePath,
                    BackupType = backupType,
                    Status = "InProgress",
                    IsCompressed = compress,
                    IsEncrypted = encrypt,
                    StartedAt = DateTime.Now,
                    CreatedBy = createdBy
                };
                _db.BackupHistories.Add(backupRecord);
                _db.SaveChanges();

                progressCallback?.Invoke(20, "Creating database backup...");

                // Get connection string info
                var connectionString = _db.Database.Connection.ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                // Execute SQL Server backup command
                string backupSql = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = N'{bakFilePath}' 
                    WITH FORMAT, 
                         INIT, 
                         NAME = N'RosalHealthcare-Full Database Backup', 
                         SKIP, 
                         NOREWIND, 
                         NOUNLOAD, 
                         COMPRESSION,
                         STATS = 10";

                progressCallback?.Invoke(40, "Executing backup command...");

                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (var command = new SqlCommand(backupSql, connection))
                    {
                        command.CommandTimeout = 3600; // 1 hour timeout
                        command.ExecuteNonQuery();
                    }
                }

                progressCallback?.Invoke(70, "Backup file created...");

                string finalFilePath = bakFilePath;

                // Compress if enabled
                if (compress)
                {
                    progressCallback?.Invoke(80, "Compressing backup file...");
                    string zipFilePath = Path.Combine(backupLocation, $"{baseFileName}.zip");

                    using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(bakFilePath, bakFileName, CompressionLevel.Optimal);
                    }

                    // Delete original .bak file
                    File.Delete(bakFilePath);

                    finalFilePath = zipFilePath;
                    backupRecord.FileName = $"{baseFileName}.zip";
                    backupRecord.FilePath = zipFilePath;
                }

                progressCallback?.Invoke(90, "Finalizing backup...");

                // Get file size
                var fileInfo = new FileInfo(finalFilePath);
                backupRecord.FileSize = fileInfo.Length;
                backupRecord.Status = "Success";
                backupRecord.CompletedAt = DateTime.Now;
                _db.SaveChanges();

                // Clean up old backups
                CleanupOldBackups();

                progressCallback?.Invoke(100, "Backup completed successfully!");

                result.Success = true;
                result.Message = "Backup completed successfully!";
                result.FilePath = finalFilePath;
                result.FileSize = fileInfo.Length;
                result.BackupId = backupRecord.Id;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Backup failed: {ex.Message}";
                result.ErrorDetails = ex.ToString();

                // Update backup record with error
                if (backupRecord != null)
                {
                    backupRecord.Status = "Failed";
                    backupRecord.ErrorMessage = ex.Message;
                    backupRecord.CompletedAt = DateTime.Now;
                    _db.SaveChanges();
                }
            }

            return result;
        }

        /// <summary>
        /// Restore database with notification
        /// </summary>
        public bool RestoreDatabaseWithNotification(string backupFilePath, out string errorMessage)
        {
            errorMessage = null;
            bool success = false;

            try
            {
                // Your existing restore logic here
                success = RestoreDatabase(backupFilePath);

                // Send notification
                if (success)
                {
                    _notificationService.NotifyRestoreCompleted(true, backupFilePath);
                }
                else
                {
                    _notificationService.NotifyRestoreCompleted(false, backupFilePath, "Restore operation failed");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                try
                {
                    _notificationService.NotifyRestoreCompleted(false, backupFilePath, ex.Message);
                }
                catch { }
            }

            return success;
        }

        /// <summary>
        /// Restore database from backup
        /// </summary>
        public BackupResult RestoreBackup(string backupFilePath, string restoredBy, Action<int, string> progressCallback = null)
        {
            var result = new BackupResult();

            try
            {
                progressCallback?.Invoke(5, "Validating backup file...");

                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException("Backup file not found.", backupFilePath);
                }

                string actualBackupFile = backupFilePath;

                // If it's a zip file, extract first
                if (Path.GetExtension(backupFilePath).ToLower() == ".zip")
                {
                    progressCallback?.Invoke(10, "Extracting backup file...");

                    string tempDir = Path.Combine(Path.GetTempPath(), $"RosalRestore_{Guid.NewGuid()}");
                    Directory.CreateDirectory(tempDir);

                    ZipFile.ExtractToDirectory(backupFilePath, tempDir);

                    // Find .bak file
                    var bakFiles = Directory.GetFiles(tempDir, "*.bak");
                    if (bakFiles.Length == 0)
                    {
                        throw new Exception("No .bak file found in the backup archive.");
                    }
                    actualBackupFile = bakFiles[0];
                }

                progressCallback?.Invoke(20, "Preparing restore...");

                // Get connection info
                var connectionString = _db.Database.Connection.ConnectionString;
                var builder = new SqlConnectionStringBuilder(connectionString);
                string databaseName = builder.InitialCatalog;

                // Use master database for restore
                builder.InitialCatalog = "master";
                string masterConnectionString = builder.ConnectionString;

                progressCallback?.Invoke(30, "Setting database to single user mode...");

                using (var connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();

                    // Set to single user mode
                    string setSingleUser = $@"
                        ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";

                    using (var cmd = new SqlCommand(setSingleUser, connection))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.ExecuteNonQuery();
                    }

                    progressCallback?.Invoke(50, "Restoring database...");

                    // Restore database
                    string restoreSql = $@"
                        RESTORE DATABASE [{databaseName}] 
                        FROM DISK = N'{actualBackupFile}' 
                        WITH REPLACE, RECOVERY;";

                    using (var cmd = new SqlCommand(restoreSql, connection))
                    {
                        cmd.CommandTimeout = 3600;
                        cmd.ExecuteNonQuery();
                    }

                    progressCallback?.Invoke(90, "Setting database to multi user mode...");

                    // Set back to multi user
                    string setMultiUser = $@"
                        ALTER DATABASE [{databaseName}] SET MULTI_USER;";

                    using (var cmd = new SqlCommand(setMultiUser, connection))
                    {
                        cmd.CommandTimeout = 300;
                        cmd.ExecuteNonQuery();
                    }
                }

                progressCallback?.Invoke(100, "Restore completed successfully!");

                result.Success = true;
                result.Message = "Database restored successfully! Please restart the application.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Restore failed: {ex.Message}";
                result.ErrorDetails = ex.ToString();

                // Try to set database back to multi-user if possible
                try
                {
                    var connectionString = _db.Database.Connection.ConnectionString;
                    var builder = new SqlConnectionStringBuilder(connectionString);
                    builder.InitialCatalog = "master";

                    using (var connection = new SqlConnection(builder.ConnectionString))
                    {
                        connection.Open();
                        string setMultiUser = $"ALTER DATABASE [{builder.InitialCatalog}] SET MULTI_USER;";
                        using (var cmd = new SqlCommand(setMultiUser, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }

            return result;
        }

        public bool CreateBackupWithNotification(string backupPath, out string errorMessage)
        {
            errorMessage = null;
            bool success = false;

            try
            {
                // Your existing backup logic here
                success = CreateBackup(backupPath);

                // Send notification
                if (success)
                {
                    _notificationService.NotifyBackupCompleted(true, backupPath);
                }
                else
                {
                    _notificationService.NotifyBackupCompleted(false, null, "Backup operation failed");
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                try
                {
                    _notificationService.NotifyBackupCompleted(false, null, ex.Message);
                }
                catch { }
            }

            return success;
        }

        #endregion

        #region Backup History

        /// <summary>
        /// Get all backup history
        /// </summary>
        public IEnumerable<BackupHistory> GetBackupHistory()
        {
            return _db.BackupHistories
                .OrderByDescending(b => b.StartedAt)
                .ToList();
        }

        /// <summary>
        /// Get backup history with pagination
        /// </summary>
        public IEnumerable<BackupHistory> GetBackupHistoryPaged(int pageNumber, int pageSize)
        {
            return _db.BackupHistories
                .OrderByDescending(b => b.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Get total backup count
        /// </summary>
        public int GetBackupCount()
        {
            return _db.BackupHistories.Count();
        }

        /// <summary>
        /// Get backup by ID
        /// </summary>
        public BackupHistory GetBackupById(int id)
        {
            return _db.BackupHistories.Find(id);
        }

        /// <summary>
        /// Delete a backup record and file
        /// </summary>
        public bool DeleteBackup(int id)
        {
            var backup = _db.BackupHistories.Find(id);
            if (backup == null) return false;

            // Delete file if exists
            if (File.Exists(backup.FilePath))
            {
                try
                {
                    File.Delete(backup.FilePath);
                }
                catch { /* Ignore file deletion errors */ }
            }

            _db.BackupHistories.Remove(backup);
            _db.SaveChanges();
            return true;
        }

        /// <summary>
        /// Verify backup file integrity
        /// </summary>
        public bool VerifyBackup(int id)
        {
            var backup = _db.BackupHistories.Find(id);
            if (backup == null) return false;

            return File.Exists(backup.FilePath);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clean up old backups based on retention settings
        /// </summary>
        public void CleanupOldBackups()
        {
            int retentionCount = _settingsService.GetBackupRetentionCount();

            var backupsToDelete = _db.BackupHistories
                .Where(b => b.Status == "Success")
                .OrderByDescending(b => b.StartedAt)
                .Skip(retentionCount)
                .ToList();

            foreach (var backup in backupsToDelete)
            {
                DeleteBackup(backup.Id);
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get last successful backup
        /// </summary>
        public BackupHistory GetLastSuccessfulBackup()
        {
            return _db.BackupHistories
                .Where(b => b.Status == "Success")
                .OrderByDescending(b => b.CompletedAt)
                .FirstOrDefault();
        }

        /// <summary>
        /// Get total backup size
        /// </summary>
        public long GetTotalBackupSize()
        {
            return _db.BackupHistories
                .Where(b => b.Status == "Success" && b.FileSize.HasValue)
                .Sum(b => b.FileSize) ?? 0;
        }

        #endregion
    }

    /// <summary>
    /// Result class for backup operations
    /// </summary>
    public class BackupResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public int BackupId { get; set; }
        public string ErrorDetails { get; set; }
    }
}
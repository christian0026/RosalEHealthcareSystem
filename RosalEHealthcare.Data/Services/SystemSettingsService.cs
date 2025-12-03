using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class SystemSettingsService
    {
        private readonly RosalEHealthcareDbContext _db;
        private static Dictionary<string, SystemSetting> _cache;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private const int CACHE_MINUTES = 5;

        public SystemSettingsService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Cache Management

        private void RefreshCacheIfNeeded()
        {
            if (_cache == null || DateTime.Now > _cacheExpiry)
            {
                RefreshCache();
            }
        }

        public void RefreshCache()
        {
            try
            {
                var settings = _db.SystemSettings.ToList();
                _cache = settings.ToDictionary(s => s.SettingKey, s => s);
                _cacheExpiry = DateTime.Now.AddMinutes(CACHE_MINUTES);
            }
            catch
            {
                _cache = new Dictionary<string, SystemSetting>();
            }
        }

        public void ClearCache()
        {
            _cache = null;
            _cacheExpiry = DateTime.MinValue;
        }

        #endregion

        #region Get Settings

        /// <summary>
        /// Get all settings
        /// </summary>
        public IEnumerable<SystemSetting> GetAll()
        {
            return _db.SystemSettings.OrderBy(s => s.Category).ThenBy(s => s.SettingKey).ToList();
        }

        /// <summary>
        /// Get settings by category
        /// </summary>
        public IEnumerable<SystemSetting> GetByCategory(string category)
        {
            return _db.SystemSettings
                .Where(s => s.Category == category)
                .OrderBy(s => s.SettingKey)
                .ToList();
        }

        /// <summary>
        /// Get a single setting by key
        /// </summary>
        public SystemSetting GetByKey(string key)
        {
            RefreshCacheIfNeeded();
            return _cache.TryGetValue(key, out var setting) ? setting : null;
        }

        /// <summary>
        /// Get string value
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            var setting = GetByKey(key);
            return setting?.GetString() ?? defaultValue;
        }

        /// <summary>
        /// Get integer value
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            var setting = GetByKey(key);
            return setting?.GetInt(defaultValue) ?? defaultValue;
        }

        /// <summary>
        /// Get boolean value
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            var setting = GetByKey(key);
            return setting?.GetBool(defaultValue) ?? defaultValue;
        }

        /// <summary>
        /// Get DateTime value
        /// </summary>
        public DateTime? GetDateTime(string key)
        {
            var setting = GetByKey(key);
            return setting?.GetDateTime();
        }

        /// <summary>
        /// Gets the lockout duration in minutes
        /// </summary>
        /// <summary>
        /// Gets the lockout duration in minutes
        /// </summary>
        public int GetLockoutDurationMinutes()
        {
            return GetInt("LockoutDurationMinutes", 5);
        }

        /// <summary>
        /// Sets the lockout duration in minutes
        /// </summary>
        public void SetLockoutDurationMinutes(int minutes, string modifiedBy)
        {
            SetInt("LockoutDurationMinutes", minutes, modifiedBy);
        }
        #endregion

        #region Set Settings

        /// <summary>
        /// Set a setting value
        /// </summary>
        public void Set(string key, string value, string modifiedBy = null)
        {
            var setting = _db.SystemSettings.FirstOrDefault(s => s.SettingKey == key);

            if (setting != null)
            {
                setting.SettingValue = value;
                setting.LastModified = DateTime.Now;
                setting.ModifiedBy = modifiedBy;
                _db.SaveChanges();
            }
            else
            {
                // Create new setting if doesn't exist
                setting = new SystemSetting
                {
                    SettingKey = key,
                    SettingValue = value,
                    SettingType = "String",
                    Category = "General",
                    LastModified = DateTime.Now,
                    ModifiedBy = modifiedBy
                };
                _db.SystemSettings.Add(setting);
                _db.SaveChanges();
            }

            ClearCache();
        }

        /// <summary>
        /// Set integer value
        /// </summary>
        public void SetInt(string key, int value, string modifiedBy = null)
        {
            Set(key, value.ToString(), modifiedBy);
        }

        /// <summary>
        /// Set boolean value
        /// </summary>
        public void SetBool(string key, bool value, string modifiedBy = null)
        {
            Set(key, value.ToString().ToLower(), modifiedBy);
        }

        /// <summary>
        /// Update multiple settings at once
        /// </summary>
        public void UpdateMultiple(Dictionary<string, string> settings, string modifiedBy = null)
        {
            foreach (var kvp in settings)
            {
                var setting = _db.SystemSettings.FirstOrDefault(s => s.SettingKey == kvp.Key);
                if (setting != null)
                {
                    setting.SettingValue = kvp.Value;
                    setting.LastModified = DateTime.Now;
                    setting.ModifiedBy = modifiedBy;
                }
            }

            _db.SaveChanges();
            ClearCache();
        }

        #endregion

        #region User-Specific Settings

        /// <summary>
        /// Gets a user-specific setting
        /// </summary>
        public string GetUserSetting(string userId, string key, string defaultValue = "")
        {
            string category = $"User_{userId}";
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Category == category && s.SettingKey == key);
            return setting?.SettingValue ?? defaultValue;
        }

        /// <summary>
        /// Saves a user-specific setting
        /// </summary>
        public void SaveUserSetting(string userId, string key, string value, string modifiedBy)
        {
            string category = $"User_{userId}";
            var setting = _db.SystemSettings.FirstOrDefault(s => s.Category == category && s.SettingKey == key);

            if (setting == null)
            {
                // Create new setting
                setting = new SystemSetting
                {
                    Category = category,
                    SettingKey = key,
                    SettingValue = value,
                    SettingType = "String",
                    DataType = "String",
                    CreatedAt = DateTime.Now,
                    ModifiedAt = DateTime.Now,
                    LastModified = DateTime.Now,
                    ModifiedBy = modifiedBy
                };
                _db.SystemSettings.Add(setting);
            }
            else
            {
                // Update existing setting
                setting.SettingValue = value;
                setting.ModifiedAt = DateTime.Now;
                setting.LastModified = DateTime.Now;
                setting.ModifiedBy = modifiedBy;
            }

            _db.SaveChanges();
            ClearCache();
        }

        /// <summary>
        /// Gets the last modified date for any setting
        /// </summary>
        public DateTime? GetLastModifiedDate()
        {
            return _db.SystemSettings
                .OrderByDescending(s => s.LastModified)
                .Select(s => s.LastModified)
                .FirstOrDefault();
        }

        #endregion

        #region Specific Settings Helpers

        // ===== GENERAL SETTINGS =====

        public string GetClinicName() => GetString("ClinicName", "Rosal Medical Clinic");
        public void SetClinicName(string value, string modifiedBy) => Set("ClinicName", value, modifiedBy);

        public string GetClinicAddress() => GetString("ClinicAddress", "");
        public void SetClinicAddress(string value, string modifiedBy) => Set("ClinicAddress", value, modifiedBy);

        public string GetClinicContactNumber() => GetString("ClinicContactNumber", "");
        public void SetClinicContactNumber(string value, string modifiedBy) => Set("ClinicContactNumber", value, modifiedBy);

        public string GetDateFormat() => GetString("DateFormat", "MM/dd/yyyy");
        public void SetDateFormat(string value, string modifiedBy) => Set("DateFormat", value, modifiedBy);

        public string GetTimeFormat() => GetString("TimeFormat", "12");
        public void SetTimeFormat(string value, string modifiedBy) => Set("TimeFormat", value, modifiedBy);

        public string GetTimezone() => GetString("Timezone", "Asia/Manila");
        public void SetTimezone(string value, string modifiedBy) => Set("Timezone", value, modifiedBy);

        public string GetDefaultLandingPage() => GetString("DefaultLandingPage", "Dashboard");
        public void SetDefaultLandingPage(string value, string modifiedBy) => Set("DefaultLandingPage", value, modifiedBy);

        public int GetItemsPerPage() => GetInt("ItemsPerPage", 10);
        public void SetItemsPerPage(int value, string modifiedBy) => SetInt("ItemsPerPage", value, modifiedBy);

        public int GetAutoRefreshInterval() => GetInt("AutoRefreshInterval", 0);
        public void SetAutoRefreshInterval(int value, string modifiedBy) => SetInt("AutoRefreshInterval", value, modifiedBy);

        public bool GetEnableSoundNotifications() => GetBool("EnableSoundNotifications", true);
        public void SetEnableSoundNotifications(bool value, string modifiedBy) => SetBool("EnableSoundNotifications", value, modifiedBy);

        // ===== NOTIFICATION SETTINGS =====

        public bool GetEnableInAppNotifications() => GetBool("EnableInAppNotifications", true);
        public void SetEnableInAppNotifications(bool value, string modifiedBy) => SetBool("EnableInAppNotifications", value, modifiedBy);

        public bool GetEnableLowStockAlerts() => GetBool("EnableLowStockAlerts", true);
        public void SetEnableLowStockAlerts(bool value, string modifiedBy) => SetBool("EnableLowStockAlerts", value, modifiedBy);

        public int GetLowStockThreshold() => GetInt("LowStockThreshold", 50);
        public void SetLowStockThreshold(int value, string modifiedBy) => SetInt("LowStockThreshold", value, modifiedBy);

        public bool GetEnableExpiryAlerts() => GetBool("EnableExpiryAlerts", true);
        public void SetEnableExpiryAlerts(bool value, string modifiedBy) => SetBool("EnableExpiryAlerts", value, modifiedBy);

        public int GetExpiryAlertDays() => GetInt("ExpiryAlertDays", 30);
        public void SetExpiryAlertDays(int value, string modifiedBy) => SetInt("ExpiryAlertDays", value, modifiedBy);

        public bool GetEnableAppointmentReminders() => GetBool("EnableAppointmentReminders", true);
        public void SetEnableAppointmentReminders(bool value, string modifiedBy) => SetBool("EnableAppointmentReminders", value, modifiedBy);

        public int GetAppointmentReminderHours() => GetInt("AppointmentReminderHours", 1);
        public void SetAppointmentReminderHours(int value, string modifiedBy) => SetInt("AppointmentReminderHours", value, modifiedBy);

        public string GetLowStockAlertRecipients() => GetString("LowStockAlertRecipients", "Administrator,Doctor");
        public void SetLowStockAlertRecipients(string value, string modifiedBy) => Set("LowStockAlertRecipients", value, modifiedBy);

        public string GetAppointmentReminderRecipients() => GetString("AppointmentReminderRecipients", "Doctor,Receptionist");
        public void SetAppointmentReminderRecipients(string value, string modifiedBy) => Set("AppointmentReminderRecipients", value, modifiedBy);

        // ===== SECURITY SETTINGS - PASSWORD =====

        public int GetPasswordMinLength() => GetInt("PasswordMinLength", 8);
        public void SetPasswordMinLength(int value, string modifiedBy) => SetInt("PasswordMinLength", value, modifiedBy);

        public bool GetPasswordRequireUppercase() => GetBool("PasswordRequireUppercase", true);
        public void SetPasswordRequireUppercase(bool value, string modifiedBy) => SetBool("PasswordRequireUppercase", value, modifiedBy);

        public bool GetPasswordRequireLowercase() => GetBool("PasswordRequireLowercase", true);
        public void SetPasswordRequireLowercase(bool value, string modifiedBy) => SetBool("PasswordRequireLowercase", value, modifiedBy);

        public bool GetPasswordRequireNumbers() => GetBool("PasswordRequireNumbers", true);
        public void SetPasswordRequireNumbers(bool value, string modifiedBy) => SetBool("PasswordRequireNumbers", value, modifiedBy);

        public bool GetPasswordRequireSpecial() => GetBool("PasswordRequireSpecial", false);
        public void SetPasswordRequireSpecial(bool value, string modifiedBy) => SetBool("PasswordRequireSpecial", value, modifiedBy);

        public int GetPasswordExpiryDays() => GetInt("PasswordExpiryDays", 0);
        public void SetPasswordExpiryDays(int value, string modifiedBy) => SetInt("PasswordExpiryDays", value, modifiedBy);

        public int GetPasswordHistoryCount() => GetInt("PasswordHistoryCount", 5);
        public void SetPasswordHistoryCount(int value, string modifiedBy) => SetInt("PasswordHistoryCount", value, modifiedBy);

        public bool GetForcePasswordChangeOnFirstLogin() => GetBool("ForcePasswordChangeOnFirstLogin", true);
        public void SetForcePasswordChangeOnFirstLogin(bool value, string modifiedBy) => SetBool("ForcePasswordChangeOnFirstLogin", value, modifiedBy);

        // ===== SECURITY SETTINGS - LOGIN =====

        public int GetMaxFailedLoginAttempts() => GetInt("MaxFailedLoginAttempts", 3);
        public void SetMaxFailedLoginAttempts(int value, string modifiedBy) => SetInt("MaxFailedLoginAttempts", value, modifiedBy);

        public int GetAccountLockoutMinutes() => GetInt("AccountLockoutMinutes", 5);
        public void SetAccountLockoutMinutes(int value, string modifiedBy) => SetInt("AccountLockoutMinutes", value, modifiedBy);

        public bool GetEnableRememberMe() => GetBool("EnableRememberMe", false);
        public void SetEnableRememberMe(bool value, string modifiedBy) => SetBool("EnableRememberMe", value, modifiedBy);

        public int GetRememberMeDays() => GetInt("RememberMeDays", 7);
        public void SetRememberMeDays(int value, string modifiedBy) => SetInt("RememberMeDays", value, modifiedBy);

        // ===== SECURITY SETTINGS - SESSION =====

        public int GetSessionTimeoutMinutes() => GetInt("SessionTimeoutMinutes", 30);
        public void SetSessionTimeoutMinutes(int value, string modifiedBy) => SetInt("SessionTimeoutMinutes", value, modifiedBy);

        public int GetSessionWarningMinutes() => GetInt("SessionWarningMinutes", 5);
        public void SetSessionWarningMinutes(int value, string modifiedBy) => SetInt("SessionWarningMinutes", value, modifiedBy);

        public bool GetAllowConcurrentSessions() => GetBool("AllowConcurrentSessions", true);
        public void SetAllowConcurrentSessions(bool value, string modifiedBy) => SetBool("AllowConcurrentSessions", value, modifiedBy);

        public bool GetForceLogoutOtherSessions() => GetBool("ForceLogoutOtherSessions", false);
        public void SetForceLogoutOtherSessions(bool value, string modifiedBy) => SetBool("ForceLogoutOtherSessions", value, modifiedBy);

        // ===== BACKUP SETTINGS =====

        public string GetBackupLocation() => GetString("BackupLocation", @"C:\RosalHealthcare\Backups");
        public void SetBackupLocation(string value, string modifiedBy) => Set("BackupLocation", value, modifiedBy);

        public bool GetBackupCompression() => GetBool("BackupCompression", true);
        public void SetBackupCompression(bool value, string modifiedBy) => SetBool("BackupCompression", value, modifiedBy);

        public bool GetBackupEncryption() => GetBool("BackupEncryption", false);
        public void SetBackupEncryption(bool value, string modifiedBy) => SetBool("BackupEncryption", value, modifiedBy);

        public string GetBackupEncryptionPassword() => GetString("BackupEncryptionPassword", "");
        public void SetBackupEncryptionPassword(string value, string modifiedBy) => Set("BackupEncryptionPassword", value, modifiedBy);

        public bool GetAutoBackupEnabled() => GetBool("AutoBackupEnabled", false);
        public void SetAutoBackupEnabled(bool value, string modifiedBy) => SetBool("AutoBackupEnabled", value, modifiedBy);

        public string GetAutoBackupFrequency() => GetString("AutoBackupFrequency", "Daily");
        public void SetAutoBackupFrequency(string value, string modifiedBy) => Set("AutoBackupFrequency", value, modifiedBy);

        public string GetAutoBackupTime() => GetString("AutoBackupTime", "23:00");
        public void SetAutoBackupTime(string value, string modifiedBy) => Set("AutoBackupTime", value, modifiedBy);

        public string GetAutoBackupDayOfWeek() => GetString("AutoBackupDayOfWeek", "Sunday");
        public void SetAutoBackupDayOfWeek(string value, string modifiedBy) => Set("AutoBackupDayOfWeek", value, modifiedBy);

        public int GetAutoBackupDayOfMonth() => GetInt("AutoBackupDayOfMonth", 1);
        public void SetAutoBackupDayOfMonth(int value, string modifiedBy) => SetInt("AutoBackupDayOfMonth", value, modifiedBy);

        public int GetBackupRetentionCount() => GetInt("BackupRetentionCount", 10);
        public void SetBackupRetentionCount(int value, string modifiedBy) => SetInt("BackupRetentionCount", value, modifiedBy);

        // ===== DATABASE SETTINGS =====

        public int GetLogRetentionDays() => GetInt("LogRetentionDays", 90);
        public void SetLogRetentionDays(int value, string modifiedBy) => SetInt("LogRetentionDays", value, modifiedBy);

        public bool GetEnableAuditLogs() => GetBool("EnableAuditLogs", true);
        public void SetEnableAuditLogs(bool value, string modifiedBy) => SetBool("EnableAuditLogs", value, modifiedBy);

        public int GetArchiveRecordsOlderThanYears() => GetInt("ArchiveRecordsOlderThanYears", 5);
        public void SetArchiveRecordsOlderThanYears(int value, string modifiedBy) => SetInt("ArchiveRecordsOlderThanYears", value, modifiedBy);

        // ===== APPLICATION SETTINGS =====

        public string GetApplicationVersion() => GetString("ApplicationVersion", "1.0.0");
        public DateTime? GetLastUpdated() => GetDateTime("LastUpdated");

        public bool GetDebugModeEnabled() => GetBool("DebugModeEnabled", false);
        public void SetDebugModeEnabled(bool value, string modifiedBy) => SetBool("DebugModeEnabled", value, modifiedBy);

        #endregion

        #region Reset to Defaults

        /// <summary>
        /// Reset all settings in a category to their default values
        /// </summary>
        public void ResetCategoryToDefaults(string category, string modifiedBy = null)
        {
            var defaults = GetDefaultSettings(category);

            foreach (var kvp in defaults)
            {
                Set(kvp.Key, kvp.Value, modifiedBy);
            }
        }

        /// <summary>
        /// Get default values for a category
        /// </summary>
        private Dictionary<string, string> GetDefaultSettings(string category)
        {
            var defaults = new Dictionary<string, string>();

            switch (category)
            {
                case "General":
                    defaults["ClinicName"] = "Rosal Medical Clinic";
                    defaults["ClinicAddress"] = "";
                    defaults["ClinicContactNumber"] = "";
                    defaults["DateFormat"] = "MM/dd/yyyy";
                    defaults["TimeFormat"] = "12";
                    defaults["Timezone"] = "Asia/Manila";
                    defaults["DefaultLandingPage"] = "Dashboard";
                    defaults["ItemsPerPage"] = "10";
                    defaults["AutoRefreshInterval"] = "0";
                    defaults["EnableSoundNotifications"] = "true";
                    break;

                case "Notification":
                    defaults["EnableInAppNotifications"] = "true";
                    defaults["EnableLowStockAlerts"] = "true";
                    defaults["LowStockThreshold"] = "50";
                    defaults["EnableExpiryAlerts"] = "true";
                    defaults["ExpiryAlertDays"] = "30";
                    defaults["EnableAppointmentReminders"] = "true";
                    defaults["AppointmentReminderHours"] = "1";
                    defaults["LowStockAlertRecipients"] = "Administrator,Doctor";
                    defaults["AppointmentReminderRecipients"] = "Doctor,Receptionist";
                    break;

                case "Security":
                    defaults["PasswordMinLength"] = "8";
                    defaults["PasswordRequireUppercase"] = "true";
                    defaults["PasswordRequireLowercase"] = "true";
                    defaults["PasswordRequireNumbers"] = "true";
                    defaults["PasswordRequireSpecial"] = "false";
                    defaults["PasswordExpiryDays"] = "0";
                    defaults["PasswordHistoryCount"] = "5";
                    defaults["ForcePasswordChangeOnFirstLogin"] = "true";
                    defaults["MaxFailedLoginAttempts"] = "3";
                    defaults["AccountLockoutMinutes"] = "5";
                    defaults["EnableRememberMe"] = "false";
                    defaults["RememberMeDays"] = "7";
                    defaults["SessionTimeoutMinutes"] = "30";
                    defaults["SessionWarningMinutes"] = "5";
                    defaults["AllowConcurrentSessions"] = "true";
                    defaults["ForceLogoutOtherSessions"] = "false";
                    break;

                case "Backup":
                    defaults["BackupLocation"] = @"C:\RosalHealthcare\Backups";
                    defaults["BackupCompression"] = "true";
                    defaults["BackupEncryption"] = "false";
                    defaults["BackupEncryptionPassword"] = "";
                    defaults["AutoBackupEnabled"] = "false";
                    defaults["AutoBackupFrequency"] = "Daily";
                    defaults["AutoBackupTime"] = "23:00";
                    defaults["AutoBackupDayOfWeek"] = "Sunday";
                    defaults["AutoBackupDayOfMonth"] = "1";
                    defaults["BackupRetentionCount"] = "10";
                    break;

                case "Database":
                    defaults["LogRetentionDays"] = "90";
                    defaults["EnableAuditLogs"] = "true";
                    defaults["ArchiveRecordsOlderThanYears"] = "5";
                    break;
            }

            return defaults;
        }

        #endregion
    }
}
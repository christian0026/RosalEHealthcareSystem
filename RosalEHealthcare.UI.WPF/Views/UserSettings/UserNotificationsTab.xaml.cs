using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserNotificationsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _isInitialized = false;
        private bool _pendingLoadSettings = false;

        public UserNotificationsTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _activityLogService = new ActivityLogService(_db);

            // Subscribe to Loaded event to ensure controls are ready
            Loaded += UserNotificationsTab_Loaded;
        }

        private void UserNotificationsTab_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;

            // If LoadSettings was called before we were loaded, do it now
            if (_pendingLoadSettings)
            {
                _pendingLoadSettings = false;
                LoadSettingsInternal();
            }
        }

        #region Load Settings

        public void LoadSettings()
        {
            // If controls aren't ready yet, defer the load
            if (!_isInitialized || ChkSoundAlerts == null)
            {
                System.Diagnostics.Debug.WriteLine("UserNotificationsTab: Deferring LoadSettings until controls are ready");
                _pendingLoadSettings = true;
                return;
            }

            LoadSettingsInternal();
        }

        private void LoadSettingsInternal()
        {
            _isLoading = true;

            try
            {
                // Double-check controls are available
                if (ChkEnableNotifications == null || ChkSoundAlerts == null)
                {
                    System.Diagnostics.Debug.WriteLine("UserNotificationsTab: Controls still null in LoadSettingsInternal");
                    return;
                }

                var user = SessionManager.CurrentUser;
                if (user == null)
                {
                    System.Diagnostics.Debug.WriteLine("UserNotificationsTab: No current user");
                    return;
                }

                string userId = user.Id.ToString();

                // General Notifications
                ChkEnableNotifications.IsChecked = GetBoolSetting(userId, "EnableNotifications", true);
                ChkSoundAlerts.IsChecked = GetBoolSetting(userId, "SoundAlerts", true);
                ChkDesktopNotifications.IsChecked = GetBoolSetting(userId, "DesktopNotifications", false);

                // Appointment Notifications
                ChkAppointmentReminders.IsChecked = GetBoolSetting(userId, "AppointmentReminders", true);
                ChkNewAppointmentAlerts.IsChecked = GetBoolSetting(userId, "NewAppointmentAlerts", true);
                ChkCancellationAlerts.IsChecked = GetBoolSetting(userId, "CancellationAlerts", true);

                // System Notifications
                ChkLowStockAlerts.IsChecked = GetBoolSetting(userId, "LowStockAlerts", true);
                ChkExpiringAlerts.IsChecked = GetBoolSetting(userId, "ExpiringAlerts", true);
                ChkSystemUpdates.IsChecked = GetBoolSetting(userId, "SystemUpdates", false);

                // Update UI based on master switch
                UpdateNotificationStates();

                System.Diagnostics.Debug.WriteLine("UserNotificationsTab: Settings loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notification settings: {ex.Message}");
                MessageBox.Show($"Error loading notification settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool GetBoolSetting(string userId, string key, bool defaultValue)
        {
            try
            {
                string value = _settingsService.GetUserSetting(userId, key, defaultValue.ToString());
                return bool.TryParse(value, out bool result) ? result : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        #endregion

        #region Event Handlers

        private void ChkEnableNotifications_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading && _isInitialized)
            {
                UpdateNotificationStates();
            }
        }

        private void UpdateNotificationStates()
        {
            // Safety check - ensure all controls exist
            if (ChkEnableNotifications == null || ChkSoundAlerts == null ||
                ChkDesktopNotifications == null || ChkAppointmentReminders == null ||
                ChkNewAppointmentAlerts == null || ChkCancellationAlerts == null ||
                ChkLowStockAlerts == null || ChkExpiringAlerts == null ||
                ChkSystemUpdates == null)
            {
                System.Diagnostics.Debug.WriteLine("UserNotificationsTab: Some controls are null in UpdateNotificationStates");
                return;
            }

            bool enabled = ChkEnableNotifications.IsChecked == true;

            // Enable/disable all other notification checkboxes
            ChkSoundAlerts.IsEnabled = enabled;
            ChkDesktopNotifications.IsEnabled = enabled;
            ChkAppointmentReminders.IsEnabled = enabled;
            ChkNewAppointmentAlerts.IsEnabled = enabled;
            ChkCancellationAlerts.IsEnabled = enabled;
            ChkLowStockAlerts.IsEnabled = enabled;
            ChkExpiringAlerts.IsEnabled = enabled;
            ChkSystemUpdates.IsEnabled = enabled;

            // Visual feedback
            double opacity = enabled ? 1.0 : 0.5;
            ChkSoundAlerts.Opacity = opacity;
            ChkDesktopNotifications.Opacity = opacity;
            ChkAppointmentReminders.Opacity = opacity;
            ChkNewAppointmentAlerts.Opacity = opacity;
            ChkCancellationAlerts.Opacity = opacity;
            ChkLowStockAlerts.Opacity = opacity;
            ChkExpiringAlerts.Opacity = opacity;
            ChkSystemUpdates.Opacity = opacity;
        }

        #endregion

        #region Save Settings

        private void BtnSaveNotifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null)
                {
                    MessageBox.Show("No user logged in.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string userId = user.Id.ToString();
                string modifiedBy = user.FullName;

                // Save General Notifications
                _settingsService.SaveUserSetting(userId, "EnableNotifications",
                    (ChkEnableNotifications.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "SoundAlerts",
                    (ChkSoundAlerts.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "DesktopNotifications",
                    (ChkDesktopNotifications.IsChecked == true).ToString(), modifiedBy);

                // Save Appointment Notifications
                _settingsService.SaveUserSetting(userId, "AppointmentReminders",
                    (ChkAppointmentReminders.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "NewAppointmentAlerts",
                    (ChkNewAppointmentAlerts.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "CancellationAlerts",
                    (ChkCancellationAlerts.IsChecked == true).ToString(), modifiedBy);

                // Save System Notifications
                _settingsService.SaveUserSetting(userId, "LowStockAlerts",
                    (ChkLowStockAlerts.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "ExpiringAlerts",
                    (ChkExpiringAlerts.IsChecked == true).ToString(), modifiedBy);
                _settingsService.SaveUserSetting(userId, "SystemUpdates",
                    (ChkSystemUpdates.IsChecked == true).ToString(), modifiedBy);

                // Update sound player setting
                NotificationSoundPlayer.SetSoundEnabled(ChkSoundAlerts.IsChecked == true);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated notification preferences",
                    module: "UserSettings",
                    performedBy: user.FullName,
                    performedByRole: user.Role
                );

                MessageBox.Show("Notification settings saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving notification settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all notification settings to their default values?",
                "Reset Notifications",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _isLoading = true;

                    // Reset to defaults
                    ChkEnableNotifications.IsChecked = true;
                    ChkSoundAlerts.IsChecked = true;
                    ChkDesktopNotifications.IsChecked = false;
                    ChkAppointmentReminders.IsChecked = true;
                    ChkNewAppointmentAlerts.IsChecked = true;
                    ChkCancellationAlerts.IsChecked = true;
                    ChkLowStockAlerts.IsChecked = true;
                    ChkExpiringAlerts.IsChecked = true;
                    ChkSystemUpdates.IsChecked = false;

                    _isLoading = false;
                    UpdateNotificationStates();

                    MessageBox.Show("Notification settings have been reset to defaults.\n\nClick 'Save Settings' to apply.",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _isLoading = false;
                    MessageBox.Show($"Error resetting notification settings: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
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

        public UserNotificationsTab()
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
                var user = SessionManager.CurrentUser;
                if (user == null) return;

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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notification settings: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private bool GetBoolSetting(string userId, string key, bool defaultValue)
        {
            string value = _settingsService.GetUserSetting(userId, key, defaultValue.ToString());
            return bool.TryParse(value, out bool result) ? result : defaultValue;
        }

        #endregion

        #region Event Handlers

        private void ChkEnableNotifications_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                UpdateNotificationStates();
            }
        }

        private void UpdateNotificationStates()
        {
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
            if (!enabled)
            {
                ChkSoundAlerts.Opacity = 0.5;
                ChkDesktopNotifications.Opacity = 0.5;
                ChkAppointmentReminders.Opacity = 0.5;
                ChkNewAppointmentAlerts.Opacity = 0.5;
                ChkCancellationAlerts.Opacity = 0.5;
                ChkLowStockAlerts.Opacity = 0.5;
                ChkExpiringAlerts.Opacity = 0.5;
                ChkSystemUpdates.Opacity = 0.5;
            }
            else
            {
                ChkSoundAlerts.Opacity = 1;
                ChkDesktopNotifications.Opacity = 1;
                ChkAppointmentReminders.Opacity = 1;
                ChkNewAppointmentAlerts.Opacity = 1;
                ChkCancellationAlerts.Opacity = 1;
                ChkLowStockAlerts.Opacity = 1;
                ChkExpiringAlerts.Opacity = 1;
                ChkSystemUpdates.Opacity = 1;
            }
        }

        #endregion

        #region Save Settings

        private void BtnSaveNotifications_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return;

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

                    UpdateNotificationStates();

                    MessageBox.Show("Notification settings have been reset to defaults.\n\nClick 'Save Settings' to apply.",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting notification settings: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
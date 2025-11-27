using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class NotificationSettingsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _hasChanges = false;

        public NotificationSettingsTab()
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
                // System Notifications
                ChkEnableNotifications.IsChecked = _settingsService.GetEnableInAppNotifications();

                // Low Stock Alerts
                ChkLowStockAlerts.IsChecked = _settingsService.GetEnableLowStockAlerts();
                SelectComboBoxItem(CmbLowStockThreshold, _settingsService.GetLowStockThreshold().ToString());
                UpdateLowStockPanelVisibility();

                // Expiry Alerts
                ChkExpiryAlerts.IsChecked = _settingsService.GetEnableExpiryAlerts();
                SelectComboBoxItem(CmbExpiryDays, _settingsService.GetExpiryAlertDays().ToString());
                UpdateExpiryPanelVisibility();

                // Appointment Reminders
                ChkAppointmentReminders.IsChecked = _settingsService.GetEnableAppointmentReminders();
                SelectComboBoxItem(CmbReminderHours, _settingsService.GetAppointmentReminderHours().ToString());
                UpdateReminderPanelVisibility();

                // Load Recipients
                LoadLowStockRecipients();
                LoadAppointmentRecipients();
                LoadExpiryRecipients();

                _hasChanges = false;
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

        private void LoadLowStockRecipients()
        {
            string recipients = _settingsService.GetLowStockAlertRecipients();
            var roles = recipients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(r => r.Trim())
                                  .ToList();

            ChkLowStockAdmin.IsChecked = roles.Contains("Administrator");
            ChkLowStockDoctor.IsChecked = roles.Contains("Doctor");
            ChkLowStockReceptionist.IsChecked = roles.Contains("Receptionist");
        }

        private void LoadAppointmentRecipients()
        {
            string recipients = _settingsService.GetAppointmentReminderRecipients();
            var roles = recipients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(r => r.Trim())
                                  .ToList();

            ChkAppointmentAdmin.IsChecked = roles.Contains("Administrator");
            ChkAppointmentDoctor.IsChecked = roles.Contains("Doctor");
            ChkAppointmentReceptionist.IsChecked = roles.Contains("Receptionist");
        }

        private void LoadExpiryRecipients()
        {
            // Using same setting as low stock for expiry - you can create a separate one if needed
            string recipients = _settingsService.GetLowStockAlertRecipients();
            var roles = recipients.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(r => r.Trim())
                                  .ToList();

            ChkExpiryAdmin.IsChecked = roles.Contains("Administrator");
            ChkExpiryDoctor.IsChecked = roles.Contains("Doctor");
            ChkExpiryReceptionist.IsChecked = roles.Contains("Receptionist");
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
            // Default to first item if not found
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

        #endregion

        #region Toggle Visibility

        private void ChkLowStockAlerts_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateLowStockPanelVisibility();
            }
        }

        private void ChkExpiryAlerts_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateExpiryPanelVisibility();
            }
        }

        private void ChkAppointmentReminders_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateReminderPanelVisibility();
            }
        }

        private void UpdateLowStockPanelVisibility()
        {
            if (LowStockThresholdPanel != null)
            {
                LowStockThresholdPanel.Visibility = ChkLowStockAlerts.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void UpdateExpiryPanelVisibility()
        {
            if (ExpiryDaysPanel != null)
            {
                ExpiryDaysPanel.Visibility = ChkExpiryAlerts.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void UpdateReminderPanelVisibility()
        {
            if (ReminderTimePanel != null)
            {
                ReminderTimePanel.Visibility = ChkAppointmentReminders.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        #endregion

        #region Save Settings

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                // Save System Notifications
                _settingsService.SetEnableInAppNotifications(ChkEnableNotifications.IsChecked ?? false, currentUser);

                // Save Low Stock Settings
                _settingsService.SetEnableLowStockAlerts(ChkLowStockAlerts.IsChecked ?? false, currentUser);
                _settingsService.SetLowStockThreshold(int.Parse(GetSelectedTag(CmbLowStockThreshold)), currentUser);

                // Save Expiry Settings
                _settingsService.SetEnableExpiryAlerts(ChkExpiryAlerts.IsChecked ?? false, currentUser);
                _settingsService.SetExpiryAlertDays(int.Parse(GetSelectedTag(CmbExpiryDays)), currentUser);

                // Save Appointment Reminder Settings
                _settingsService.SetEnableAppointmentReminders(ChkAppointmentReminders.IsChecked ?? false, currentUser);
                _settingsService.SetAppointmentReminderHours(int.Parse(GetSelectedTag(CmbReminderHours)), currentUser);

                // Save Recipients
                SaveLowStockRecipients(currentUser);
                SaveAppointmentRecipients(currentUser);

                // Update last updated
                _settingsService.Set("LastUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), currentUser);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated Notification Settings",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );

                _hasChanges = false;

                MessageBox.Show("Notification settings saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveLowStockRecipients(string modifiedBy)
        {
            var recipients = new System.Collections.Generic.List<string>();

            if (ChkLowStockAdmin.IsChecked == true) recipients.Add("Administrator");
            if (ChkLowStockDoctor.IsChecked == true) recipients.Add("Doctor");
            if (ChkLowStockReceptionist.IsChecked == true) recipients.Add("Receptionist");

            _settingsService.SetLowStockAlertRecipients(string.Join(",", recipients), modifiedBy);
        }

        private void SaveAppointmentRecipients(string modifiedBy)
        {
            var recipients = new System.Collections.Generic.List<string>();

            if (ChkAppointmentAdmin.IsChecked == true) recipients.Add("Administrator");
            if (ChkAppointmentDoctor.IsChecked == true) recipients.Add("Doctor");
            if (ChkAppointmentReceptionist.IsChecked == true) recipients.Add("Receptionist");

            _settingsService.SetAppointmentReminderRecipients(string.Join(",", recipients), modifiedBy);
        }

        #endregion

        #region Reset and Cancel

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all Notification settings to their default values?\n\nThis action cannot be undone.",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                    _settingsService.ResetCategoryToDefaults("Notification", currentUser);

                    // Log activity
                    _activityLogService.LogActivity(
                        activityType: "Reset",
                        description: "Reset Notification Settings to defaults",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    // Reload settings
                    LoadSettings();

                    MessageBox.Show("Settings have been reset to defaults.",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting settings: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_hasChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Are you sure you want to discard them?",
                    "Discard Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            // Reload original settings
            LoadSettings();
        }

        #endregion
    }
}
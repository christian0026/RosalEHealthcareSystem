using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class GeneralSettingsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _hasChanges = false;

        public GeneralSettingsTab()
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
                // Clinic Information
                TxtClinicName.Text = _settingsService.GetClinicName();
                TxtContactNumber.Text = _settingsService.GetClinicContactNumber();
                TxtAddress.Text = _settingsService.GetClinicAddress();

                // Regional Settings
                SelectComboBoxItem(CmbDateFormat, _settingsService.GetDateFormat());
                SelectComboBoxItem(CmbTimeFormat, _settingsService.GetTimeFormat());
                SelectComboBoxItem(CmbTimezone, _settingsService.GetTimezone());

                // System Preferences
                SelectComboBoxItem(CmbDefaultPage, _settingsService.GetDefaultLandingPage());
                SelectComboBoxItem(CmbItemsPerPage, _settingsService.GetItemsPerPage().ToString());
                SelectComboBoxItem(CmbAutoRefresh, _settingsService.GetAutoRefreshInterval().ToString());
                ChkSoundNotifications.IsChecked = _settingsService.GetEnableSoundNotifications();

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

        #region Save Settings

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                // Save Clinic Information
                _settingsService.SetClinicName(TxtClinicName.Text.Trim(), currentUser);
                _settingsService.SetClinicContactNumber(TxtContactNumber.Text.Trim(), currentUser);
                _settingsService.SetClinicAddress(TxtAddress.Text.Trim(), currentUser);

                // Save Regional Settings
                _settingsService.SetDateFormat(GetSelectedTag(CmbDateFormat), currentUser);
                _settingsService.SetTimeFormat(GetSelectedTag(CmbTimeFormat), currentUser);
                _settingsService.SetTimezone(GetSelectedTag(CmbTimezone), currentUser);

                // Save System Preferences
                _settingsService.SetDefaultLandingPage(GetSelectedTag(CmbDefaultPage), currentUser);
                _settingsService.SetItemsPerPage(int.Parse(GetSelectedTag(CmbItemsPerPage)), currentUser);
                _settingsService.SetAutoRefreshInterval(int.Parse(GetSelectedTag(CmbAutoRefresh)), currentUser);
                _settingsService.SetEnableSoundNotifications(ChkSoundNotifications.IsChecked ?? false, currentUser);

                // Update last updated
                _settingsService.Set("LastUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), currentUser);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated General Settings",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );

                _hasChanges = false;

                MessageBox.Show("Settings saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Reset and Cancel

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all General settings to their default values?\n\nThis action cannot be undone.",
                "Reset to Defaults",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                    _settingsService.ResetCategoryToDefaults("General", currentUser);

                    // Log activity
                    _activityLogService.LogActivity(
                        activityType: "Reset",
                        description: "Reset General Settings to defaults",
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
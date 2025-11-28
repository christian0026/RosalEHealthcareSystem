using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserPreferencesTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;

        public UserPreferencesTab()
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

                // Display Preferences
                SelectComboBoxByTag(CmbItemsPerPage,
                    _settingsService.GetUserSetting(userId, "ItemsPerPage", "20"));
                SelectComboBoxByTag(CmbDateFormat,
                    _settingsService.GetUserSetting(userId, "DateFormat", "MM/dd/yyyy"));
                SelectComboBoxByTag(CmbTimeFormat,
                    _settingsService.GetUserSetting(userId, "TimeFormat", "h:mm tt"));
                SelectComboBoxByTag(CmbDefaultPage,
                    _settingsService.GetUserSetting(userId, "DefaultPage", "Dashboard"));

                // Data Display
                SelectComboBoxByTag(CmbSortOrder,
                    _settingsService.GetUserSetting(userId, "SortOrder", "DESC"));
                SelectComboBoxByTag(CmbAutoRefresh,
                    _settingsService.GetUserSetting(userId, "AutoRefresh", "0"));

                // Accessibility
                SelectComboBoxByTag(CmbFontSize,
                    _settingsService.GetUserSetting(userId, "FontSize", "Medium"));
                SelectComboBoxByTag(CmbAnimations,
                    _settingsService.GetUserSetting(userId, "Animations", "True"));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preferences: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SelectComboBoxByTag(ComboBox comboBox, string tagValue)
        {
            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
            // If not found, select first item
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private string GetComboBoxSelectedTag(ComboBox comboBox)
        {
            if (comboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                return item.Tag.ToString();
            }
            return "";
        }

        #endregion

        #region Save Settings

        private void BtnSavePreferences_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return;

                string userId = user.Id.ToString();
                string modifiedBy = user.FullName;

                // Save Display Preferences
                _settingsService.SaveUserSetting(userId, "ItemsPerPage",
                    GetComboBoxSelectedTag(CmbItemsPerPage), modifiedBy);
                _settingsService.SaveUserSetting(userId, "DateFormat",
                    GetComboBoxSelectedTag(CmbDateFormat), modifiedBy);
                _settingsService.SaveUserSetting(userId, "TimeFormat",
                    GetComboBoxSelectedTag(CmbTimeFormat), modifiedBy);
                _settingsService.SaveUserSetting(userId, "DefaultPage",
                    GetComboBoxSelectedTag(CmbDefaultPage), modifiedBy);

                // Save Data Display
                _settingsService.SaveUserSetting(userId, "SortOrder",
                    GetComboBoxSelectedTag(CmbSortOrder), modifiedBy);
                _settingsService.SaveUserSetting(userId, "AutoRefresh",
                    GetComboBoxSelectedTag(CmbAutoRefresh), modifiedBy);

                // Save Accessibility
                _settingsService.SaveUserSetting(userId, "FontSize",
                    GetComboBoxSelectedTag(CmbFontSize), modifiedBy);
                _settingsService.SaveUserSetting(userId, "Animations",
                    GetComboBoxSelectedTag(CmbAnimations), modifiedBy);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated user preferences",
                    module: "UserSettings",
                    performedBy: user.FullName,
                    performedByRole: user.Role
                );

                MessageBox.Show("Preferences saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving preferences: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset all preferences to their default values?",
                "Reset Preferences",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Reset to defaults
                    SelectComboBoxByTag(CmbItemsPerPage, "20");
                    SelectComboBoxByTag(CmbDateFormat, "MM/dd/yyyy");
                    SelectComboBoxByTag(CmbTimeFormat, "h:mm tt");
                    SelectComboBoxByTag(CmbDefaultPage, "Dashboard");
                    SelectComboBoxByTag(CmbSortOrder, "DESC");
                    SelectComboBoxByTag(CmbAutoRefresh, "0");
                    SelectComboBoxByTag(CmbFontSize, "Medium");
                    SelectComboBoxByTag(CmbAnimations, "True");

                    MessageBox.Show("Preferences have been reset to defaults.\n\nClick 'Save Preferences' to apply.",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting preferences: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion
    }
}
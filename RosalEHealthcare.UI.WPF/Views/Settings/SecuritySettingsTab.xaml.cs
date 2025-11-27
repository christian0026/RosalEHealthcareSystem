using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class SecuritySettingsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly RolePermissionService _rolePermissionService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _hasChanges = false;
        private string _currentSubTab = "PasswordPolicy";
        private Button _activeSubTabButton;

        private List<RolePermission> _currentRolePermissions;
        private string _selectedRole = "Administrator";

        public SecuritySettingsTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _rolePermissionService = new RolePermissionService(_db);
            _activityLogService = new ActivityLogService(_db);

            _activeSubTabButton = BtnPasswordPolicy;
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;

            try
            {
                LoadPasswordPolicySettings();
                LoadLoginSecuritySettings();
                LoadSessionSecuritySettings();
                LoadRBACSettings();

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

        private void LoadPasswordPolicySettings()
        {
            // Password Requirements
            SelectComboBoxItem(CmbPasswordLength, _settingsService.GetPasswordMinLength().ToString());
            ChkRequireUppercase.IsChecked = _settingsService.GetPasswordRequireUppercase();
            ChkRequireLowercase.IsChecked = _settingsService.GetPasswordRequireLowercase();
            ChkRequireNumbers.IsChecked = _settingsService.GetPasswordRequireNumbers();
            ChkRequireSpecial.IsChecked = _settingsService.GetPasswordRequireSpecial();

            // Password Expiry & History
            SelectComboBoxItem(CmbPasswordExpiry, _settingsService.GetPasswordExpiryDays().ToString());
            SelectComboBoxItem(CmbPasswordHistory, _settingsService.GetPasswordHistoryCount().ToString());
            ChkForceChangeFirstLogin.IsChecked = _settingsService.GetForcePasswordChangeOnFirstLogin();
        }

        private void LoadLoginSecuritySettings()
        {
            // Account Lockout
            SelectComboBoxItem(CmbMaxFailedAttempts, _settingsService.GetMaxFailedLoginAttempts().ToString());
            SelectComboBoxItem(CmbLockoutDuration, _settingsService.GetAccountLockoutMinutes().ToString());

            // Remember Me
            ChkEnableRememberMe.IsChecked = _settingsService.GetEnableRememberMe();
            SelectComboBoxItem(CmbRememberMeDays, _settingsService.GetRememberMeDays().ToString());
            UpdateRememberMePanelVisibility();
        }

        private void LoadSessionSecuritySettings()
        {
            // Session Timeout
            SelectComboBoxItem(CmbSessionTimeout, _settingsService.GetSessionTimeoutMinutes().ToString());
            SelectComboBoxItem(CmbSessionWarning, _settingsService.GetSessionWarningMinutes().ToString());

            // Concurrent Sessions
            ChkAllowConcurrentSessions.IsChecked = _settingsService.GetAllowConcurrentSessions();
            ChkForceLogoutOthers.IsChecked = _settingsService.GetForceLogoutOtherSessions();
        }

        private void LoadRBACSettings()
        {
            // Default to Administrator
            CmbRoleSelector.SelectedIndex = 0;
            LoadRolePermissions("Administrator");
        }

        private void LoadRolePermissions(string roleName)
        {
            _selectedRole = roleName;
            _currentRolePermissions = _rolePermissionService.GetByRole(roleName).ToList();

            // Ensure all modules are present
            foreach (var module in RolePermissionService.Modules)
            {
                if (!_currentRolePermissions.Any(p => p.Module == module))
                {
                    _currentRolePermissions.Add(new RolePermission
                    {
                        RoleName = roleName,
                        Module = module,
                        CanView = false,
                        CanCreate = false,
                        CanEdit = false,
                        CanDelete = false,
                        CanExport = false
                    });
                }
            }

            // Sort by module order
            _currentRolePermissions = _currentRolePermissions
                .OrderBy(p => Array.IndexOf(RolePermissionService.Modules, p.Module))
                .ToList();

            PermissionsItemsControl.ItemsSource = _currentRolePermissions;
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

        #endregion

        #region Sub-Tab Navigation

        private void SubTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                SetActiveSubTab(button);
                ShowSubTabPanel(tabName);
            }
        }

        private void SetActiveSubTab(Button button)
        {
            // Reset all buttons
            BtnPasswordPolicy.Style = (Style)FindResource("SubTabButton");
            BtnLoginSecurity.Style = (Style)FindResource("SubTabButton");
            BtnSessionSecurity.Style = (Style)FindResource("SubTabButton");
            BtnRBAC.Style = (Style)FindResource("SubTabButton");

            // Set active button
            button.Style = (Style)FindResource("SubTabButtonActive");
            _activeSubTabButton = button;
        }

        private void ShowSubTabPanel(string tabName)
        {
            _currentSubTab = tabName;

            PasswordPolicyPanel.Visibility = Visibility.Collapsed;
            LoginSecurityPanel.Visibility = Visibility.Collapsed;
            SessionSecurityPanel.Visibility = Visibility.Collapsed;
            RBACPanel.Visibility = Visibility.Collapsed;

            switch (tabName)
            {
                case "PasswordPolicy":
                    PasswordPolicyPanel.Visibility = Visibility.Visible;
                    break;
                case "LoginSecurity":
                    LoginSecurityPanel.Visibility = Visibility.Visible;
                    break;
                case "SessionSecurity":
                    SessionSecurityPanel.Visibility = Visibility.Visible;
                    break;
                case "RBAC":
                    RBACPanel.Visibility = Visibility.Visible;
                    break;
            }
        }

        #endregion

        #region Event Handlers

        private void ChkEnableRememberMe_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isLoading)
            {
                _hasChanges = true;
                UpdateRememberMePanelVisibility();
            }
        }

        private void UpdateRememberMePanelVisibility()
        {
            if (RememberMeDurationPanel != null)
            {
                RememberMeDurationPanel.Visibility = ChkEnableRememberMe.IsChecked == true
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void CmbRoleSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading && CmbRoleSelector.SelectedItem is ComboBoxItem item)
            {
                string roleName = item.Tag?.ToString() ?? "Administrator";
                LoadRolePermissions(roleName);
            }
        }

        #endregion

        #region RBAC Quick Actions

        private void BtnGrantAll_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRolePermissions == null) return;

            foreach (var perm in _currentRolePermissions)
            {
                perm.CanView = true;
                perm.CanCreate = true;
                perm.CanEdit = true;
                perm.CanDelete = true;
                perm.CanExport = true;
            }

            PermissionsItemsControl.ItemsSource = null;
            PermissionsItemsControl.ItemsSource = _currentRolePermissions;
            _hasChanges = true;
        }

        private void BtnRevokeAll_Click(object sender, RoutedEventArgs e)
        {
            if (_currentRolePermissions == null) return;

            // Don't allow revoking all for Administrator
            if (_selectedRole == "Administrator")
            {
                MessageBox.Show("Cannot revoke all permissions for Administrator role.",
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var perm in _currentRolePermissions)
            {
                perm.CanView = false;
                perm.CanCreate = false;
                perm.CanEdit = false;
                perm.CanDelete = false;
                perm.CanExport = false;
            }

            PermissionsItemsControl.ItemsSource = null;
            PermissionsItemsControl.ItemsSource = _currentRolePermissions;
            _hasChanges = true;
        }

        private void BtnResetRoleDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to reset permissions for '{_selectedRole}' to default values?",
                "Reset Role Permissions",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                    _rolePermissionService.ResetRoleToDefault(_selectedRole, currentUser);
                    LoadRolePermissions(_selectedRole);

                    MessageBox.Show($"Permissions for '{_selectedRole}' have been reset to defaults.",
                        "Reset Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting permissions: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Save Settings

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                // Save Password Policy
                SavePasswordPolicySettings(currentUser);

                // Save Login Security
                SaveLoginSecuritySettings(currentUser);

                // Save Session Security
                SaveSessionSecuritySettings(currentUser);

                // Save RBAC
                SaveRBACSettings(currentUser);

                // Update last updated
                _settingsService.Set("LastUpdated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), currentUser);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated Security Settings",
                    module: "SystemSettings",
                    performedBy: currentUser,
                    performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                );

                _hasChanges = false;

                MessageBox.Show("Security settings saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePasswordPolicySettings(string modifiedBy)
        {
            _settingsService.SetPasswordMinLength(int.Parse(GetSelectedTag(CmbPasswordLength)), modifiedBy);
            _settingsService.SetPasswordRequireUppercase(ChkRequireUppercase.IsChecked ?? false, modifiedBy);
            _settingsService.SetPasswordRequireLowercase(ChkRequireLowercase.IsChecked ?? false, modifiedBy);
            _settingsService.SetPasswordRequireNumbers(ChkRequireNumbers.IsChecked ?? false, modifiedBy);
            _settingsService.SetPasswordRequireSpecial(ChkRequireSpecial.IsChecked ?? false, modifiedBy);
            _settingsService.SetPasswordExpiryDays(int.Parse(GetSelectedTag(CmbPasswordExpiry)), modifiedBy);
            _settingsService.SetPasswordHistoryCount(int.Parse(GetSelectedTag(CmbPasswordHistory)), modifiedBy);
            _settingsService.SetForcePasswordChangeOnFirstLogin(ChkForceChangeFirstLogin.IsChecked ?? false, modifiedBy);
        }

        private void SaveLoginSecuritySettings(string modifiedBy)
        {
            _settingsService.SetMaxFailedLoginAttempts(int.Parse(GetSelectedTag(CmbMaxFailedAttempts)), modifiedBy);
            _settingsService.SetAccountLockoutMinutes(int.Parse(GetSelectedTag(CmbLockoutDuration)), modifiedBy);
            _settingsService.SetEnableRememberMe(ChkEnableRememberMe.IsChecked ?? false, modifiedBy);
            _settingsService.SetRememberMeDays(int.Parse(GetSelectedTag(CmbRememberMeDays)), modifiedBy);
        }

        private void SaveSessionSecuritySettings(string modifiedBy)
        {
            _settingsService.SetSessionTimeoutMinutes(int.Parse(GetSelectedTag(CmbSessionTimeout)), modifiedBy);
            _settingsService.SetSessionWarningMinutes(int.Parse(GetSelectedTag(CmbSessionWarning)), modifiedBy);
            _settingsService.SetAllowConcurrentSessions(ChkAllowConcurrentSessions.IsChecked ?? false, modifiedBy);
            _settingsService.SetForceLogoutOtherSessions(ChkForceLogoutOthers.IsChecked ?? false, modifiedBy);
        }

        private void SaveRBACSettings(string modifiedBy)
        {
            if (_currentRolePermissions != null)
            {
                foreach (var perm in _currentRolePermissions)
                {
                    _rolePermissionService.UpdatePermission(
                        perm.RoleName,
                        perm.Module,
                        perm.CanView,
                        perm.CanCreate,
                        perm.CanEdit,
                        perm.CanDelete,
                        perm.CanExport,
                        modifiedBy
                    );
                }
            }
        }

        #endregion

        #region Reset and Cancel

        private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset ALL Security settings to their default values?\n\nThis includes Password Policy, Login Security, Session Security, and RBAC.\n\nThis action cannot be undone.",
                "Reset All Security Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";

                    // Reset security settings
                    _settingsService.ResetCategoryToDefaults("Security", currentUser);

                    // Reset all role permissions
                    _rolePermissionService.ResetAllToDefaults(currentUser);

                    // Log activity
                    _activityLogService.LogActivity(
                        activityType: "Reset",
                        description: "Reset all Security Settings to defaults",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    // Reload settings
                    LoadSettings();

                    MessageBox.Show("All security settings have been reset to defaults.",
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
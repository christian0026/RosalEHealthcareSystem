using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using RosalEHealthcare.UI.WPF.Views.Settings;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class SystemSettingsView : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        private Button _activeTabButton;
        private string _currentTab = "General";

        // Tab content controls (lazy loaded)
        private GeneralSettingsTab _generalSettingsTab;
        private NotificationSettingsTab _notificationSettingsTab;
        private SecuritySettingsTab _securitySettingsTab;
        private BackupRestoreTab _backupRestoreTab;
        private DatabaseSettingsTab _databaseSettingsTab;
        private SystemLogsTab _systemLogsTab;
        private ApplicationSettingsTab _applicationSettingsTab;

        public SystemSettingsView()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _activityLogService = new ActivityLogService(_db);

            Loaded += SystemSettingsView_Loaded;
        }

        private void SystemSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial active tab
            _activeTabButton = BtnGeneralSettings;
            LoadTab("General");

            // Log activity
            LogActivity("Viewed System Settings");
        }

        #region Tab Navigation

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                // Update active button style
                SetActiveTab(button);

                // Load the tab content
                LoadTab(tabName);
            }
        }

        private void SetActiveTab(Button button)
        {
            // Reset previous active button
            if (_activeTabButton != null)
            {
                _activeTabButton.Style = (Style)FindResource("TabButton");
            }

            // Set new active button
            button.Style = (Style)FindResource("TabButtonActive");
            _activeTabButton = button;
        }

        private void LoadTab(string tabName)
        {
            _currentTab = tabName;
            ShowLoading(true, $"Loading {tabName} settings...");

            try
            {
                switch (tabName)
                {
                    case "General":
                        TxtContentTitle.Text = "General Settings";
                        TxtContentSubtitle.Text = "Configure clinic information, regional settings, and system preferences";
                        SetActiveTab(BtnGeneralSettings);

                        if (_generalSettingsTab == null)
                            _generalSettingsTab = new GeneralSettingsTab();
                        TabContent.Content = _generalSettingsTab;
                        _generalSettingsTab.LoadSettings();
                        break;

                    case "Notification":
                        TxtContentTitle.Text = "Notification Settings";
                        TxtContentSubtitle.Text = "Configure alerts, reminders, and notification preferences";
                        SetActiveTab(BtnNotificationSettings);

                        if (_notificationSettingsTab == null)
                            _notificationSettingsTab = new NotificationSettingsTab();
                        TabContent.Content = _notificationSettingsTab;
                        _notificationSettingsTab.LoadSettings();
                        break;

                    case "Security":
                        TxtContentTitle.Text = "Security & Access Control";
                        TxtContentSubtitle.Text = "Configure password policies, login security, and role-based permissions";
                        SetActiveTab(BtnSecuritySettings);

                        if (_securitySettingsTab == null)
                            _securitySettingsTab = new SecuritySettingsTab();
                        TabContent.Content = _securitySettingsTab;
                        _securitySettingsTab.LoadSettings();
                        break;

                    case "Backup":
                        TxtContentTitle.Text = "Backup & Restore";
                        TxtContentSubtitle.Text = "Manage database backups, restore points, and automatic backup schedules";
                        SetActiveTab(BtnBackupRestore);

                        if (_backupRestoreTab == null)
                            _backupRestoreTab = new BackupRestoreTab();
                        TabContent.Content = _backupRestoreTab;
                        _backupRestoreTab.LoadSettings();
                        break;

                    case "Database":
                        TxtContentTitle.Text = "Database Settings";
                        TxtContentSubtitle.Text = "View database information, perform maintenance, and manage data";
                        SetActiveTab(BtnDatabaseSettings);

                        if (_databaseSettingsTab == null)
                            _databaseSettingsTab = new DatabaseSettingsTab();
                        TabContent.Content = _databaseSettingsTab;
                        _databaseSettingsTab.LoadSettings();
                        break;

                    case "Logs":
                        TxtContentTitle.Text = "System Logs";
                        TxtContentSubtitle.Text = "View activity logs, login history, and audit trail";
                        SetActiveTab(BtnSystemLogs);

                        if (_systemLogsTab == null)
                            _systemLogsTab = new SystemLogsTab();
                        TabContent.Content = _systemLogsTab;
                        _systemLogsTab.LoadSettings();
                        break;

                    case "Application":
                        TxtContentTitle.Text = "Application Information";
                        TxtContentSubtitle.Text = "View version info, system requirements, and developer options";
                        SetActiveTab(BtnApplicationSettings);

                        if (_applicationSettingsTab == null)
                            _applicationSettingsTab = new ApplicationSettingsTab();
                        TabContent.Content = _applicationSettingsTab;
                        _applicationSettingsTab.LoadSettings();
                        break;
                }

                UpdateLastModified();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading {tabName} settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        #endregion

        #region UI Helpers

        private void ShowLoading(bool show, string message = "Loading...")
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            TxtLoadingMessage.Text = message;
        }

        private void UpdateLastModified()
        {
            try
            {
                var lastModified = _settingsService.GetLastUpdated();
                if (lastModified.HasValue)
                {
                    TxtLastModified.Text = $"Last modified: {lastModified.Value:MMM dd, yyyy HH:mm}";
                }
                else
                {
                    TxtLastModified.Text = "Last modified: --";
                }
            }
            catch
            {
                TxtLastModified.Text = "Last modified: --";
            }
        }

        #endregion

        #region Activity Logging

        private void LogActivity(string description)
        {
            try
            {
                var currentUser = SessionManager.CurrentUser;
                if (currentUser != null)
                {
                    _activityLogService.LogActivity(
                        activityType: "View",
                        description: description,
                        module: "SystemSettings",
                        performedBy: currentUser.FullName,
                        performedByRole: currentUser.Role
                    );
                }
            }
            catch { /* Ignore logging errors */ }
        }

        #endregion

        #region Public Methods (for refreshing from child tabs)

        /// <summary>
        /// Refresh the current tab
        /// </summary>
        public void RefreshCurrentTab()
        {
            LoadTab(_currentTab);
        }

        /// <summary>
        /// Navigate to a specific tab
        /// </summary>
        public void NavigateToTab(string tabName)
        {
            LoadTab(tabName);
        }

        #endregion
    }
}
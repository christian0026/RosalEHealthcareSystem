using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserSystemSettingsView : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly SystemSettingsService _settingsService;
        private readonly ActivityLogService _activityLogService;

        // Tab instances (lazy loading)
        private UserProfileTab _profileTab;
        private UserSecurityTab _securityTab;
        private UserPreferencesTab _preferencesTab;
        private UserNotificationsTab _notificationsTab;
        private UserAboutTab _aboutTab;

        private string _currentTab = "MyProfile";

        public UserSystemSettingsView()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _settingsService = new SystemSettingsService(_db);
            _activityLogService = new ActivityLogService(_db);

            Loaded += UserSystemSettingsView_Loaded;
        }

        private void UserSystemSettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadUserInfo();
            LoadTab("MyProfile");
            UpdateLastModified();
        }

        #region User Info

        private void LoadUserInfo()
        {
            var user = SessionManager.CurrentUser;
            if (user != null)
            {
                TxtCurrentUser.Text = user.FullName ?? user.Email ?? "User";
                TxtCurrentRole.Text = user.Role ?? "User";
                TxtUserInitials.Text = GetInitials(user.FullName ?? user.Email ?? "U");
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";

            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();

            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        #endregion

        #region Tab Navigation

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string tabName)
            {
                SetActiveTab(button);
                LoadTab(tabName);
            }
        }

        private void SetActiveTab(Button activeButton)
        {
            // Reset all buttons to default style
            BtnMyProfile.Style = (Style)FindResource("SettingsTabButton");
            BtnSecurity.Style = (Style)FindResource("SettingsTabButton");
            BtnPreferences.Style = (Style)FindResource("SettingsTabButton");
            BtnNotifications.Style = (Style)FindResource("SettingsTabButton");
            BtnAbout.Style = (Style)FindResource("SettingsTabButton");

            // Set active button style
            activeButton.Style = (Style)FindResource("SettingsTabButtonActive");
        }

        private void LoadTab(string tabName)
        {
            _currentTab = tabName;
            ShowLoading(true);

            try
            {
                switch (tabName)
                {
                    case "MyProfile":
                        TxtTabTitle.Text = "My Profile";
                        TxtTabSubtitle.Text = "View and manage your profile information";
                        if (_profileTab == null)
                            _profileTab = new UserProfileTab();
                        _profileTab.LoadSettings();
                        TabContent.Content = _profileTab;
                        break;

                    case "Security":
                        TxtTabTitle.Text = "Security";
                        TxtTabSubtitle.Text = "Manage your password and view login activity";
                        if (_securityTab == null)
                            _securityTab = new UserSecurityTab();
                        _securityTab.LoadSettings();
                        TabContent.Content = _securityTab;
                        break;

                    case "Preferences":
                        TxtTabTitle.Text = "Preferences";
                        TxtTabSubtitle.Text = "Customize your experience";
                        if (_preferencesTab == null)
                            _preferencesTab = new UserPreferencesTab();
                        _preferencesTab.LoadSettings();
                        TabContent.Content = _preferencesTab;
                        break;

                    case "Notifications":
                        TxtTabTitle.Text = "Notifications";
                        TxtTabSubtitle.Text = "Configure your notification preferences";
                        if (_notificationsTab == null)
                            _notificationsTab = new UserNotificationsTab();
                        _notificationsTab.LoadSettings();
                        TabContent.Content = _notificationsTab;
                        break;

                    case "About":
                        TxtTabTitle.Text = "About";
                        TxtTabSubtitle.Text = "Application information and help";
                        if (_aboutTab == null)
                            _aboutTab = new UserAboutTab();
                        _aboutTab.LoadSettings();
                        TabContent.Content = _aboutTab;
                        break;
                }

                LogActivity($"Viewed {tabName} settings");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tab: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        #endregion

        #region Helpers

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateLastModified()
        {
            try
            {
                var lastModified = _settingsService.GetLastModifiedDate();
                if (lastModified.HasValue)
                {
                    TxtLastModified.Text = $"Last updated: {lastModified.Value:MMM dd, yyyy 'at' h:mm tt}";
                }
                else
                {
                    TxtLastModified.Text = "Last updated: Never";
                }
            }
            catch
            {
                TxtLastModified.Text = "Last updated: Unknown";
            }
        }

        private void LogActivity(string description)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                _activityLogService.LogActivity(
                    activityType: "View",
                    description: description,
                    module: "UserSettings",
                    performedBy: user?.FullName ?? "System",
                    performedByRole: user?.Role ?? "User"
                );
            }
            catch { /* Ignore logging errors */ }
        }

        public void RefreshCurrentTab()
        {
            LoadTab(_currentTab);
        }

        public void NavigateToTab(string tabName)
        {
            switch (tabName)
            {
                case "MyProfile":
                    SetActiveTab(BtnMyProfile);
                    break;
                case "Security":
                    SetActiveTab(BtnSecurity);
                    break;
                case "Preferences":
                    SetActiveTab(BtnPreferences);
                    break;
                case "Notifications":
                    SetActiveTab(BtnNotifications);
                    break;
                case "About":
                    SetActiveTab(BtnAbout);
                    break;
            }
            LoadTab(tabName);
        }

        #endregion
    }
}
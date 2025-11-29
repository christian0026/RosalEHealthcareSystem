using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views.Settings
{
    public partial class SystemLogsTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly ActivityLogService _activityLogService;
        private readonly LoginHistoryService _loginHistoryService;

        private bool _isLoading = false;
        private string _currentSubTab = "ActivityLogs";

        // Pagination for Activity Logs
        private int _activityCurrentPage = 1;
        private int _activityPageSize = 20;
        private int _activityTotalCount = 0;

        // Pagination for Login History
        private int _loginCurrentPage = 1;
        private int _loginPageSize = 20;
        private int _loginTotalCount = 0;

        public SystemLogsTab()
        {
            InitializeComponent();

            // Initialize services safely
            try
            {
                _db = new RosalEHealthcareDbContext();
                _activityLogService = new ActivityLogService(_db);
                _loginHistoryService = new LoginHistoryService(_db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database connection failed: {ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;

            try
            {
                // CRITICAL FIX: Check ALL UI elements are initialized
                if (TxtTodayActivities == null || TxtTodayLogins == null)
                {
                    System.Diagnostics.Debug.WriteLine("UI elements not initialized yet - deferring load");
                    // Defer loading until UI is ready
                    this.Loaded += SystemLogsTab_Loaded;
                    return;
                }

                // Ensure services exist
                if (_activityLogService == null || _loginHistoryService == null) return;

                LoadActivityStats();
                LoadActivityLogs();

                // Only load other tabs if they are visible or we want to preload them
                // To prevent null reference on collapsed elements in some WPF versions
                LoadLoginStats();
                LoadLoginHistory();
                LoadActiveSessions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Full error: {ex}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SystemLogsTab_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= SystemLogsTab_Loaded; // Unsubscribe
            LoadSettings();
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
            // Reset styles
            if (BtnActivityLogs != null) BtnActivityLogs.Style = (Style)FindResource("SubTabButton");
            if (BtnLoginHistory != null) BtnLoginHistory.Style = (Style)FindResource("SubTabButton");
            if (BtnActiveSessions != null) BtnActiveSessions.Style = (Style)FindResource("SubTabButton");

            // Set active style
            button.Style = (Style)FindResource("SubTabButtonActive");
        }

        private void ShowSubTabPanel(string tabName)
        {
            _currentSubTab = tabName;

            if (ActivityLogsPanel != null) ActivityLogsPanel.Visibility = Visibility.Collapsed;
            if (LoginHistoryPanel != null) LoginHistoryPanel.Visibility = Visibility.Collapsed;
            if (ActiveSessionsPanel != null) ActiveSessionsPanel.Visibility = Visibility.Collapsed;

            switch (tabName)
            {
                case "ActivityLogs":
                    if (ActivityLogsPanel != null) ActivityLogsPanel.Visibility = Visibility.Visible;
                    LoadActivityLogs();
                    break;
                case "LoginHistory":
                    if (LoginHistoryPanel != null) LoginHistoryPanel.Visibility = Visibility.Visible;
                    LoadLoginHistory();
                    break;
                case "ActiveSessions":
                    if (ActiveSessionsPanel != null) ActiveSessionsPanel.Visibility = Visibility.Visible;
                    LoadActiveSessions();
                    break;
            }
        }

        #endregion

        #region Activity Logs

        private void LoadActivityStats()
        {
            try
            {
                if (_activityLogService == null) return;

                // Null Check UI Elements before using
                if (TxtTodayActivities == null || TxtWeekActivities == null ||
                    TxtMonthActivities == null || TxtTotalActivities == null) return;

                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                // Get counts directly from DB for performance (assuming Service supports it, or fetch simplified list)
                var allLogs = _activityLogService.GetRecentActivities(1000).ToList();

                TxtTodayActivities.Text = allLogs.Count(l => l.PerformedAt.Date == today).ToString();
                TxtWeekActivities.Text = allLogs.Count(l => l.PerformedAt >= weekStart).ToString();
                TxtMonthActivities.Text = allLogs.Count(l => l.PerformedAt >= monthStart).ToString();
                TxtTotalActivities.Text = _activityLogService.GetTotalCount().ToString(); // Use count method if available
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in LoadActivityStats: {ex.Message}");
            }
        }

        private void LoadActivityLogs()
        {
            try
            {
                if (_activityLogService == null || ActivityLogsList == null) return;

                string searchText = TxtActivitySearch?.Text?.Trim() ?? "";
                string module = GetSelectedTag(CmbActivityModule);
                string activityType = GetSelectedTag(CmbActivityType);

                var allLogs = _activityLogService.GetRecentActivities(1000).AsQueryable();

                if (!string.IsNullOrEmpty(searchText))
                {
                    allLogs = allLogs.Where(l => (l.Description != null && l.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                                 (l.PerformedBy != null && l.PerformedBy.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
                }

                if (!string.IsNullOrEmpty(module)) allLogs = allLogs.Where(l => l.Module == module);
                if (!string.IsNullOrEmpty(activityType)) allLogs = allLogs.Where(l => l.ActivityType == activityType);

                _activityTotalCount = allLogs.Count();

                var logs = allLogs.OrderByDescending(l => l.PerformedAt)
                                  .Skip((_activityCurrentPage - 1) * _activityPageSize)
                                  .Take(_activityPageSize)
                                  .ToList();

                if (logs.Any())
                {
                    ActivityLogsList.ItemsSource = logs;
                    ActivityLogsList.Visibility = Visibility.Visible;
                    if (EmptyActivityPanel != null) EmptyActivityPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActivityLogsList.ItemsSource = null;
                    ActivityLogsList.Visibility = Visibility.Collapsed;
                    if (EmptyActivityPanel != null) EmptyActivityPanel.Visibility = Visibility.Visible;
                }

                UpdateActivityPagination();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading activity logs: {ex.Message}");
            }
        }

        private void UpdateActivityPagination()
        {
            if (TxtActivityPagination == null) return;

            int startRecord = _activityTotalCount == 0 ? 0 : ((_activityCurrentPage - 1) * _activityPageSize) + 1;
            int endRecord = Math.Min(_activityCurrentPage * _activityPageSize, _activityTotalCount);

            TxtActivityPagination.Text = $"Showing {startRecord}-{endRecord} of {_activityTotalCount}";

            if (BtnActivityPrevPage != null) BtnActivityPrevPage.IsEnabled = _activityCurrentPage > 1;
            if (BtnActivityNextPage != null) BtnActivityNextPage.IsEnabled = endRecord < _activityTotalCount;
        }

        private void TxtActivitySearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _activityCurrentPage = 1;
                LoadActivityLogs();
            }
        }

        private void CmbActivityFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _activityCurrentPage = 1;
                LoadActivityLogs();
            }
        }

        private void BtnSearchActivities_Click(object sender, RoutedEventArgs e)
        {
            _activityCurrentPage = 1;
            LoadActivityLogs();
        }

        private void BtnRefreshActivities_Click(object sender, RoutedEventArgs e)
        {
            if (TxtActivitySearch != null) TxtActivitySearch.Text = "";
            if (CmbActivityModule != null) CmbActivityModule.SelectedIndex = 0;
            if (CmbActivityType != null) CmbActivityType.SelectedIndex = 0;
            _activityCurrentPage = 1;
            LoadActivityLogs();
            LoadActivityStats();
        }

        private void BtnActivityPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_activityCurrentPage > 1)
            {
                _activityCurrentPage--;
                LoadActivityLogs();
            }
        }

        private void BtnActivityNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_activityTotalCount / _activityPageSize);
            if (_activityCurrentPage < totalPages)
            {
                _activityCurrentPage++;
                LoadActivityLogs();
            }
        }

        #endregion

        #region Login History

        private void LoadLoginStats()
        {
            try
            {
                // FIX: Add Null Checks for Login Stats UI Elements
                if (_loginHistoryService == null ||
                    TxtTodayLogins == null ||
                    TxtSuccessfulLogins == null ||
                    TxtFailedLogins == null ||
                    TxtLoginSuccessRate == null) return;

                var stats = _loginHistoryService.GetTodayStatistics();

                TxtTodayLogins.Text = stats.TotalLogins.ToString();
                TxtSuccessfulLogins.Text = stats.SuccessfulLogins.ToString();
                TxtFailedLogins.Text = stats.FailedLogins.ToString();
                TxtLoginSuccessRate.Text = $"{stats.SuccessRate:F0}%";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading login stats: {ex.Message}");
            }
        }

        private void LoadLoginHistory()
        {
            try
            {
                if (_loginHistoryService == null || LoginHistoryList == null) return;

                string searchText = TxtLoginSearch?.Text?.Trim() ?? "";
                string status = GetSelectedTag(CmbLoginStatus);

                // Fix: Use named arguments to match Service signature exactly if needed, or just pass positionally
                var logs = _loginHistoryService.Search(
                    query: searchText,
                    status: status,
                    startDate: null,
                    endDate: null,
                    pageNumber: _loginCurrentPage,
                    pageSize: _loginPageSize
                ).ToList();

                _loginTotalCount = _loginHistoryService.GetSearchCount(searchText, status, null, null);

                if (logs.Any())
                {
                    LoginHistoryList.ItemsSource = logs;
                    LoginHistoryList.Visibility = Visibility.Visible;
                    if (EmptyLoginPanel != null) EmptyLoginPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginHistoryList.ItemsSource = null;
                    LoginHistoryList.Visibility = Visibility.Collapsed;
                    if (EmptyLoginPanel != null) EmptyLoginPanel.Visibility = Visibility.Visible;
                }

                UpdateLoginPagination();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading login history: {ex.Message}");
            }
        }

        private void UpdateLoginPagination()
        {
            if (TxtLoginPagination == null) return;

            int startRecord = _loginTotalCount == 0 ? 0 : ((_loginCurrentPage - 1) * _loginPageSize) + 1;
            int endRecord = Math.Min(_loginCurrentPage * _loginPageSize, _loginTotalCount);

            TxtLoginPagination.Text = $"Showing {startRecord}-{endRecord} of {_loginTotalCount}";

            if (BtnLoginPrevPage != null) BtnLoginPrevPage.IsEnabled = _loginCurrentPage > 1;
            if (BtnLoginNextPage != null) BtnLoginNextPage.IsEnabled = endRecord < _loginTotalCount;
        }

        private void TxtLoginSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _loginCurrentPage = 1;
                LoadLoginHistory();
            }
        }

        private void CmbLoginFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoading)
            {
                _loginCurrentPage = 1;
                LoadLoginHistory();
            }
        }

        private void BtnSearchLogins_Click(object sender, RoutedEventArgs e)
        {
            _loginCurrentPage = 1;
            LoadLoginHistory();
        }

        private void BtnRefreshLogins_Click(object sender, RoutedEventArgs e)
        {
            if (TxtLoginSearch != null) TxtLoginSearch.Text = "";
            if (CmbLoginStatus != null) CmbLoginStatus.SelectedIndex = 0;
            _loginCurrentPage = 1;
            LoadLoginHistory();
            LoadLoginStats();
        }

        private void BtnLoginPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_loginCurrentPage > 1)
            {
                _loginCurrentPage--;
                LoadLoginHistory();
            }
        }

        private void BtnLoginNextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_loginTotalCount / _loginPageSize);
            if (_loginCurrentPage < totalPages)
            {
                _loginCurrentPage++;
                LoadLoginHistory();
            }
        }

        #endregion

        #region Active Sessions

        private void LoadActiveSessions()
        {
            try
            {
                if (_loginHistoryService == null || TxtActiveSessionCount == null || ActiveSessionsList == null) return;

                var sessions = _loginHistoryService.GetActiveSessions().ToList();

                TxtActiveSessionCount.Text = sessions.Count.ToString();

                if (sessions.Any())
                {
                    ActiveSessionsList.ItemsSource = sessions;
                    ActiveSessionsList.Visibility = Visibility.Visible;
                    if (EmptySessionsPanel != null) EmptySessionsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActiveSessionsList.ItemsSource = null;
                    ActiveSessionsList.Visibility = Visibility.Collapsed;
                    if (EmptySessionsPanel != null) EmptySessionsPanel.Visibility = Visibility.Visible;
                }

                // Disable force logout all if no sessions or only current user
                if (BtnForceLogoutAll != null)
                {
                    BtnForceLogoutAll.IsEnabled = sessions.Count > 1 ||
                        (sessions.Count == 1 && sessions[0].UserId != SessionManager.CurrentUser?.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading active sessions: {ex.Message}");
            }
        }

        private void BtnRefreshSessions_Click(object sender, RoutedEventArgs e)
        {
            LoadActiveSessions();
        }

        private void BtnForceLogoutSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is LoginHistory session)
            {
                if (session.UserId == SessionManager.CurrentUser?.Id)
                {
                    MessageBox.Show("You cannot force logout your own session.", "Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show($"Are you sure you want to force logout {session.FullName}?", "Force Logout", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        string user = SessionManager.CurrentUser?.FullName ?? "System";
                        _loginHistoryService.ForceLogout(session.Id, user);

                        // Log
                        _activityLogService.LogActivity("ForceLogout", $"Forced logout of {session.FullName}", "SystemSettings", user, "Administrator");

                        LoadActiveSessions();
                        MessageBox.Show($"{session.FullName} has been logged out.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error: {ex.Message}");
                    }
                }
            }
        }

        private void BtnForceLogoutAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Force logout ALL users except yourself?", "Force Logout All", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    string user = SessionManager.CurrentUser?.FullName ?? "System";
                    var currentSessionId = SessionManager.CurrentSessionId.ToString(); // Ensure string conversion if needed

                    _loginHistoryService.ForceLogoutAll(user, currentSessionId);

                    _activityLogService.LogActivity("ForceLogout", "Forced logout of all users", "SystemSettings", user, "Administrator");

                    LoadActiveSessions();
                    MessageBox.Show("All other users have been logged out.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Helpers

        private string GetSelectedTag(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
            {
                return item.Tag?.ToString() ?? "";
            }
            return "";
        }

        #endregion
    }
}
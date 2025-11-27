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

            _db = new RosalEHealthcareDbContext();
            _activityLogService = new ActivityLogService(_db);
            _loginHistoryService = new LoginHistoryService(_db);
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;

            try
            {
                LoadActivityLogs();
                LoadActivityStats();
                LoadLoginHistory();
                LoadLoginStats();
                LoadActiveSessions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
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
            BtnActivityLogs.Style = (Style)FindResource("SubTabButton");
            BtnLoginHistory.Style = (Style)FindResource("SubTabButton");
            BtnActiveSessions.Style = (Style)FindResource("SubTabButton");

            button.Style = (Style)FindResource("SubTabButtonActive");
        }

        private void ShowSubTabPanel(string tabName)
        {
            _currentSubTab = tabName;

            ActivityLogsPanel.Visibility = Visibility.Collapsed;
            LoginHistoryPanel.Visibility = Visibility.Collapsed;
            ActiveSessionsPanel.Visibility = Visibility.Collapsed;

            switch (tabName)
            {
                case "ActivityLogs":
                    ActivityLogsPanel.Visibility = Visibility.Visible;
                    LoadActivityLogs();
                    break;
                case "LoginHistory":
                    LoginHistoryPanel.Visibility = Visibility.Visible;
                    LoadLoginHistory();
                    break;
                case "ActiveSessions":
                    ActiveSessionsPanel.Visibility = Visibility.Visible;
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
                if (_activityLogService == null)
                {
                    System.Diagnostics.Debug.WriteLine("ActivityLogService is not initialized yet");
                    return;
                }

                var today = DateTime.Today;
                var weekStart = today.AddDays(-(int)today.DayOfWeek);
                var monthStart = new DateTime(today.Year, today.Month, 1);

                var allLogs = _activityLogService.GetRecentActivities(10000).ToList();

                TxtTodayActivities.Text = allLogs.Count(l => l.PerformedAt.Date == today).ToString();
                TxtWeekActivities.Text = allLogs.Count(l => l.PerformedAt >= weekStart).ToString();
                TxtMonthActivities.Text = allLogs.Count(l => l.PerformedAt >= monthStart).ToString();
                TxtTotalActivities.Text = allLogs.Count.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading activity stats: {ex.Message}");
            }
        }

        private void LoadActivityLogs()
        {
            try
            {
                if (_activityLogService == null) return;
                string searchText = TxtActivitySearch?.Text?.Trim() ?? "";
                string module = GetSelectedTag(CmbActivityModule);
                string activityType = GetSelectedTag(CmbActivityType);

                // Get all logs first (increase count if needed)
                var allLogs = _activityLogService.GetRecentActivities(10000).AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchText))
                {
                    allLogs = allLogs.Where(l => l.Description.Contains(searchText) ||
                                                 l.PerformedBy.Contains(searchText));
                }

                if (!string.IsNullOrEmpty(module))
                {
                    allLogs = allLogs.Where(l => l.Module == module);
                }

                if (!string.IsNullOrEmpty(activityType))
                {
                    allLogs = allLogs.Where(l => l.ActivityType == activityType);
                }

                // Get total count
                _activityTotalCount = allLogs.Count();

                // Apply pagination
                var logs = allLogs.OrderByDescending(l => l.PerformedAt)
                                  .Skip((_activityCurrentPage - 1) * _activityPageSize)
                                  .Take(_activityPageSize)
                                  .ToList();

                if (logs.Any())
                {
                    ActivityLogsList.ItemsSource = logs;
                    ActivityLogsList.Visibility = Visibility.Visible;
                    EmptyActivityPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActivityLogsList.ItemsSource = null;
                    ActivityLogsList.Visibility = Visibility.Collapsed;
                    EmptyActivityPanel.Visibility = Visibility.Visible;
                }

                UpdateActivityPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading activity logs: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateActivityPagination()
        {
            int startRecord = _activityTotalCount == 0 ? 0 : ((_activityCurrentPage - 1) * _activityPageSize) + 1;
            int endRecord = Math.Min(_activityCurrentPage * _activityPageSize, _activityTotalCount);

            TxtActivityPagination.Text = $"Showing {startRecord}-{endRecord} of {_activityTotalCount}";

            BtnActivityPrevPage.IsEnabled = _activityCurrentPage > 1;
            BtnActivityNextPage.IsEnabled = endRecord < _activityTotalCount;
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
            TxtActivitySearch.Text = "";
            CmbActivityModule.SelectedIndex = 0;
            CmbActivityType.SelectedIndex = 0;
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
                if (_loginHistoryService == null) return;
                string searchText = TxtLoginSearch?.Text?.Trim() ?? "";
                string status = GetSelectedTag(CmbLoginStatus);

                // Use the Search method from LoginHistoryService
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
                    EmptyLoginPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginHistoryList.ItemsSource = null;
                    LoginHistoryList.Visibility = Visibility.Collapsed;
                    EmptyLoginPanel.Visibility = Visibility.Visible;
                }

                UpdateLoginPagination();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading login history: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLoginPagination()
        {
            int startRecord = _loginTotalCount == 0 ? 0 : ((_loginCurrentPage - 1) * _loginPageSize) + 1;
            int endRecord = Math.Min(_loginCurrentPage * _loginPageSize, _loginTotalCount);

            TxtLoginPagination.Text = $"Showing {startRecord}-{endRecord} of {_loginTotalCount}";

            BtnLoginPrevPage.IsEnabled = _loginCurrentPage > 1;
            BtnLoginNextPage.IsEnabled = endRecord < _loginTotalCount;
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
            TxtLoginSearch.Text = "";
            CmbLoginStatus.SelectedIndex = 0;
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
                var sessions = _loginHistoryService.GetActiveSessions().ToList();

                TxtActiveSessionCount.Text = sessions.Count.ToString();

                if (sessions.Any())
                {
                    ActiveSessionsList.ItemsSource = sessions;
                    ActiveSessionsList.Visibility = Visibility.Visible;
                    EmptySessionsPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    ActiveSessionsList.ItemsSource = null;
                    ActiveSessionsList.Visibility = Visibility.Collapsed;
                    EmptySessionsPanel.Visibility = Visibility.Visible;
                }

                // Disable force logout all if no sessions or only current user
                BtnForceLogoutAll.IsEnabled = sessions.Count > 1 ||
                    (sessions.Count == 1 && sessions[0].UserId != SessionManager.CurrentUser?.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading active sessions: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Prevent logging out yourself
                if (session.UserId == SessionManager.CurrentUser?.Id)
                {
                    MessageBox.Show("You cannot force logout your own session.",
                        "Not Allowed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to force logout {session.FullName}?\n\nThey will be logged out immediately.",
                    "Force Logout",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                        _loginHistoryService.ForceLogout(session.Id, currentUser);

                        // Log activity
                        _activityLogService.LogActivity(
                            activityType: "ForceLogout",
                            description: $"Forced logout of user: {session.FullName}",
                            module: "SystemSettings",
                            performedBy: currentUser,
                            performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                        );

                        LoadActiveSessions();

                        MessageBox.Show($"{session.FullName} has been logged out.",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error forcing logout: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnForceLogoutAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to force logout ALL users?\n\nAll users except you will be logged out immediately.",
                "Force Logout All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser?.FullName ?? "System";
                    var currentSessionId = SessionManager.CurrentSessionId;

                    _loginHistoryService.ForceLogoutAll(currentUser, currentSessionId);

                    // Log activity
                    _activityLogService.LogActivity(
                        activityType: "ForceLogout",
                        description: "Forced logout of all users",
                        module: "SystemSettings",
                        performedBy: currentUser,
                        performedByRole: SessionManager.CurrentUser?.Role ?? "Administrator"
                    );

                    LoadActiveSessions();

                    MessageBox.Show("All other users have been logged out.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error forcing logout: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
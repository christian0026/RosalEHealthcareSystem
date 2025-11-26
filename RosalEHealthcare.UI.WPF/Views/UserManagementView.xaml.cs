using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class UserManagementView : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly UserService _userService;
        private readonly ActivityLogService _activityLogService;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private int _totalRecords = 0;

        // Search debounce
        private DispatcherTimer _searchTimer;

        public UserManagementView()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _userService = new UserService(_db);
            _activityLogService = new ActivityLogService(_db);

            // Setup search debounce
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                _currentPage = 1;
                LoadUsers();
            };
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSummaryCards();
            LoadUsers();
        }

        #region Data Loading

        private void LoadSummaryCards()
        {
            try
            {
                CardTotalUsers.Value = _userService.GetTotalUsers().ToString();
                CardDoctors.Value = _userService.GetUsersByRole("Doctor").ToString();
                CardReceptionists.Value = _userService.GetUsersByRole("Receptionist").ToString();
                CardAdmins.Value = _userService.GetUsersByRole("Administrator").ToString();

                // Calculate trend
                var thisMonth = _userService.GetNewUsersThisMonth();
                var lastMonth = _userService.GetNewUsersLastMonth();
                var growth = lastMonth > 0 ? Math.Round(((double)(thisMonth - lastMonth) / lastMonth) * 100, 1) : (thisMonth > 0 ? 100 : 0);

                CardTotalUsers.TrendText = growth >= 0 ? $"+{growth}% from last month" : $"{growth}% from last month";
                CardTotalUsers.TrendIcon = growth >= 0 ? "↑" : "↓";
                CardTotalUsers.TrendColor = growth >= 0
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading summary cards: {ex.Message}");
            }
        }

        private void LoadUsers()
        {
            try
            {
                string query = SearchBox.Text?.Trim();
                string role = (RoleFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Roles";
                string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Status";

                _totalRecords = _userService.GetFilteredCount(query, role, status);
                _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (_totalPages == 0) _totalPages = 1;

                var users = _userService.SearchPaged(query, role, status, _currentPage, _pageSize);

                // Convert to display model
                var displayUsers = users.Select(u => new UserDisplayModel
                {
                    Id = u.Id,
                    UserCode = u.UserCode ?? $"USR-{u.Id:D4}",
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    Status = u.Status ?? "Active",
                    LastLogin = u.LastLogin,
                    DateCreated = u.DateCreated,
                    ProfileImagePath = u.ProfileImagePath,
                    Initials = u.Initials
                }).ToList();

                UsersDataGrid.ItemsSource = displayUsers;

                TxtResultCount.Text = $" ({_totalRecords} user{(_totalRecords != 1 ? "s" : "")})";
                UpdatePaginationUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Pagination

        private void UpdatePaginationUI()
        {
            int startRecord = ((_currentPage - 1) * _pageSize) + 1;
            int endRecord = Math.Min(_currentPage * _pageSize, _totalRecords);

            TxtPageInfo.Text = _totalRecords > 0
                ? $"Showing {startRecord}-{endRecord} of {_totalRecords} users"
                : "No users found";

            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;

            UpdatePageButtons();
        }

        private void UpdatePageButtons()
        {
            BtnPage1.Style = (Style)FindResource("PaginationButton");
            BtnPage2.Style = (Style)FindResource("PaginationButton");
            BtnPage3.Style = (Style)FindResource("PaginationButton");
            BtnPageLast.Style = (Style)FindResource("PaginationButton");

            if (_totalPages <= 3)
            {
                BtnPage1.Content = "1";
                BtnPage1.Visibility = _totalPages >= 1 ? Visibility.Visible : Visibility.Collapsed;
                BtnPage2.Content = "2";
                BtnPage2.Visibility = _totalPages >= 2 ? Visibility.Visible : Visibility.Collapsed;
                BtnPage3.Content = "3";
                BtnPage3.Visibility = _totalPages >= 3 ? Visibility.Visible : Visibility.Collapsed;
                TxtEllipsis.Visibility = Visibility.Collapsed;
                BtnPageLast.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnPage1.Visibility = Visibility.Visible;
                BtnPage2.Visibility = Visibility.Visible;
                BtnPage3.Visibility = Visibility.Visible;
                TxtEllipsis.Visibility = Visibility.Visible;
                BtnPageLast.Visibility = Visibility.Visible;
                BtnPageLast.Content = _totalPages.ToString();

                if (_currentPage <= 2)
                {
                    BtnPage1.Content = "1";
                    BtnPage2.Content = "2";
                    BtnPage3.Content = "3";
                }
                else if (_currentPage >= _totalPages - 1)
                {
                    BtnPage1.Content = (_totalPages - 2).ToString();
                    BtnPage2.Content = (_totalPages - 1).ToString();
                    BtnPage3.Content = _totalPages.ToString();
                }
                else
                {
                    BtnPage1.Content = (_currentPage - 1).ToString();
                    BtnPage2.Content = _currentPage.ToString();
                    BtnPage3.Content = (_currentPage + 1).ToString();
                }
            }

            // Highlight current page
            if (BtnPage1.Content.ToString() == _currentPage.ToString())
                BtnPage1.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPage2.Content.ToString() == _currentPage.ToString())
                BtnPage2.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPage3.Content.ToString() == _currentPage.ToString())
                BtnPage3.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPageLast.Content.ToString() == _currentPage.ToString())
                BtnPageLast.Style = (Style)FindResource("PaginationButtonActive");
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadUsers();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadUsers();
            }
        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && int.TryParse(btn.Content.ToString(), out int page))
            {
                if (page >= 1 && page <= _totalPages && page != _currentPage)
                {
                    _currentPage = page;
                    LoadUsers();
                }
            }
        }

        #endregion

        #region Search & Filter

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                _currentPage = 1;
                LoadUsers();
            }
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            RoleFilter.SelectedIndex = 0;
            StatusFilter.SelectedIndex = 0;
            _currentPage = 1;
            LoadUsers();
        }

        #endregion

        #region User Actions

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditUserWindow();
            if (dialog.ShowDialog() == true)
            {
                // Log activity
                var currentUser = SessionManager.CurrentUser;
                _activityLogService.LogActivity(
                    "UserCreated",
                    $"Created new user: {dialog.User?.FullName} ({dialog.User?.Email})",
                    "UserManagement",
                    currentUser?.FullName ?? "System",
                    currentUser?.Role,
                    dialog.User?.Id.ToString()
                );

                LoadSummaryCards();
                LoadUsers();
            }
        }

        private void ViewUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                var user = _userService.GetById(userId);
                if (user != null)
                {
                    var dialog = new ViewUserDialog(user);
                    dialog.ShowDialog();
                }
            }
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                var user = _userService.GetById(userId);
                if (user != null)
                {
                    var dialog = new AddEditUserWindow(user);
                    if (dialog.ShowDialog() == true)
                    {
                        // Log activity
                        var currentUser = SessionManager.CurrentUser;
                        _activityLogService.LogActivity(
                            "UserUpdated",
                            $"Updated user: {user.FullName} ({user.Email})",
                            "UserManagement",
                            currentUser?.FullName ?? "System",
                            currentUser?.Role,
                            user.Id.ToString()
                        );

                        LoadSummaryCards();
                        LoadUsers();
                    }
                }
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int userId)
            {
                var user = _userService.GetById(userId);
                if (user == null) return;

                var currentUser = SessionManager.CurrentUser;

                // Check if can delete
                if (!_userService.CanDeleteUser(userId, currentUser?.Id ?? 0, out string reason))
                {
                    MessageBox.Show(reason, "Cannot Delete User", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Confirm deletion with details
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this user?\n\n" +
                    $"Name: {user.FullName}\n" +
                    $"Email: {user.Email}\n" +
                    $"Role: {user.Role}\n" +
                    $"User ID: {user.UserCode}\n\n" +
                    $"This action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _userService.DeleteUser(userId);

                        // Log activity
                        _activityLogService.LogActivity(
                            "UserDeleted",
                            $"Deleted user: {user.FullName} ({user.Email})",
                            "UserManagement",
                            currentUser?.FullName ?? "System",
                            currentUser?.Role,
                            userId.ToString()
                        );

                        MessageBox.Show("User deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        LoadSummaryCards();
                        LoadUsers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        #endregion
    }

    #region Display Model

    public class UserDisplayModel
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime DateCreated { get; set; }
        public string ProfileImagePath { get; set; }
        public string Initials { get; set; }

        public string LastLoginFormatted => LastLogin.HasValue ? LastLogin.Value.ToString("MMM dd, yyyy HH:mm") : "Never";

        // Role badge colors
        public Brush RoleBadgeBackground
        {
            get
            {
                switch (Role)
                {
                    case "Administrator": return new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    case "Doctor": return new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    case "Receptionist": return new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    default: return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                }
            }
        }

        public Brush RoleBadgeColor
        {
            get
            {
                switch (Role)
                {
                    case "Administrator": return new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    case "Doctor": return new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    case "Receptionist": return new SolidColorBrush(Color.FromRgb(239, 108, 0));
                    default: return new SolidColorBrush(Color.FromRgb(97, 97, 97));
                }
            }
        }

        public Brush StatusBadgeBackground
        {
            get
            {
                switch (Status)
                {
                    case "Active": return new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    case "Inactive": return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    case "Locked": return new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    case "Pending": return new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    default: return new SolidColorBrush(Color.FromRgb(245, 245, 245));
                }
            }
        }

        public Brush StatusBadgeColor
        {
            get
            {
                switch (Status)
                {
                    case "Active": return new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    case "Inactive": return new SolidColorBrush(Color.FromRgb(117, 117, 117));
                    case "Locked": return new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    case "Pending": return new SolidColorBrush(Color.FromRgb(239, 108, 0));
                    default: return new SolidColorBrush(Color.FromRgb(117, 117, 117));
                }
            }
        }
    }

    #endregion
}
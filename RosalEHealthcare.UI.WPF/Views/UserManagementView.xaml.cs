using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class UserManagementView : UserControl
    {
        private UserService _userService;
        private ActivityLogService _activityLogService;
        private RosalEHealthcareDbContext _db;

        private bool _isInitialized = false;
        private List<UserViewModel> _allUsers;
        private List<UserViewModel> _filteredUsers;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public UserManagementView()
        {
            InitializeComponent();
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                _db = new RosalEHealthcareDbContext();
                _userService = new UserService(_db);
                _activityLogService = new ActivityLogService(_db);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                MessageBox.Show($"Database Connection Failed:\n{ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            if (_allUsers == null)
            {
                await LoadDataAsync();
            }
        }

        private async Task LoadDataAsync()
        {
            ShowLoading(true);
            try
            {
                await LoadStatisticsAsync();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadStatisticsAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var totalUsers = _userService.GetTotalUsers();
                    var doctors = _userService.GetUsersByRole("Doctor");
                    var receptionists = _userService.GetUsersByRole("Receptionist");
                    var admins = _userService.GetUsersByRole("Administrator");

                    Dispatcher.Invoke(() =>
                    {
                        if (CardTotalUsers != null) CardTotalUsers.Value = totalUsers.ToString();
                        if (CardDoctors != null) CardDoctors.Value = doctors.ToString();
                        if (CardReceptionists != null) CardReceptionists.Value = receptionists.ToString();
                        if (CardAdmins != null) CardAdmins.Value = admins.ToString();
                    });
                }
                catch { }
            });
        }

        private async Task LoadUsersAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var users = _userService.GetAllUsers().ToList();

                    _allUsers = users.Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        UserCode = u.UserCode ?? $"USR-{u.Id:D4}",
                        Username = u.Username,
                        FullName = u.FullName ?? "Unknown",
                        Email = u.Email,
                        Role = u.Role,
                        Status = u.Status ?? "Active",
                        LastLogin = u.LastLogin,
                        ProfileImagePath = u.ProfileImagePath
                    }).ToList();

                    _filteredUsers = new List<UserViewModel>(_allUsers);

                    Dispatcher.Invoke(() =>
                    {
                        ApplyPagination();
                    });
                }
                catch { }
            });
        }

        private void ApplyFilters()
        {
            if (_allUsers == null) return;

            var searchQuery = txtSearch.Text?.Trim().ToLower() ?? "";
            var selectedRole = (cbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Roles";
            var selectedStatus = (cbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Status";

            _filteredUsers = _allUsers.Where(u =>
            {
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                    u.FullName.ToLower().Contains(searchQuery) ||
                    u.Username.ToLower().Contains(searchQuery) ||
                    u.Email.ToLower().Contains(searchQuery) ||
                    u.UserCode.ToLower().Contains(searchQuery);

                bool matchesRole = selectedRole == "All Roles" || u.Role == selectedRole;
                bool matchesStatus = selectedStatus == "All Status" || u.Status == selectedStatus;

                return matchesSearch && matchesRole && matchesStatus;
            }).ToList();

            _currentPage = 1;
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            if (_filteredUsers == null || dgUsers == null) return;

            _totalPages = (int)Math.Ceiling((double)_filteredUsers.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var pagedUsers = _filteredUsers
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            dgUsers.ItemsSource = pagedUsers;

            if (txtResultCount != null) txtResultCount.Text = $"Showing {pagedUsers.Count} of {_filteredUsers.Count} users";
            if (txtPageInfo != null) txtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";

            if (btnFirst != null) btnFirst.IsEnabled = _currentPage > 1;
            if (btnPrevious != null) btnPrevious.IsEnabled = _currentPage > 1;
            if (btnNext != null) btnNext.IsEnabled = _currentPage < _totalPages;
            if (btnLast != null) btnLast.IsEnabled = _currentPage < _totalPages;
        }

        private void ShowLoading(bool show)
        {
            if (LoadingOverlay != null)
                LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allUsers != null) ApplyFilters();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (txtSearch != null) txtSearch.Text = "";
            if (cbRole != null) cbRole.SelectedIndex = 0;
            if (cbStatus != null) cbStatus.SelectedIndex = 0;
        }

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            var dialog = new AddEditUserWindow();
            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
                LogActivitySafe("UserCreated", $"Created new user: {dialog.User?.FullName}");
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var user = _userService.GetById(id);
                if (user != null)
                {
                    var dialog = new AddEditUserWindow(user);
                    if (dialog.ShowDialog() == true)
                    {
                        _ = LoadDataAsync();
                        LogActivitySafe("UserUpdated", $"Updated user: {user.FullName}");
                    }
                }
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var user = _userService.GetById(id);
                if (user != null)
                {
                    var dialog = new ViewUserDialog(user);
                    dialog.ShowDialog();
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                var user = _allUsers.FirstOrDefault(u => u.Id == id);
                if (user == null) return;

                // Prevent deleting self
                if (SessionManager.CurrentUser != null && SessionManager.CurrentUser.Id == id)
                {
                    MessageBox.Show("You cannot delete your own account.", "Action Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show($"Are you sure you want to delete user '{user.FullName}'?\nThis action cannot be undone.",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ShowLoading(true);
                        await Task.Run(() =>
                        {
                            _userService.DeleteUser(id);
                        });
                        LogActivitySafe("UserDeleted", $"Deleted user: {user.FullName}");
                        await LoadDataAsync();
                    }
                    catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
                    finally { ShowLoading(false); }
                }
            }
        }

        private void LogActivitySafe(string type, string desc)
        {
            try
            {
                if (SessionManager.CurrentUser != null && _activityLogService != null)
                {
                    _activityLogService.LogActivity(type, desc, "User Management",
                        SessionManager.CurrentUser.FullName, SessionManager.CurrentUser.Role);
                }
            }
            catch { }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e) { _currentPage = 1; ApplyPagination(); }
        private void BtnPrevious_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ApplyPagination(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { if (_currentPage < _totalPages) { _currentPage++; ApplyPagination(); } }
        private void BtnLast_Click(object sender, RoutedEventArgs e) { _currentPage = _totalPages; ApplyPagination(); }
    }

    // VIEW MODEL
    public class UserViewModel
    {
        public int Id { get; set; }
        public string UserCode { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime? LastLogin { get; set; }
        public string ProfileImagePath { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "?";
                var words = FullName.Split(' ');
                if (words.Length >= 2) return $"{words[0][0]}{words[1][0]}".ToUpper();
                return FullName.Length >= 2 ? FullName.Substring(0, 2).ToUpper() : FullName.ToUpper();
            }
        }

        public string LastLoginFormatted => LastLogin.HasValue ? LastLogin.Value.ToString("MMM dd, yyyy HH:mm") : "Never";

        // Colors logic inside ViewModel for clean XAML
        public Brush RoleBadgeBackground
        {
            get
            {
                switch (Role)
                {
                    case "Administrator": return new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Red Light
                    case "Doctor": return new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Blue Light
                    case "Receptionist": return new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Orange Light
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
                    case "Administrator": return new SolidColorBrush(Color.FromRgb(198, 40, 40)); // Red Dark
                    case "Doctor": return new SolidColorBrush(Color.FromRgb(21, 101, 192)); // Blue Dark
                    case "Receptionist": return new SolidColorBrush(Color.FromRgb(239, 108, 0)); // Orange Dark
                    default: return new SolidColorBrush(Color.FromRgb(97, 97, 97));
                }
            }
        }
    }
}
using MahApps.Metro.IconPacks;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserSecurityTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly UserService _userService;
        private readonly LoginHistoryService _loginHistoryService;
        private readonly ActivityLogService _activityLogService;

        // Password visibility state
        private bool _showCurrentPassword = false;
        private bool _showNewPassword = false;
        private bool _showConfirmPassword = false;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public UserSecurityTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _userService = new UserService(_db);
            _loginHistoryService = new LoginHistoryService(_db);
            _activityLogService = new ActivityLogService(_db);
        }

        #region Load Settings

        public void LoadSettings()
        {
            try
            {
                ClearPasswordFields();
                LoadPasswordStatus();
                LoadCurrentSessionInfo();
                LoadLoginHistory();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading security settings: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPasswordStatus()
        {
            var user = SessionManager.CurrentUser;
            if (user == null) return;

            // Get fresh user data
            var dbUser = _userService.GetById(user.Id);
            if (dbUser == null) return;

            // Password last changed
            if (dbUser.PasswordChangedAt.HasValue)
            {
                var lastChanged = dbUser.PasswordChangedAt.Value;
                TxtLastPasswordChange.Text = $"Last changed: {lastChanged:MMMM dd, yyyy 'at' h:mm tt}";

                var daysSince = (DateTime.Now - lastChanged).Days;
                if (daysSince == 0)
                {
                    TxtPasswordAge.Text = "Changed today";
                    TxtPasswordAge.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else if (daysSince < 30)
                {
                    TxtPasswordAge.Text = $"{daysSince} day(s) ago";
                    TxtPasswordAge.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else if (daysSince < 90)
                {
                    TxtPasswordAge.Text = $"{daysSince} days ago";
                    TxtPasswordAge.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    TxtPasswordAge.Text = $"{daysSince} days ago - Consider changing your password";
                    TxtPasswordAge.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                }
            }
            else
            {
                TxtLastPasswordChange.Text = "Last changed: Never";
                TxtPasswordAge.Text = "Consider setting a strong password";
                TxtPasswordAge.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
            }
        }

        private void LoadCurrentSessionInfo()
        {
            var sessionStart = SessionManager.SessionStartTime;
            if (sessionStart.HasValue)
            {
                TxtCurrentSessionInfo.Text = $"Started: {sessionStart.Value:MMM dd, yyyy 'at' h:mm tt}";

                var duration = DateTime.Now - sessionStart.Value;
                if (duration.TotalHours >= 1)
                {
                    TxtSessionDuration.Text = $"Duration: {(int)duration.TotalHours}h {duration.Minutes}m";
                }
                else
                {
                    TxtSessionDuration.Text = $"Duration: {duration.Minutes}m";
                }
            }
            else
            {
                TxtCurrentSessionInfo.Text = "Session info unavailable";
                TxtSessionDuration.Text = "";
            }
        }

        private void LoadLoginHistory()
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return;

                // Get login history for current user
                var allHistory = _loginHistoryService.GetByUserId(user.Id, 100).ToList();
                var totalCount = allHistory.Count;
                _totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / _pageSize));

                if (_currentPage > _totalPages) _currentPage = _totalPages;

                var pagedHistory = allHistory
                    .Skip((_currentPage - 1) * _pageSize)
                    .Take(_pageSize)
                    .Select(h => new LoginHistoryViewModel
                    {
                        LoginTime = h.LoginTime,
                        LoginTimeFormatted = FormatLoginTime(h.LoginTime),
                        MachineName = h.MachineName ?? "Unknown",
                        IpAddress = h.IpAddress ?? "N/A",
                        Status = h.Status,
                        StatusIcon = GetStatusIcon(h.Status),
                        StatusBackground = GetStatusBackground(h.Status),
                        StatusBadgeBackground = GetStatusBadgeBackground(h.Status),
                        StatusBadgeForeground = GetStatusBadgeForeground(h.Status)
                    })
                    .ToList();

                if (pagedHistory.Any())
                {
                    LoginHistoryList.ItemsSource = pagedHistory;
                    LoginHistoryList.Visibility = Visibility.Visible;
                    EmptyHistoryPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    LoginHistoryList.ItemsSource = null;
                    LoginHistoryList.Visibility = Visibility.Collapsed;
                    EmptyHistoryPanel.Visibility = Visibility.Visible;
                }

                // Update pagination
                TxtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";
                BtnPrevPage.IsEnabled = _currentPage > 1;
                BtnNextPage.IsEnabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading login history: {ex.Message}");
                LoginHistoryList.ItemsSource = null;
                EmptyHistoryPanel.Visibility = Visibility.Visible;
            }
        }

        private string FormatLoginTime(DateTime loginTime)
        {
            if (loginTime.Date == DateTime.Today)
            {
                return $"Today at {loginTime:h:mm tt}";
            }
            else if (loginTime.Date == DateTime.Today.AddDays(-1))
            {
                return $"Yesterday at {loginTime:h:mm tt}";
            }
            else
            {
                return loginTime.ToString("MMM dd, yyyy 'at' h:mm tt");
            }
        }

        private string GetStatusIcon(string status)
        {
            switch (status?.ToUpper())
            {
                case "SUCCESS": return "✓";
                case "FAILED": return "✗";
                case "LOCKED": return "🔒";
                default: return "•";
            }
        }

        private Brush GetStatusBackground(string status)
        {
            switch (status?.ToUpper())
            {
                case "SUCCESS": return new SolidColorBrush(Color.FromRgb(232, 245, 233)); // Light green
                case "FAILED": return new SolidColorBrush(Color.FromRgb(255, 235, 238)); // Light red
                case "LOCKED": return new SolidColorBrush(Color.FromRgb(255, 243, 224)); // Light orange
                default: return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
        }

        private Brush GetStatusBadgeBackground(string status)
        {
            switch (status?.ToUpper())
            {
                case "SUCCESS": return new SolidColorBrush(Color.FromRgb(232, 245, 233));
                case "FAILED": return new SolidColorBrush(Color.FromRgb(255, 235, 238));
                case "LOCKED": return new SolidColorBrush(Color.FromRgb(255, 243, 224));
                default: return new SolidColorBrush(Color.FromRgb(245, 245, 245));
            }
        }

        private Brush GetStatusBadgeForeground(string status)
        {
            switch (status?.ToUpper())
            {
                case "SUCCESS": return new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Green
                case "FAILED": return new SolidColorBrush(Color.FromRgb(211, 47, 47)); // Red
                case "LOCKED": return new SolidColorBrush(Color.FromRgb(245, 124, 0)); // Orange
                default: return new SolidColorBrush(Color.FromRgb(117, 117, 117));
            }
        }

        #endregion

        #region Password Toggle

        private void BtnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                switch (tag)
                {
                    case "Current":
                        _showCurrentPassword = !_showCurrentPassword;
                        IconToggleCurrent.Kind = _showCurrentPassword ? PackIconMaterialKind.EyeOff : PackIconMaterialKind.Eye;
                        break;
                    case "New":
                        _showNewPassword = !_showNewPassword;
                        IconToggleNew.Kind = _showNewPassword ? PackIconMaterialKind.EyeOff : PackIconMaterialKind.Eye;
                        break;
                    case "Confirm":
                        _showConfirmPassword = !_showConfirmPassword;
                        IconToggleConfirm.Kind = _showConfirmPassword ? PackIconMaterialKind.EyeOff : PackIconMaterialKind.Eye;
                        break;
                }
            }
        }

        #endregion

        #region Password Validation

        private void Password_Changed(object sender, RoutedEventArgs e)
        {
            // Just track that password has been entered
        }

        private void NewPassword_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePasswordStrength(TxtNewPassword.Password);
            UpdatePasswordMatch();
        }

        private void ConfirmPassword_Changed(object sender, RoutedEventArgs e)
        {
            UpdatePasswordMatch();
        }

        private void UpdatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ResetStrengthBars();
                TxtPasswordStrength.Text = "";
                return;
            }

            int strength = CalculatePasswordStrength(password);

            // Reset bars
            StrengthBar1.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            StrengthBar2.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            StrengthBar3.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            StrengthBar4.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));

            Brush strengthColor;
            string strengthText;

            if (strength <= 1)
            {
                strengthColor = (Brush)FindResource("StrengthWeak");
                strengthText = "Weak";
                StrengthBar1.Background = strengthColor;
            }
            else if (strength == 2)
            {
                strengthColor = (Brush)FindResource("StrengthFair");
                strengthText = "Fair";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
            }
            else if (strength == 3)
            {
                strengthColor = (Brush)FindResource("StrengthGood");
                strengthText = "Good";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
                StrengthBar3.Background = strengthColor;
            }
            else
            {
                strengthColor = (Brush)FindResource("StrengthStrong");
                strengthText = "Strong";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
                StrengthBar3.Background = strengthColor;
                StrengthBar4.Background = strengthColor;
            }

            TxtPasswordStrength.Text = $"Password Strength: {strengthText}";
            TxtPasswordStrength.Foreground = strengthColor;
        }

        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            if (password.Length >= 8) strength++;
            if (Regex.IsMatch(password, @"[a-z]")) strength++;
            if (Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (Regex.IsMatch(password, @"[0-9]")) strength++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) strength++;

            return Math.Min(4, strength);
        }

        private void ResetStrengthBars()
        {
            var defaultColor = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            StrengthBar1.Background = defaultColor;
            StrengthBar2.Background = defaultColor;
            StrengthBar3.Background = defaultColor;
            StrengthBar4.Background = defaultColor;
        }

        private void UpdatePasswordMatch()
        {
            string newPassword = TxtNewPassword.Password;
            string confirmPassword = TxtConfirmPassword.Password;

            if (string.IsNullOrEmpty(confirmPassword))
            {
                PasswordMatchPanel.Visibility = Visibility.Collapsed;
                return;
            }

            PasswordMatchPanel.Visibility = Visibility.Visible;

            if (newPassword == confirmPassword)
            {
                IconPasswordMatch.Kind = PackIconMaterialKind.CheckCircle;
                IconPasswordMatch.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                TxtPasswordMatch.Text = "Passwords match";
                TxtPasswordMatch.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else
            {
                IconPasswordMatch.Kind = PackIconMaterialKind.CloseCircle;
                IconPasswordMatch.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                TxtPasswordMatch.Text = "Passwords do not match";
                TxtPasswordMatch.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
        }

        private bool ValidatePassword(string password, out string errorMessage)
        {
            errorMessage = "";

            if (password.Length < 8)
            {
                errorMessage = "Password must be at least 8 characters long.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[0-9]"))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }

            return true;
        }

        #endregion

        #region Change Password

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null)
                {
                    MessageBox.Show("Session expired. Please log in again.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string currentPassword = TxtCurrentPassword.Password;
                string newPassword = TxtNewPassword.Password;
                string confirmPassword = TxtConfirmPassword.Password;

                // Validate current password is entered
                if (string.IsNullOrEmpty(currentPassword))
                {
                    MessageBox.Show("Please enter your current password.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCurrentPassword.Focus();
                    return;
                }

                // Validate new password is entered
                if (string.IsNullOrEmpty(newPassword))
                {
                    MessageBox.Show("Please enter a new password.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNewPassword.Focus();
                    return;
                }

                // Validate password strength
                if (!ValidatePassword(newPassword, out string errorMessage))
                {
                    MessageBox.Show(errorMessage, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNewPassword.Focus();
                    return;
                }

                // Validate passwords match
                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("New passwords do not match.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtConfirmPassword.Focus();
                    return;
                }

                // Validate current password is correct
                if (!_userService.ValidateUserByUsername(user.Username, currentPassword))
                {
                    MessageBox.Show("Current password is incorrect.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCurrentPassword.Focus();
                    TxtCurrentPassword.Clear();
                    return;
                }

                // Check new password is different from current
                if (currentPassword == newPassword)
                {
                    MessageBox.Show("New password must be different from current password.",
                        "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNewPassword.Focus();
                    return;
                }

                // Change password
                var dbUser = _userService.GetById(user.Id);
                if (dbUser == null) return;

                _userService.ChangePassword(dbUser.Id, newPassword);

                // Update password changed date
                dbUser.PasswordChangedAt = DateTime.Now;
                _userService.UpdateUser(dbUser);

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Changed password",
                    module: "UserSettings",
                    performedBy: user.FullName,
                    performedByRole: user.Role
                );

                MessageBox.Show("Password changed successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearPasswordFields();
                LoadPasswordStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing password: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearPasswordFields()
        {
            TxtCurrentPassword.Clear();
            TxtNewPassword.Clear();
            TxtConfirmPassword.Clear();
            ResetStrengthBars();
            TxtPasswordStrength.Text = "";
            PasswordMatchPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Pagination

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadLoginHistory();
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadLoginHistory();
            }
        }

        private void BtnRefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadLoginHistory();
            LoadCurrentSessionInfo();
        }

        #endregion
    }

    #region View Models

    public class LoginHistoryViewModel
    {
        public DateTime LoginTime { get; set; }
        public string LoginTimeFormatted { get; set; }
        public string MachineName { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; }
        public string StatusIcon { get; set; }
        public Brush StatusBackground { get; set; }
        public Brush StatusBadgeBackground { get; set; }
        public Brush StatusBadgeForeground { get; set; }
    }

    #endregion
}
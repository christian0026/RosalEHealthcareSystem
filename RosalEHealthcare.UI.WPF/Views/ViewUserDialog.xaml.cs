using RosalEHealthcare.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ViewUserDialog : Window
    {
        private readonly User _user;

        public ViewUserDialog(User user)
        {
            InitializeComponent();
            _user = user;

            LoadUserData();

            // Allow window dragging
            this.MouseLeftButtonDown += (s, e) => { try { DragMove(); } catch { } };
        }

        private void LoadUserData()
        {
            if (_user == null) return;

            // Basic Info
            TxtUserName.Text = _user.FullName;
            TxtUsername.Text = _user.Username ?? "-";
            TxtUserCode.Text = _user.UserCode ?? $"USR-{_user.Id:D4}";
            TxtEmail.Text = _user.Email;
            TxtRole.Text = _user.Role;

            // Status
            TxtStatus.Text = _user.Status ?? "Active";
            TxtStatusLabel.Text = GetStatusDescription(_user.Status);
            ApplyStatusStyle(_user.Status);

            // Role badge
            ApplyRoleStyle(_user.Role);

            // Dates
            TxtLastLogin.Text = _user.LastLogin.HasValue
                ? _user.LastLogin.Value.ToString("MMM dd, yyyy HH:mm")
                : "Never logged in";

            TxtDateCreated.Text = _user.DateCreated.ToString("MMM dd, yyyy HH:mm");

            // Created/Modified info
            TxtCreatedBy.Text = !string.IsNullOrEmpty(_user.CreatedBy) ? _user.CreatedBy : "System";
            TxtModifiedAt.Text = _user.ModifiedAt.HasValue
                ? $"{_user.ModifiedAt.Value:MMM dd, yyyy}\nby {_user.ModifiedBy ?? "Unknown"}"
                : "Never modified";

            // Initials
            TxtInitials.Text = _user.Initials;

            // Profile Image
            if (!string.IsNullOrEmpty(_user.ProfileImagePath) && File.Exists(_user.ProfileImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(_user.ProfileImagePath, UriKind.Absolute);
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;
                    ProfileImage.Visibility = Visibility.Visible;
                    TxtInitials.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    // Keep initials visible
                }
            }
        }

        private string GetStatusDescription(string status)
        {
            switch (status)
            {
                case "Active": return "Account is active and operational";
                case "Inactive": return "Account has been deactivated";
                case "Locked": return "Account is locked due to security";
                case "Pending": return "Pending password change";
                default: return "Unknown status";
            }
        }

        private void ApplyStatusStyle(string status)
        {
            switch (status)
            {
                case "Active":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;
                case "Inactive":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                    break;
                case "Locked":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    break;
                case "Pending":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                    break;
                default:
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    TxtStatus.Foreground = new SolidColorBrush(Color.FromRgb(117, 117, 117));
                    break;
            }
        }

        private void ApplyRoleStyle(string role)
        {
            switch (role)
            {
                case "Administrator":
                    RoleBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    TxtRole.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    break;
                case "Doctor":
                    RoleBadge.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    TxtRole.Foreground = new SolidColorBrush(Color.FromRgb(21, 101, 192));
                    break;
                case "Receptionist":
                    RoleBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    TxtRole.Foreground = new SolidColorBrush(Color.FromRgb(239, 108, 0));
                    break;
                default:
                    RoleBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    TxtRole.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
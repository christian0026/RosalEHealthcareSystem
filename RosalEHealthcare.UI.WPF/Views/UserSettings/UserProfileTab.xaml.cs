using Microsoft.Win32;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserProfileTab : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly UserService _userService;
        private readonly ActivityLogService _activityLogService;

        private bool _isLoading = false;
        private bool _hasChanges = false;
        private string _newProfileImagePath = null;

        public UserProfileTab()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _userService = new UserService(_db);
            _activityLogService = new ActivityLogService(_db);
        }

        #region Load Settings

        public void LoadSettings()
        {
            _isLoading = true;
            _hasChanges = false;
            _newProfileImagePath = null;

            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return;

                // Refresh user data from database
                var freshUser = _userService.GetById(user.Id);
                if (freshUser == null) return;

                // Profile header
                TxtDisplayName.Text = freshUser.FullName ?? "Unknown User";
                TxtRoleBadge.Text = freshUser.Role ?? "User";
                TxtAvatarInitials.Text = GetInitials(freshUser.FullName ?? "U");

                // Member since
                if (freshUser. CreatedAt != default)
                {
                    TxtMemberSince.Text = $"Member since {freshUser.CreatedAt:MMMM yyyy}";
                }
                else
                {
                    TxtMemberSince.Text = "Member";
                }

                // Profile image
                LoadProfileImage(freshUser.ProfileImagePath);

                // Personal information
                var nameParts = (freshUser.FullName ?? "").Split(new[] { ' ' }, 2);
                TxtFirstName.Text = nameParts.Length > 0 ? nameParts[0] : "";
                TxtLastName.Text = nameParts.Length > 1 ? nameParts[1] : "";
                TxtEmail.Text = freshUser.Email ?? "";
                TxtUsername.Text = freshUser.Username ?? "";

                // Contact information
                TxtPhone.Text = freshUser.Contact ?? "";
                TxtAddress.Text = freshUser.Address ?? "";

                // Account information
                TxtRole.Text = freshUser.Role ?? "User";
                TxtStatus.Text = freshUser.IsActive ? "Active" : "Inactive";
                StatusIndicator.Fill = freshUser.IsActive
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));

                // Last login
                if (freshUser.LastLogin.HasValue)
                {
                    var lastLogin = freshUser.LastLogin.Value;
                    if (lastLogin.Date == DateTime.Today)
                    {
                        TxtLastLogin.Text = $"Today at {lastLogin:h:mm tt}";
                    }
                    else if (lastLogin.Date == DateTime.Today.AddDays(-1))
                    {
                        TxtLastLogin.Text = $"Yesterday at {lastLogin:h:mm tt}";
                    }
                    else
                    {
                        TxtLastLogin.Text = lastLogin.ToString("MMM dd, yyyy");
                    }
                }
                else
                {
                    TxtLastLogin.Text = "Never";
                }

                // Show/hide remove photo button
                BtnRemovePhoto.Visibility = !string.IsNullOrEmpty(freshUser.ProfileImagePath)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadProfileImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ProfileImageEllipse.Fill = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                    TxtAvatarInitials.Visibility = Visibility.Collapsed;
                    BtnRemovePhoto.Visibility = Visibility.Visible;
                }
                catch
                {
                    ProfileImageEllipse.Fill = Brushes.Transparent;
                    TxtAvatarInitials.Visibility = Visibility.Visible;
                    BtnRemovePhoto.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ProfileImageEllipse.Fill = Brushes.Transparent;
                TxtAvatarInitials.Visibility = Visibility.Visible;
                BtnRemovePhoto.Visibility = Visibility.Collapsed;
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

        #region Photo Actions

        private void BtnChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select Profile Photo",
                Filter = "Image Files (*.jpg;*.jpeg;*.png;*.gif)|*.jpg;*.jpeg;*.png;*.gif|All Files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Validate file size (max 5MB)
                    var fileInfo = new FileInfo(dialog.FileName);
                    if (fileInfo.Length > 5 * 1024 * 1024)
                    {
                        MessageBox.Show("Image file is too large. Maximum size is 5MB.",
                            "File Too Large", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Create profile images folder
                    var profileImagesPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "RosalHealthcare", "ProfileImages");

                    if (!Directory.Exists(profileImagesPath))
                        Directory.CreateDirectory(profileImagesPath);

                    // Copy file with unique name
                    var extension = Path.GetExtension(dialog.FileName);
                    var newFileName = $"{SessionManager.CurrentUser.Id}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
                    var newFilePath = Path.Combine(profileImagesPath, newFileName);

                    File.Copy(dialog.FileName, newFilePath, true);

                    // Update UI
                    _newProfileImagePath = newFilePath;
                    LoadProfileImage(newFilePath);
                    _hasChanges = true;

                    MessageBox.Show("Photo selected. Click 'Save Changes' to apply.",
                        "Photo Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error selecting photo: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to remove your profile photo?",
                "Remove Photo",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _newProfileImagePath = ""; // Empty string means remove
                ProfileImageEllipse.Fill = Brushes.Transparent;
                TxtAvatarInitials.Visibility = Visibility.Visible;
                BtnRemovePhoto.Visibility = Visibility.Collapsed;
                _hasChanges = true;
            }
        }

        #endregion

        #region Save / Cancel

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var user = SessionManager.CurrentUser;
                if (user == null) return;

                // Validation
                if (string.IsNullOrWhiteSpace(TxtFirstName.Text))
                {
                    MessageBox.Show("First name is required.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtFirstName.Focus();
                    return;
                }

                // Get fresh user from database
                var dbUser = _userService.GetById(user.Id);
                if (dbUser == null) return;

                // Update user properties
                string firstName = TxtFirstName.Text.Trim();
                string lastName = TxtLastName.Text.Trim();
                dbUser.FullName = string.IsNullOrEmpty(lastName) ? firstName : $"{firstName} {lastName}";
                dbUser.Contact = TxtPhone.Text.Trim();
                dbUser.Address = TxtAddress.Text.Trim();

                // Update profile image if changed
                if (_newProfileImagePath != null)
                {
                    if (_newProfileImagePath == "")
                    {
                        // Remove photo
                        if (!string.IsNullOrEmpty(dbUser.ProfileImagePath) && File.Exists(dbUser.ProfileImagePath))
                        {
                            try { File.Delete(dbUser.ProfileImagePath); } catch { }
                        }
                        dbUser.ProfileImagePath = null;
                    }
                    else
                    {
                        dbUser.ProfileImagePath = _newProfileImagePath;
                    }
                }

                // Save to database
                _userService.UpdateUser(dbUser);

                // Update session
                SessionManager.CurrentUser = dbUser;

                // Log activity
                _activityLogService.LogActivity(
                    activityType: "Update",
                    description: "Updated profile information",
                    module: "UserSettings",
                    performedBy: dbUser.FullName,
                    performedByRole: dbUser.Role
                );

                _hasChanges = false;
                _newProfileImagePath = null;

                MessageBox.Show("Profile updated successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Reload to show updated data
                LoadSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving profile: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (result != MessageBoxResult.Yes) return;
            }

            LoadSettings();
        }

        #endregion
    }
}
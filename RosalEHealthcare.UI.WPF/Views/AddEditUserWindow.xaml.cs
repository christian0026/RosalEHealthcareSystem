using Microsoft.Win32;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditUserWindow : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly UserService _userService;
        private readonly ActivityLogService _activityLogService;

        private bool _isEditMode = false;
        private User _editingUser;
        private string _imagePath = "";
        private bool _isPasswordVisible = false;
        private bool _imageRemoved = false;

        public User User { get; private set; }

        #region Constructor

        public AddEditUserWindow()
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _userService = new UserService(_db);
            _activityLogService = new ActivityLogService(_db);

            // Set defaults
            cbRole.SelectedIndex = 1; // Doctor
            cbStatus.SelectedIndex = 0; // Active

            // Allow window dragging
            this.MouseLeftButtonDown += (s, e) => { try { DragMove(); } catch { } };
        }

        public AddEditUserWindow(User user) : this()
        {
            _isEditMode = true;
            _editingUser = user;

            // Update UI for edit mode
            TxtWindowTitle.Text = "Edit User";
            TxtWindowSubtitle.Text = $"Modify account for {user.FullName}";
            BtnSave.Content = "Update User";

            // Show reset password section
            ResetPasswordSection.Visibility = Visibility.Visible;

            // Make password optional in edit mode
            TxtPasswordLabel.Text = "New Password (Optional)";
            TxtPasswordRequired.Visibility = Visibility.Collapsed;
            TxtConfirmPasswordRequired.Visibility = Visibility.Collapsed;

            // Load user data
            LoadUserData(user);
        }

        #endregion

        #region Data Loading

        private void LoadUserData(User user)
        {
            txtFullName.Text = user.FullName;
            txtUsername.Text = user.Username;
            txtEmail.Text = user.Email;

            // Set Role
            foreach (ComboBoxItem item in cbRole.Items)
            {
                if (item.Content.ToString() == user.Role)
                {
                    cbRole.SelectedItem = item;
                    break;
                }
            }

            // Set Status
            foreach (ComboBoxItem item in cbStatus.Items)
            {
                if (item.Content.ToString() == user.Status)
                {
                    cbStatus.SelectedItem = item;
                    break;
                }
            }

            // Load profile image
            if (!string.IsNullOrEmpty(user.ProfileImagePath) && File.Exists(user.ProfileImagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(user.ProfileImagePath, UriKind.Absolute);
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;
                    ProfileImage.Visibility = Visibility.Visible;
                    TxtProfileInitials.Visibility = Visibility.Collapsed;
                    BtnRemovePhoto.Visibility = Visibility.Visible;
                    _imagePath = user.ProfileImagePath;
                }
                catch
                {
                    UpdateInitials();
                }
            }
            else
            {
                UpdateInitials();
            }
        }

        private void UpdateInitials()
        {
            string name = txtFullName.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name))
            {
                TxtProfileInitials.Text = "?";
            }
            else
            {
                var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    TxtProfileInitials.Text = $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
                else
                    TxtProfileInitials.Text = name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
            }
        }

        #endregion

        #region Profile Image

        private void UploadProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Select Profile Picture"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(dialog.FileName, UriKind.Absolute);
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;
                    ProfileImage.Visibility = Visibility.Visible;
                    TxtProfileInitials.Visibility = Visibility.Collapsed;
                    BtnRemovePhoto.Visibility = Visibility.Visible;
                    _imagePath = dialog.FileName;
                    _imageRemoved = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            ProfileImage.Source = null;
            ProfileImage.Visibility = Visibility.Collapsed;
            TxtProfileInitials.Visibility = Visibility.Visible;
            BtnRemovePhoto.Visibility = Visibility.Collapsed;
            _imagePath = "";
            _imageRemoved = true;
            UpdateInitials();
        }

        #endregion

        #region Password Handling

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password - switch to TextBox
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;

                // Update icon to EyeOff
                var icon = FindVisualChild<MahApps.Metro.IconPacks.PackIconMaterial>(BtnTogglePassword);
                if (icon != null)
                {
                    icon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.EyeOff;
                }

                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                // Hide password - switch to PasswordBox
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;

                // Update icon to Eye
                var icon = FindVisualChild<MahApps.Metro.IconPacks.PackIconMaterial>(BtnTogglePassword);
                if (icon != null)
                {
                    icon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Eye;
                }

                txtPassword.Focus();
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                ValidatePasswordStrength(txtPassword.Password);
                ValidateConfirmPassword();
            }
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                ValidatePasswordStrength(txtPasswordVisible.Text);
            }
        }

        private void txtConfirmPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ValidateConfirmPassword();
        }

        private void ValidatePasswordStrength(string password)
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
                strengthColor = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                strengthText = "Weak";
                StrengthBar1.Background = strengthColor;
            }
            else if (strength == 2)
            {
                strengthColor = new SolidColorBrush(Color.FromRgb(255, 152, 0)); // Orange
                strengthText = "Fair";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
            }
            else if (strength == 3)
            {
                strengthColor = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Yellow
                strengthText = "Good";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
                StrengthBar3.Background = strengthColor;
            }
            else
            {
                strengthColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                strengthText = "Strong";
                StrengthBar1.Background = strengthColor;
                StrengthBar2.Background = strengthColor;
                StrengthBar3.Background = strengthColor;
                StrengthBar4.Background = strengthColor;
            }

            TxtPasswordStrength.Text = strengthText;
            TxtPasswordStrength.Foreground = strengthColor;
        }

        private int CalculatePasswordStrength(string password)
        {
            int strength = 0;

            if (password.Length >= 8) strength++;
            if (password.Length >= 12) strength++;
            if (Regex.IsMatch(password, @"[A-Z]")) strength++;
            if (Regex.IsMatch(password, @"[a-z]")) strength++;
            if (Regex.IsMatch(password, @"[0-9]")) strength++;
            if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>]")) strength++;

            // Normalize to 4 levels
            if (strength <= 2) return 1;
            if (strength <= 3) return 2;
            if (strength <= 4) return 3;
            return 4;
        }

        private void ResetStrengthBars()
        {
            var gray = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            StrengthBar1.Background = gray;
            StrengthBar2.Background = gray;
            StrengthBar3.Background = gray;
            StrengthBar4.Background = gray;
        }

        private void ValidateConfirmPassword()
        {
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (!string.IsNullOrEmpty(confirmPassword) && password != confirmPassword)
            {
                TxtConfirmPasswordError.Text = "Passwords do not match";
                TxtConfirmPasswordError.Visibility = Visibility.Visible;
            }
            else
            {
                TxtConfirmPasswordError.Visibility = Visibility.Collapsed;
            }
        }

        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_editingUser == null) return;

            var result = MessageBox.Show(
                $"Are you sure you want to reset the password for {_editingUser.FullName}?\n\n" +
                "A temporary password will be generated and the user will be required to change it on next login.",
                "Confirm Password Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var currentUser = SessionManager.CurrentUser;
                    string tempPassword = _userService.ResetPassword(_editingUser.Id, currentUser?.FullName ?? "System");

                    if (!string.IsNullOrEmpty(tempPassword))
                    {
                        // Log activity
                        _activityLogService.LogActivity(
                            "PasswordReset",
                            $"Password reset for user: {_editingUser.FullName} ({_editingUser.Email})",
                            "UserManagement",
                            currentUser?.FullName ?? "System",
                            currentUser?.Role,
                            _editingUser.Id.ToString()
                        );

                        MessageBox.Show(
                            $"Password has been reset successfully.\n\n" +
                            $"Temporary Password: {tempPassword}\n\n" +
                            "Please provide this password to the user. They will be required to change it on their next login.",
                            "Password Reset Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error resetting password: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Validation

        private void txtFullName_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateInitials();
            ValidateFullName();

            // Auto-generate username if empty (new user mode only)
            if (!_isEditMode && string.IsNullOrEmpty(txtUsername.Text))
            {
                string fullName = txtFullName.Text?.Trim();
                if (!string.IsNullOrEmpty(fullName) && fullName.Length >= 2)
                {
                    txtUsername.Text = _userService.GenerateUsername(fullName);
                }
            }
        }

        private void txtEmail_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateEmail();
        }

        private bool ValidateFullName()
        {
            string name = txtFullName.Text?.Trim();

            if (string.IsNullOrEmpty(name))
            {
                TxtFullNameError.Text = "Full name is required";
                TxtFullNameError.Visibility = Visibility.Visible;
                return false;
            }

            if (name.Length < 2)
            {
                TxtFullNameError.Text = "Name must be at least 2 characters";
                TxtFullNameError.Visibility = Visibility.Visible;
                return false;
            }

            TxtFullNameError.Visibility = Visibility.Collapsed;
            return true;
        }

        private bool ValidateEmail()
        {
            string email = txtEmail.Text?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                TxtEmailError.Text = "Email is required";
                TxtEmailError.Visibility = Visibility.Visible;
                return false;
            }

            // Email format validation
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                TxtEmailError.Text = "Invalid email format";
                TxtEmailError.Visibility = Visibility.Visible;
                return false;
            }

            // Check uniqueness
            int? excludeId = _isEditMode ? _editingUser?.Id : null;
            if (!_userService.IsEmailUnique(email, excludeId))
            {
                TxtEmailError.Text = "This email is already registered";
                TxtEmailError.Visibility = Visibility.Visible;
                return false;
            }

            TxtEmailError.Visibility = Visibility.Collapsed;
            return true;
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Force lowercase
            int caretIndex = txtUsername.CaretIndex;
            string text = txtUsername.Text;
            string lowered = text.ToLower();

            // Remove invalid characters
            lowered = System.Text.RegularExpressions.Regex.Replace(lowered, @"[^a-z0-9._]", "");

            if (text != lowered)
            {
                txtUsername.Text = lowered;
                txtUsername.CaretIndex = Math.Min(caretIndex, lowered.Length);
            }

            ValidateUsername();
        }

        private bool ValidateUsername()
        {
            string username = txtUsername.Text?.Trim();

            if (string.IsNullOrEmpty(username))
            {
                TxtUsernameError.Text = "Username is required";
                TxtUsernameError.Visibility = Visibility.Visible;
                return false;
            }

            if (username.Length < 3)
            {
                TxtUsernameError.Text = "Username must be at least 3 characters";
                TxtUsernameError.Visibility = Visibility.Visible;
                return false;
            }

            // Check for valid characters
            if (!System.Text.RegularExpressions.Regex.IsMatch(username, @"^[a-z][a-z0-9._]*$"))
            {
                TxtUsernameError.Text = "Username must start with a letter and contain only lowercase letters, numbers, dots, or underscores";
                TxtUsernameError.Visibility = Visibility.Visible;
                return false;
            }

            // Check uniqueness
            int? excludeId = _isEditMode ? _editingUser?.Id : null;
            if (!_userService.IsUsernameUnique(username, excludeId))
            {
                TxtUsernameError.Text = "This username is already taken";
                TxtUsernameError.Visibility = Visibility.Visible;
                return false;
            }

            TxtUsernameError.Visibility = Visibility.Collapsed;
            return true;
        }

        private bool ValidatePassword()
        {
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            // In edit mode, password is optional
            if (_isEditMode && string.IsNullOrEmpty(password))
            {
                TxtPasswordError.Visibility = Visibility.Collapsed;
                return true;
            }

            if (!_isEditMode && string.IsNullOrEmpty(password))
            {
                TxtPasswordError.Text = "Password is required";
                TxtPasswordError.Visibility = Visibility.Visible;
                return false;
            }

            if (!string.IsNullOrEmpty(password))
            {
                if (password.Length < 8)
                {
                    TxtPasswordError.Text = "Password must be at least 8 characters";
                    TxtPasswordError.Visibility = Visibility.Visible;
                    return false;
                }

                if (!Regex.IsMatch(password, @"[A-Z]"))
                {
                    TxtPasswordError.Text = "Password must contain at least one uppercase letter";
                    TxtPasswordError.Visibility = Visibility.Visible;
                    return false;
                }

                if (!Regex.IsMatch(password, @"[a-z]"))
                {
                    TxtPasswordError.Text = "Password must contain at least one lowercase letter";
                    TxtPasswordError.Visibility = Visibility.Visible;
                    return false;
                }

                if (!Regex.IsMatch(password, @"[0-9]"))
                {
                    TxtPasswordError.Text = "Password must contain at least one number";
                    TxtPasswordError.Visibility = Visibility.Visible;
                    return false;
                }

                // Check password history (edit mode only)
                if (_isEditMode && _editingUser != null)
                {
                    // This check is done in the service, but we can provide early feedback
                }
            }

            TxtPasswordError.Visibility = Visibility.Collapsed;
            return true;
        }

        private bool ValidateConfirmPasswordField()
        {
            string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // If no new password entered in edit mode, skip validation
            if (_isEditMode && string.IsNullOrEmpty(password))
            {
                TxtConfirmPasswordError.Visibility = Visibility.Collapsed;
                return true;
            }

            if (!string.IsNullOrEmpty(password) && password != confirmPassword)
            {
                TxtConfirmPasswordError.Text = "Passwords do not match";
                TxtConfirmPasswordError.Visibility = Visibility.Visible;
                return false;
            }

            TxtConfirmPasswordError.Visibility = Visibility.Collapsed;
            return true;
        }

        private bool ValidateForm()
        {
            bool isValid = true;

            if (!ValidateFullName()) isValid = false;
            if (!ValidateUsername()) isValid = false;
            if (!ValidateEmail()) isValid = false;
            if (!ValidatePassword()) isValid = false;
            if (!ValidateConfirmPasswordField()) isValid = false;

            return isValid;
        }

        #endregion

        #region Save

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Please fix the errors before saving.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string fullName = txtFullName.Text.Trim();
                string username = txtUsername.Text.Trim();
                string email = txtEmail.Text.Trim();
                string password = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
                string role = (cbRole.SelectedItem as ComboBoxItem)?.Content.ToString();
                string status = (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString();

                // Save profile image
                string savedImagePath = SaveProfileImage();

                var currentUser = SessionManager.CurrentUser;

                if (_isEditMode)
                {
                    // Update existing user
                    _editingUser.FullName = fullName;
                    _editingUser.Username = username;
                    _editingUser.Email = email;
                    _editingUser.Role = role;
                    _editingUser.Status = status;
                    _editingUser.ModifiedAt = DateTime.Now;
                    _editingUser.ModifiedBy = currentUser?.FullName ?? "System";

                    // Update profile image
                    if (_imageRemoved)
                    {
                        _editingUser.ProfileImagePath = null;
                    }
                    else if (!string.IsNullOrEmpty(savedImagePath))
                    {
                        _editingUser.ProfileImagePath = savedImagePath;
                    }

                    // Update password if provided
                    if (!string.IsNullOrEmpty(password))
                    {
                        bool passwordChanged = _userService.ChangePassword(
                            _editingUser.Id,
                            password,
                            currentUser?.FullName ?? "System"
                        );

                        if (!passwordChanged)
                        {
                            MessageBox.Show(
                                "The new password cannot be the same as a recently used password. Please choose a different password.",
                                "Password History",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            return;
                        }
                    }

                    _userService.UpdateUser(_editingUser);
                    User = _editingUser;

                    MessageBox.Show("User updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Create new user
                    var newUser = new User
                    {
                        UserCode = _userService.GenerateUserCode(),
                        Username = username,
                        FullName = fullName,
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                        Role = role,
                        Status = status,
                        DateCreated = DateTime.Now,
                        CreatedBy = currentUser?.FullName ?? "System",
                        PasswordChangedAt = DateTime.Now,
                        ProfileImagePath = savedImagePath
                    };

                    _userService.AddUser(newUser);
                    User = newUser;

                    MessageBox.Show(
                        $"User created successfully.\n\n" +
                        $"User ID: {newUser.UserCode}\n" +
                        $"Username: {newUser.Username}\n" +
                        $"Email: {newUser.Email}",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string SaveProfileImage()
        {
            if (string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath))
                return null;

            // Don't re-save if it's the same image (edit mode)
            if (_isEditMode && _imagePath == _editingUser?.ProfileImagePath)
                return _imagePath;

            try
            {
                string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                string imagesFolder = Path.Combine(appFolder, "UserImages");

                if (!Directory.Exists(imagesFolder))
                    Directory.CreateDirectory(imagesFolder);

                string ext = Path.GetExtension(_imagePath);
                string fileName = $"user_{Guid.NewGuid()}{ext}";
                string destPath = Path.Combine(imagesFolder, fileName);

                File.Copy(_imagePath, destPath, true);
                return destPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving profile image: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Window Controls

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion
    }
}
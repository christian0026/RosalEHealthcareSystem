using Microsoft.Win32;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditUserWindow : Window
    {
        private string imagePath = "";
        private bool isEditMode = false;
        private User _editingUser;
        private readonly RosalEHealthcareDbContext _db;
        private readonly UserService _userService;

        public User User { get; private set; } // will contain saved user on success

        public AddEditUserWindow()
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            _userService = new UserService(_db);

            cbRole.SelectedIndex = 0;
            cbStatus.SelectedIndex = 0;
        }

        public AddEditUserWindow(Core.Models.User user) : this()
        {
            isEditMode = true;
            _editingUser = user;

            txtFullName.Text = user.FullName;
            txtEmail.Text = user.Email;

            // role
            foreach (var item in cbRole.Items)
            {
                if ((item as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() == user.Role)
                {
                    cbRole.SelectedItem = item;
                    break;
                }
            }

            // status
            foreach (var item in cbStatus.Items)
            {
                if ((item as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() == user.Status)
                {
                    cbStatus.SelectedItem = item;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(user.ProfileImagePath))
            {
                try
                {
                    ProfileImage.Source = new BitmapImage(new Uri(user.ProfileImagePath, UriKind.RelativeOrAbsolute));
                    imagePath = user.ProfileImagePath;
                }
                catch { /* ignore load failure */ }
            }
        }

        private void UploadProfile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg"
            };

            if (dlg.ShowDialog() == true)
            {
                imagePath = dlg.FileName;
                ProfileImage.Source = new BitmapImage(new Uri(imagePath));
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            pwdPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password.Trim();
            string role = (cbRole.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "";
            string status = (cbStatus.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please fill out name and email fields.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!isEditMode && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter a password for the new user.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(role) || string.IsNullOrWhiteSpace(status))
            {
                MessageBox.Show("Please select role and status.", "Missing Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save uploaded image to application folder (UserImages)
            string savedImagePath = null;
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    string appFolder = AppDomain.CurrentDomain.BaseDirectory;
                    string imagesFolder = Path.Combine(appFolder, "UserImages");
                    if (!Directory.Exists(imagesFolder)) Directory.CreateDirectory(imagesFolder);

                    string ext = Path.GetExtension(imagePath);
                    string fileName = $"user_{Guid.NewGuid()}{ext}";
                    string dest = Path.Combine(imagesFolder, fileName);

                    File.Copy(imagePath, dest, true);
                    savedImagePath = dest;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save profile image: " + ex.Message, "Image Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            if (isEditMode)
            {
                _editingUser.FullName = name;
                _editingUser.Email = email;
                _editingUser.Role = role;
                _editingUser.Status = status;
                if (!string.IsNullOrEmpty(savedImagePath)) _editingUser.ProfileImagePath = savedImagePath;

                // if password provided, update password hash
                if (!string.IsNullOrWhiteSpace(password))
                {
                    _editingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }

                _userService.UpdateUser(_editingUser);
                User = _editingUser;

                MessageBox.Show("User updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // create new user
                var newUser = new Core.Models.User
                {
                    UserCode = $"USR-{new Random().Next(100, 999)}",
                    FullName = name,
                    Email = email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                    Role = role,
                    Status = status,
                    LastLogin = null,
                    ProfileImagePath = savedImagePath
                };

                _userService.AddUser(newUser);
                User = newUser;

                MessageBox.Show("User added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}

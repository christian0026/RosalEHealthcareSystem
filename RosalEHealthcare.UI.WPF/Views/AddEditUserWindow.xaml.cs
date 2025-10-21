using Microsoft.Win32;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using BCrypt.Net;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditUserWindow : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly bool isEditMode = false;
        private readonly User editingUser;
        private string imagePath;

        public AddEditUserWindow()
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            cbRole.SelectedIndex = 0;
            cbStatus.SelectedIndex = 0;
        }

        public AddEditUserWindow(User user)
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            isEditMode = true;
            editingUser = user;

            txtFullName.Text = user.FullName;
            txtEmail.Text = user.Email;

            foreach (ComboBoxItem roleItem in cbRole.Items)
            {
                if (roleItem.Content.ToString() == user.Role)
                {
                    cbRole.SelectedItem = roleItem;
                    break;
                }
            }

            foreach (ComboBoxItem statusItem in cbStatus.Items)
            {
                if (statusItem.Content.ToString() == user.Status)
                {
                    cbStatus.SelectedItem = statusItem;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(user.ProfileImagePath) && File.Exists(user.ProfileImagePath))
            {
                ProfileImage.Source = new BitmapImage(new Uri(user.ProfileImagePath));
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
            pwdPlaceholder.Visibility = string.IsNullOrWhiteSpace(txtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string name = txtFullName.Text.Trim();
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Password.Trim();
            string role = (cbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";
            string status = (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Active";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please fill all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!isEditMode && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required for new user.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (isEditMode)
            {
                // Update existing user
                editingUser.FullName = name;
                editingUser.Email = email;
                editingUser.Role = role;
                editingUser.Status = status;

                if (!string.IsNullOrWhiteSpace(password))
                {
                    editingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                }

                if (!string.IsNullOrEmpty(imagePath))
                {
                    editingUser.ProfileImagePath = imagePath;
                }

                _db.Entry(editingUser).State = System.Data.Entity.EntityState.Modified;
                _db.SaveChanges();

                MessageBox.Show("User updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Create new user
                var hashed = BCrypt.Net.BCrypt.HashPassword(password);
                var newUser = new User
                {
                    FullName = name,
                    Email = email,
                    PasswordHash = hashed,
                    Role = role,
                    Status = status,
                    ProfileImagePath = imagePath
                };

                _db.Users.Add(newUser);
                _db.SaveChanges();

                MessageBox.Show("User added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}

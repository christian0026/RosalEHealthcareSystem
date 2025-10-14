using BCrypt.Net;
using HarfBuzzSharp;
using Microsoft.Win32;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditUserWindow : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private bool isEditMode = false;
        private string imagePath = "";
        private User _editingUser;

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
            _editingUser = user;

            txtFullName.Text = user.FullName;
            txtEmail.Text = user.Email;

            foreach (ComboBoxItem roleItem in cbRole.Items)
                if (roleItem.Content.ToString() == user.Role)
                    cbRole.SelectedItem = roleItem;

            cbStatus.SelectedIndex = 0; // Database has no status field yet
        }

        private void UploadProfile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg" };
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
            string role = (cbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Please fill all fields.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!isEditMode && string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required for new user.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isEditMode)
            {
                _editingUser.FullName = name;
                _editingUser.Email = email;
                _editingUser.Role = role;

                _db.Users.Update(_editingUser);
                _db.SaveChanges();
                MessageBox.Show("User updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var hashed = BCrypt.Net.BCrypt.HashPassword(password);
                var newUser = new User
                {
                    FullName = name,
                    Email = email,
                    PasswordHash = hashed,
                    Role = role
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

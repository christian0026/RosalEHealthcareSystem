using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class UserManagementView : UserControl
    {
        private readonly RosalEHealthcareDbContext _db;

        public UserManagementView()
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            LoadUsersFromDatabase();
            RefreshCounts();
        }

        private void LoadUsersFromDatabase()
        {
            try
            {
                var users = _db.Users.ToList();
                UsersDataGrid.ItemsSource = users.Select(u => new UserModel
                {
                    UserID = "USR-" + u.Id.ToString("D3"),
                    Name = u.FullName,
                    Email = u.Email,
                    Role = u.Role,
                    Status = "Active",
                    LastLogin = "N/A",
                    ProfileImage = ""
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}");
            }
        }

        private void RefreshCounts()
        {
            try
            {
                var users = _db.Users.ToList();
                TotalUsersText.Text = users.Count.ToString();
                DoctorsCountText.Text = users.Count(u => u.Role == "Doctor").ToString();
                ReceptionistsCountText.Text = users.Count(u => u.Role == "Receptionist").ToString();
                AdminsCountText.Text = users.Count(u => u.Role == "Administrator").ToString();
            }
            catch { }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddEditUserWindow();
            if (addUserWindow.ShowDialog() == true)
            {
                LoadUsersFromDatabase();
                RefreshCounts();
            }
        }

        private void ViewUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is UserModel user)
                MessageBox.Show($"{user.Name}\nRole: {user.Role}\nEmail: {user.Email}\nStatus: {user.Status}", "User Info");
        }

        private void EditUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is UserModel user)
            {
                var dbUser = _db.Users.FirstOrDefault(u => u.Email == user.Email);
                if (dbUser != null)
                {
                    var dialog = new AddEditUserWindow(dbUser);
                    if (dialog.ShowDialog() == true)
                    {
                        LoadUsersFromDatabase();
                        RefreshCounts();
                    }
                }
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is UserModel user)
            {
                var dbUser = _db.Users.FirstOrDefault(u => u.Email == user.Email);
                if (dbUser != null && MessageBox.Show($"Delete {dbUser.FullName}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    _db.Users.Remove(dbUser);
                    _db.SaveChanges();
                    LoadUsersFromDatabase();
                    RefreshCounts();
                }
            }
        }

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string search = SearchBox.Text.ToLower();
            string role = (RoleFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Roles";
            string status = (StatusFilter.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "All Status";

            var filtered = _db.Users
                .Where(u =>
                    (string.IsNullOrEmpty(search) || u.FullName.ToLower().Contains(search) || u.Email.ToLower().Contains(search)) &&
                    (role == "All Roles" || u.Role == role))
                .ToList();

            UsersDataGrid.ItemsSource = filtered.Select(u => new UserModel
            {
                UserID = "USR-" + u.Id.ToString("D3"),
                Name = u.FullName,
                Email = u.Email,
                Role = u.Role,
                Status = "Active",
                LastLogin = "N/A",
                ProfileImage = ""
            }).ToList();
        }
    }

    public class UserModel
    {
        public string UserID { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string LastLogin { get; set; }
        public string ProfileImage { get; set; }
    }
}

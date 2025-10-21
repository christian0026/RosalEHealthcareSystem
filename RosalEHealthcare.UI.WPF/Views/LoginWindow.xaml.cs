using MahApps.Metro.Controls;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LoginWindow : MetroWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
            UpdatePasswordPlaceholder();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            LoginUser();
        }

        // Allow Enter to submit the form
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LoginUser();
            }
        }

        private void LoginUser()
        {
            string email = txtUsername.Text?.Trim() ?? "";
            string password = txtPassword.Password?.Trim() ?? "";
            string role = rbAdmin.IsChecked == true ? "Administrator" :
                          rbDoctor.IsChecked == true ? "Doctor" : "Receptionist";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both email and password.", "Login", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var userService = new UserService(db);
                    var user = db.Users.FirstOrDefault(u => u.Email == email);

                    if (user != null && user.Role == role && userService.ValidateUser(email, password))
                    {
                        // Open dashboard based on role
                        if (user.Role == "Administrator")
                        {
                            var dashboard = new AdminDashboard();
                            dashboard.Show();
                        }
                        else if (user.Role == "Doctor")
                        {
                            var dashboard = new DoctorDashboard();
                            dashboard.Show();
                        }
                        else if (user.Role == "Receptionist")
                        {
                            var dashboard = new ReceptionistDashboard();
                            dashboard.Show();
                        }

                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Invalid email, password, or role.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                string details = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Error during login:\n{details}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            if (pwdPlaceholder == null || txtPassword == null) return;
            pwdPlaceholder.Visibility = string.IsNullOrEmpty(txtPassword.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}

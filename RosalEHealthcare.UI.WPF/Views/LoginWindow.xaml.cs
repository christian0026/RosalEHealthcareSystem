using MahApps.Metro.Controls;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                LoginUser();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window by clicking anywhere
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
                    var user = userService.GetByEmail(email);

                    if (user != null && user.Role == role && userService.ValidateUser(email, password))
                    {
                        // SET SESSION MANAGER
                        SessionManager.CurrentUser = user;

                        // Update last login
                        user.LastLogin = DateTime.Now;
                        userService.UpdateUser(user);

                        if (user.Role == "Administrator")
                        {
                            var dashboard = new AdminDashboard(user);
                            dashboard.Show();
                        }
                        else if (user.Role == "Doctor")
                        {
                            var dashboard = new DoctorDashboard(user);
                            dashboard.Show();
                        }
                        else // Receptionist
                        {
                            var dashboard = new ReceptionistDashboard(user);
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
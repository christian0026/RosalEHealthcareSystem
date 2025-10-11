using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LoginWindow : MahApps.Metro.Controls.MetroWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
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
                        MessageBox.Show($"Welcome {user.FullName}!", "Login Successful", MessageBoxButton.OK, MessageBoxImage.Information);

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
                        MessageBox.Show("Invalid email, password, or role.", "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error during login: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

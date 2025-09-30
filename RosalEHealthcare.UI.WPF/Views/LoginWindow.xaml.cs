using System.Windows;
using MahApps.Metro.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LoginWindow : MetroWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text?.Trim() ?? "";
            string password = txtPassword.Password?.Trim() ?? "";
            string role = rbAdmin.IsChecked == true ? "Administrator" :
                          rbDoctor.IsChecked == true ? "Doctor" : "Receptionist";

            if (username == "admin" && password == "1234" && role == "Administrator")
            {
                MessageBox.Show("Login successful!", "Welcome", MessageBoxButton.OK, MessageBoxImage.Information);

                // ✅ Open Admin Dashboard
                var dashboard = new AdminDashboard();
                dashboard.Show();

                // ✅ Close the login window
                this.Close();
            }
            else if (username == "doctor" && password == "1234" && role == "Doctor")
            {
                var dashboard = new DoctorDashboard();
                dashboard.Show();
                this.Close();
            }
            else if (username == "receptionist" && password == "1234" && role == "Receptionist")
            {
                var dashboard = new ReceptionistDashboard();
                dashboard.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid credentials. Please try again.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

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

            // Placeholder auth — replace with DB logic later
            if (username == "admin" && password == "1234" && role == "Administrator")
            {
                MessageBox.Show("Login successful!", "Welcome", MessageBoxButton.OK, MessageBoxImage.Information);
                // TODO: Launch Dashboard window
            }
            else
            {
                MessageBox.Show("Invalid credentials. Please try again.", "Login failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

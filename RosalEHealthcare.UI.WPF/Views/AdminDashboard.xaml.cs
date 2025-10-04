using MahApps.Metro.Controls;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminDashboard : MetroWindow
    {
        public AdminDashboard()
        {
            InitializeComponent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Admin Dashboard";

            // Show built-in dashboard, hide subpages
            DashboardPanel.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
            MainContent.Content = null;
        }

        private void PatientManagement_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Patient Management";

            // Show subpage, hide dashboard
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            MainContent.Content = new PatientManagementView();
        }

        private void MedicineInventory_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Medicine Inventory";

            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            MainContent.Content = new MedicineInventory();
        }

    }
}

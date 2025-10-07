using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminDashboard : MetroWindow
    {
        private Button _activeButton;

        public AdminDashboard()
        {
            InitializeComponent();
            SetActiveButton(BtnDashboard); // Default active highlight
        }

        private void SetActiveButton(Button clickedButton)
        {
            // Reset all sidebar buttons
            BtnDashboard.Style = (Style)FindResource("SidebarButton");
            BtnPatientManagement.Style = (Style)FindResource("SidebarButton");
            BtnMedicineInventory.Style = (Style)FindResource("SidebarButton");
            BtnUserManagement.Style = (Style)FindResource("SidebarButton");
            BtnReports.Style = (Style)FindResource("SidebarButton");
            BtnSettings.Style = (Style)FindResource("SidebarButton");

            // Apply the active style to the clicked button
            clickedButton.Style = (Style)FindResource("SidebarButtonActive");

            _activeButton = clickedButton;
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Admin Dashboard";
            DashboardPanel.Visibility = Visibility.Visible;
            MainContent.Visibility = Visibility.Collapsed;
            SetActiveButton(BtnDashboard);
        }

        private void PatientManagement_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Patient Management";
            MainContent.Content = new PatientManagementView();
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnPatientManagement);
        }

        private void MedicineInventory_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Medicine Inventory";
            MainContent.Content = new MedicineInventory();
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnMedicineInventory);
        }

        private void UserManagementView_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "UserManagement";
            MainContent.Content = new UserManagementView();
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnUserManagement);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}

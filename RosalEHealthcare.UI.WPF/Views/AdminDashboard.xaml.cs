using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminDashboard : MetroWindow
    {
        private Button _activeButton;
        private User _currentUser;

        public AdminDashboard()
        {
            InitializeComponent();
            SetActiveButton(BtnDashboard); // Default active highlight
        }

        // Constructor to receive logged-in user
        public AdminDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        // Apply user info to UI
        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            // Show full name or fallback to email
            var fullName = string.IsNullOrWhiteSpace(_currentUser.FullName)
                ? _currentUser.Email
                : _currentUser.FullName;

            // These elements are defined in your XAML
            TxtUserFullName.Text = fullName;
            TxtUserRole.Text = _currentUser.Role ?? "Administrator";

            // Load profile image if exists
            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                try
                {
                    var image = new BitmapImage(new Uri(_currentUser.ProfileImagePath, UriKind.RelativeOrAbsolute));
                    ProfileEllipse.Fill = new ImageBrush(image) { Stretch = Stretch.UniformToFill };
                }
                catch
                {
                    ProfileEllipse.Fill = Brushes.LightGray;
                }
            }
            else
            {
                ProfileEllipse.Fill = Brushes.LightGray;
            }
        }

        // Sets sidebar active state
        private void SetActiveButton(Button clickedButton)
        {
            BtnDashboard.Style = (Style)FindResource("SidebarButton");
            BtnPatientManagement.Style = (Style)FindResource("SidebarButton");
            BtnMedicineInventory.Style = (Style)FindResource("SidebarButton");
            BtnUserManagement.Style = (Style)FindResource("SidebarButton");
            BtnReports.Style = (Style)FindResource("SidebarButton");
            BtnSettings.Style = (Style)FindResource("SidebarButton");

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
            txtPageTitle.Text = "User Management";
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

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
            ShowDashboard(); // Default view
            SetActiveButton(BtnDashboard); // Highlight dashboard button
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

            var fullName = string.IsNullOrWhiteSpace(_currentUser.FullName)
                ? _currentUser.Email
                : _currentUser.FullName;

            TxtUserFullName.Text = fullName;
            TxtUserRole.Text = _currentUser.Role ?? "Administrator";

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

        // Utility: hide all panels to prevent overlap
        private void HideAllContent()
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Collapsed;
        }

        // Sidebar button highlight
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

        // Show only dashboard panel
        private void ShowDashboard()
        {
            txtPageTitle.Text = "Admin Dashboard";
            HideAllContent();
            DashboardPanel.Visibility = Visibility.Visible;
            SetActiveButton(BtnDashboard);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void PatientManagement_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Patient Management";
            HideAllContent();
            MainContent.Content = new PatientManagementView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnPatientManagement);
        }

        private void MedicineInventory_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Medicine Inventory";
            HideAllContent();
            MainContent.Content = new MedicineInventory();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnMedicineInventory);
        }

        private void UserManagementView_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "User Management";
            HideAllContent();
            MainContent.Content = new UserManagementView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnUserManagement);
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Reports & Analysis";
            HideAllContent();
            // Replace with your Reports view if available
            // MainContent.Content = new ReportsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnReports);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "System Settings";
            HideAllContent();
            // Replace with your Settings view if available
            // MainContent.Content = new SettingsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnSettings);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            Close();
        }
    }
}

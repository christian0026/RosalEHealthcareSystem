using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ReceptionistDashboard : MetroWindow
    {
        private User _currentUser;
        private Button _activeButton;

        public ReceptionistDashboard()
        {
            InitializeComponent();
            SetActiveButton(BtnDashboard);
        }

        // Constructor that receives logged-in user
        public ReceptionistDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Receptionist";

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                try
                {
                    var img = new BitmapImage(new Uri(_currentUser.ProfileImagePath, UriKind.RelativeOrAbsolute));
                    ProfileEllipse.Fill = new ImageBrush(img) { Stretch = Stretch.UniformToFill };
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

        // Window Control Events
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                btnMaximize.Content = "❐";
                this.BorderThickness = new Thickness(0);
            }
            else if (this.WindowState == WindowState.Normal)
            {
                btnMaximize.Content = "□";
                this.BorderThickness = new Thickness(0);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Ignore exception when window is maximized
                }
            }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ToggleMaximize()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                btnMaximize.Content = "□";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                btnMaximize.Content = "❐";
            }
        }

        // Utility: hide all panels to prevent overlap
        private void HideAllContent()
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            MainContent.Visibility = Visibility.Collapsed;
        }

        private void SetActiveButton(Button clickedButton)
        {
            BtnDashboard.Style = (Style)FindResource("SidebarButton");
            BtnPatientRegistration.Style = (Style)FindResource("SidebarButton");
            BtnAppointmentManagement.Style = (Style)FindResource("SidebarButton");
            BtnSettings.Style = (Style)FindResource("SidebarButton");

            clickedButton.Style = (Style)FindResource("SidebarButtonActive");
            _activeButton = clickedButton;
        }

        private void ShowDashboard()
        {
            txtPageTitle.Text = "Receptionist Dashboard";
            HideAllContent();
            DashboardPanel.Visibility = Visibility.Visible;
            SetActiveButton(BtnDashboard);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void PatientRegistration_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Patient Registration";
            HideAllContent();
            MainContent.Content = new PatientRegistrationView(_currentUser);
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnPatientRegistration);
        }

        private void AppointmentManagement_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Appointment Management";
            HideAllContent();
            MainContent.Content = new AppointmentManagementView(_currentUser);
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnAppointmentManagement);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "System Settings";
            HideAllContent();
            // MainContent.Content = new SettingsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnSettings);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?",
                                         "Confirm Logout",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }
    }
}
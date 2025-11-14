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
    public partial class DoctorDashboard : MetroWindow
    {
        private User _currentUser;
        private Button _activeButton;

        public DoctorDashboard()
        {
            InitializeComponent();
            SetActiveButton(BtnDashboard);
        }

        public DoctorDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Doctor";

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
            BtnPatientRecords.Style = (Style)FindResource("SidebarButton");
            BtnAppointmentLists.Style = (Style)FindResource("SidebarButton");
            BtnPrescription.Style = (Style)FindResource("SidebarButton");
            BtnMedicalReports.Style = (Style)FindResource("SidebarButton");
            BtnSettings.Style = (Style)FindResource("SidebarButton");

            clickedButton.Style = (Style)FindResource("SidebarButtonActive");
            _activeButton = clickedButton;
        }

        private void ShowDashboard()
        {
            txtPageTitle.Text = "Doctor Dashboard";
            HideAllContent();
            DashboardPanel.Visibility = Visibility.Visible;
            SetActiveButton(BtnDashboard);
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void PatientRecords_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Patient Records";
            HideAllContent();
            MainContent.Content = new DoctorPatientRecords();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnPatientRecords);
        }

        private void AppointmentLists_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Appointment Lists";
            HideAllContent();
            MainContent.Content = new DoctorAppointmentLists();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnAppointmentLists);
        }

        private void Prescription_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Prescription Management";
            HideAllContent();
            MainContent.Content = new DoctorPrescriptionManagement();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnPrescription);
        }

        private void MedicalReports_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Medical Reports";
            HideAllContent();
            MainContent.Content = new DoctorMedicalReports();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnMedicalReports);
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
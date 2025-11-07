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
    public partial class DoctorDashboard : MetroWindow
    {
        private User _currentUser;
        private Button _activeButton;

        public DoctorDashboard()
        {
            InitializeComponent();
        }

        public DoctorDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
            SetActiveButton(BtnDashboard);
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

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = null;
            txtPageTitle.Text = "Doctor Dashboard";
            SetActiveButton(BtnDashboard);
        }

        private void PatientRecords_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new DoctorPatientRecords();
            txtPageTitle.Text = "Patient Records";
            SetActiveButton(BtnPatientRecords);
        }

        private void AppointmentLists_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Appointment Lists";
            MainContent.Content = null; // placeholder for future page
            SetActiveButton(BtnAppointmentLists);
        }

        private void Prescription_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Prescription Management";
            MainContent.Content = null;
            SetActiveButton(BtnPrescription);
        }

        private void MedicalReports_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Medical Reports";
            MainContent.Content = null;
            SetActiveButton(BtnMedicalReports);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "System Settings";
            MainContent.Content = null;
            SetActiveButton(BtnSettings);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}

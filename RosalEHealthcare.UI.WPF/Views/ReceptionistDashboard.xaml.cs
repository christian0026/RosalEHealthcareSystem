using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RosalEHealthcare.UI.WPF.Views.UserSettings;
using RosalEHealthcare.UI.WPF.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ReceptionistDashboard : MetroWindow
    {
        private User _currentUser;
        private Button _activeButton;
        private RosalEHealthcareDbContext _db;
        private PatientService _patientService;
        private AppointmentService _appointmentService;
        private DashboardService _dashboardService;

        // Notification container for toasts
        private Grid _notificationContainer;

        public ReceptionistDashboard()
        {
            InitializeComponent();
            InitializeServices();
            SetActiveButton(BtnDashboard);
            LoadDashboardData();

            // Initialize notification container - CORRECTED
            InitializeNotificationContainer();
        }

        public ReceptionistDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        private void InitializeServices()
        {
            _db = new RosalEHealthcareDbContext();
            _patientService = new PatientService(_db);
            _appointmentService = new AppointmentService(_db);
            _dashboardService = new DashboardService(_db);
        }

        private void InitializeNotificationContainer()
        {
            try
            {
                // Find the root Grid in your XAML
                if (this.Content is Grid rootGrid)
                {
                    _notificationContainer = new Grid
                    {
                        IsHitTestVisible = false,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 60, 20, 0)
                    };

                    rootGrid.Children.Add(_notificationContainer);
                    Panel.SetZIndex(_notificationContainer, 9999);
                    NotificationManager.Initialize(_notificationContainer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing notifications: {ex.Message}");
            }
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

        private void LoadDashboardData()
        {
            try
            {
                // Load Summary Cards with real data
                var totalPatients = _dashboardService.GetTotalPatients();
                var todayAppointments = _dashboardService.GetTodayAppointments();
                var pendingAppointments = _dashboardService.GetPendingAppointments();
                var completedToday = GetCompletedTodayCount();

                var lastMonthPatients = _dashboardService.GetTotalPatientsLastMonth();
                var yesterdayAppointments = _dashboardService.GetYesterdayAppointments();

                // Calculate percentage changes
                var patientChange = _dashboardService.CalculatePercentageChange(totalPatients, lastMonthPatients);
                var appointmentChange = _dashboardService.CalculatePercentageChange(todayAppointments, yesterdayAppointments);

                // Update Summary Cards
                CardTotalPatients.Value = totalPatients.ToString("N0");

                // Fix: Build strings in memory, not in LINQ
                var patientTrendText = patientChange >= 0 ?
                    "+" + patientChange.ToString("0.#") + "% from last month" :
                    patientChange.ToString("0.#") + "% from last month";

                CardTotalPatients.TrendText = patientTrendText;
                CardTotalPatients.TrendColor = patientChange >= 0 ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));
                CardTotalPatients.TrendIcon = patientChange >= 0 ? "✓" : "▼";

                CardTodayAppointments.Value = todayAppointments.ToString("N0");

                var appointmentTrendText = appointmentChange >= 0 ?
                    "+" + appointmentChange.ToString("0.#") + "% from yesterday" :
                    appointmentChange.ToString("0.#") + "% from yesterday";

                CardTodayAppointments.TrendText = appointmentTrendText;
                CardTodayAppointments.TrendColor = appointmentChange >= 0 ?
                    new SolidColorBrush(Color.FromRgb(33, 150, 243)) :
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));
                CardTodayAppointments.TrendIcon = appointmentChange >= 0 ? "✓" : "▼";

                CardPendingAppointments.Value = pendingAppointments.ToString("N0");
                CardPendingAppointments.TrendText = pendingAppointments > 0 ? "Requires confirmation" : "All confirmed";
                CardPendingAppointments.TrendColor = pendingAppointments > 0 ?
                    new SolidColorBrush(Color.FromRgb(255, 152, 0)) :
                    new SolidColorBrush(Color.FromRgb(76, 175, 80));
                CardPendingAppointments.TrendIcon = pendingAppointments > 0 ? "⚠" : "✓";

                CardCompletedToday.Value = completedToday.ToString("N0");
                var efficiency = todayAppointments > 0 ? (completedToday * 100.0 / todayAppointments) : 0;
                CardCompletedToday.TrendText = "+" + efficiency.ToString("0.#") + "% efficiency";
                CardCompletedToday.TrendColor = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                CardCompletedToday.TrendIcon = "✓";

                // Load Today's Appointments
                LoadTodaysAppointments();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading dashboard data: " + ex.Message,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetCompletedTodayCount()
        {
            var today = DateTime.Today;
            return _db.Appointments.Count(a =>
                DbFunctions.TruncateTime(a.Time) == today &&
                a.Status == "COMPLETED");
        }

        private void LoadTodaysAppointments()
        {
            var today = DateTime.Today;
            var appointments = _db.Appointments
                .Where(a => DbFunctions.TruncateTime(a.Time) == today)
                .OrderBy(a => a.Time)
                .Select(a => new
                {
                    Date = a.Time,
                    Name = a.PatientName,
                    Contact = a.Contact,
                    Status = a.Status
                })
                .ToList();

            AppointmentsDataGrid.ItemsSource = appointments;
        }

        #region Window Controls

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

        #endregion

        #region Notifications
        private void InitializeNotifications()
        {
            try
            {
                var currentUser = SessionManager.CurrentUser;
                if (currentUser != null)
                {
                    // Initialize the notification bell
                    NotificationBell.Initialize(
                        currentUser.Username,
                        currentUser.Role,
                        ToastContainer
                    );

                    // Handle navigation requests from notifications
                    NotificationBell.OnNavigateRequested += NavigateToSection;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to a section based on notification action URL
        /// </summary>
        private void NavigateToSection(string section)
        {
            try
            {
                switch (section)
                {
                    case "Dashboard":
                        Dashboard_Click(null, null);
                        break;
                    case "PatientManagement":
                    case "Patients":
                        PatientManagement_Click(null, null);
                        break;
                    case "Appointments":
                        Appointments_Click(null, null);
                        break;
                    case "MedicineInventory":
                    case "Medicines":
                        // If receptionist has medicine view
                        // MedicineInventory_Click(null, null);
                        break;
                    case "Settings":
                    case "SystemSettings":
                        Settings_Click(null, null);
                        break;
                    default:
                        Dashboard_Click(null, null);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating to section: {ex.Message}");
            }
        }

        #region Navigation

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
            LoadDashboardData();
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
            MainContent.Content = new UserSystemSettingsView();
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
                SessionManager.ClearSession();
                var login = new LoginWindow();
                login.Show();
                this.Close();
            }
        }

        #endregion

        #region Quick Actions

        private void RegisterNewPatient_Click(object sender, RoutedEventArgs e)
        {
            var modal = new RegisterPatientModal(_currentUser);
            var result = modal.ShowDialog();

            if (result == true && modal.RegisteredPatient != null)
            {
                // Show success notification
                NotificationManager.ShowNewPatient(
                    modal.RegisteredPatient.FullName,
                    _currentUser?.FullName ?? "Receptionist"
                );

                // Refresh dashboard data
                LoadDashboardData();

                // Show success message - Fix: Build string in memory
                var successMessage = "Patient " + modal.RegisteredPatient.FullName +
                    " has been successfully registered!\n\nPatient ID: " +
                    modal.RegisteredPatient.PatientId +
                    "\nAppointment Status: Confirmed";

                MessageBox.Show(
                    successMessage,
                    "Registration Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void ScheduleAppointment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Schedule Appointment feature will be available after Appointment Management is finalized.",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void PrintAppointmentSlips_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print Appointment Slips feature coming soon!",
                "Info",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Stop notification polling
            NotificationBell?.Stop();

            base.OnClosing(e);
        }
    }
}
using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
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
        private NotificationService _notificationService;

        public ReceptionistDashboard()
        {
            InitializeComponent();
            InitializeServices();
            SetActiveButton(BtnDashboard);
            LoadDashboardData();
        }

        public ReceptionistDashboard(User user) : this()
        {
            _currentUser = user;
            SessionManager.CurrentUser = user;

            ApplyUserInfo();
            InitializeNotifications();
            LoadDashboardData();
        }

        private void InitializeServices()
        {
            _db = new RosalEHealthcareDbContext();
            _patientService = new PatientService(_db);
            _appointmentService = new AppointmentService(_db);
            _dashboardService = new DashboardService(_db);
            _notificationService = new NotificationService(_db);
        }

        #region Notifications

        private void InitializeNotifications()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ReceptionistDashboard: InitializeNotifications START ===");

                var currentUser = SessionManager.CurrentUser;
                if (currentUser == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: SessionManager.CurrentUser is NULL!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"Current User: {currentUser.Username}, Role: {currentUser.Role}");

                NotificationBell.Initialize(
                    currentUser.Username,
                    "Receptionist", // Explicitly setting Role
                    ToastContainer
                );

                NotificationBell.OnNavigateRequested += NavigateToSection;

                System.Diagnostics.Debug.WriteLine("=== ReceptionistDashboard: InitializeNotifications COMPLETE ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR initializing notifications: {ex.Message}");
            }
        }

        private void NavigateToSection(string section)
        {
            try
            {
                switch (section?.ToLower())
                {
                    case "dashboard":
                        Dashboard_Click(null, null);
                        break;
                    case "patientmanagement":
                    case "patients":
                        PatientRegistration_Click(null, null);
                        break;
                    case "appointments":
                        AppointmentManagement_Click(null, null);
                        break;
                    case "systemsettings":
                    case "settings":
                        Settings_Click(null, null);
                        break;
                    default:
                        Dashboard_Click(null, null);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error navigating: {ex.Message}");
            }
        }

        #endregion

        #region User Info

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Receptionist";
            TxtUserInitials.Text = GetInitials(_currentUser.FullName ?? _currentUser.Email);

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                try
                {
                    var img = new BitmapImage(new Uri(_currentUser.ProfileImagePath, UriKind.RelativeOrAbsolute));
                    ProfileEllipse.Fill = new ImageBrush(img) { Stretch = Stretch.UniformToFill };
                    TxtUserInitials.Visibility = Visibility.Collapsed;
                }
                catch
                {
                    ProfileEllipse.Fill = Brushes.Transparent;
                    TxtUserInitials.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ProfileEllipse.Fill = Brushes.Transparent;
                TxtUserInitials.Visibility = Visibility.Visible;
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "RP";
            var parts = name.Split(' ');
            if (parts.Length > 1) return (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper();
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        #endregion

        #region Load Dashboard Data

        private void LoadDashboardData()
        {
            try
            {
                var totalPatients = _dashboardService.GetTotalPatients();
                var todayAppointments = _dashboardService.GetTodayAppointments();
                var pendingAppointments = _dashboardService.GetPendingAppointments();
                var completedToday = GetCompletedTodayCount();

                var lastMonthPatients = _dashboardService.GetTotalPatientsLastMonth();
                var yesterdayAppointments = _dashboardService.GetYesterdayAppointments();

                var patientChange = _dashboardService.CalculatePercentageChange(totalPatients, lastMonthPatients);
                var appointmentChange = _dashboardService.CalculatePercentageChange(todayAppointments, yesterdayAppointments);

                CardTotalPatients.Value = totalPatients.ToString("N0");
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

        #endregion

        #region Window Controls

        private void Window_StateChanged(object sender, EventArgs e)
        {
            btnMaximize.Content = this.WindowState == WindowState.Maximized ? "❐" : "□";
            this.BorderThickness = new Thickness(0);
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) ToggleMaximize();
            else if (e.ButtonState == MouseButtonState.Pressed)
                try { this.DragMove(); } catch { }
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void btnMaximize_Click(object sender, RoutedEventArgs e) => ToggleMaximize();
        private void btnClose_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void ToggleMaximize()
        {
            this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            btnMaximize.Content = this.WindowState == WindowState.Maximized ? "❐" : "□";
        }

        #endregion

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

        private void Dashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();

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

        private void Logout_Click(object sender, RoutedEventArgs e) => LogoutHelper.Logout(this);

        #endregion

        #region Quick Actions

        private void RegisterNewPatient_Click(object sender, RoutedEventArgs e)
        {
            var modal = new RegisterPatientModal(_currentUser);
            var result = modal.ShowDialog();

            if (result == true && modal.RegisteredPatient != null)
            {
                try
                {
                    _notificationService.NotifyNewPatient(
                        modal.RegisteredPatient.FullName,
                        modal.RegisteredPatient.PatientId,
                        _currentUser?.FullName ?? "Receptionist"
                    );

                    System.Diagnostics.Debug.WriteLine($"Notification saved for new patient: {modal.RegisteredPatient.FullName}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving notification: {ex.Message}");
                }

                LoadDashboardData();

                var successMessage = "Patient " + modal.RegisteredPatient.FullName +
                    " has been successfully registered!\n\nPatient ID: " +
                    modal.RegisteredPatient.PatientId +
                    "\nAppointment Status: Confirmed";

                MessageBox.Show(successMessage, "Registration Successful",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ScheduleAppointment_Click(object sender, RoutedEventArgs e)
        {
            AppointmentManagement_Click(sender, e);
        }

        private void PrintAppointmentSlips_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print Appointment Slips feature coming soon!",
                "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Cleanup

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (Application.Current.MainWindow != this) { e.Cancel = false; return; }
            e.Cancel = true;
            LogoutHelper.ExitApplication(this);
            NotificationBell?.Stop();
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e) { base.OnClosed(e); _db?.Dispose(); }

        #endregion
    }
}
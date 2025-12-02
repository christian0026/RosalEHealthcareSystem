using LiveCharts;
using LiveCharts.Wpf;
using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using RosalEHealthcare.UI.WPF.Views.UserSettings;
using RosalEHealthcare.UI.WPF.Controls;
using RosalEHealthcare.UI.WPF.Helpers;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorDashboard : MetroWindow
    {
        #region Private Fields

        private User _currentUser;
        private Button _activeButton;

        // Services
        private readonly RosalEHealthcareDbContext _db;
        private readonly DashboardService _dashboardService;
        private readonly NotificationService _notificationService;
        private readonly MedicalHistoryService _medicalHistoryService;
        private readonly PatientService _patientService;
        private readonly AppointmentService _appointmentService;
        private readonly MedicineService _medicineService;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages = 1;
        private int _totalRecords = 0;

        // Chart data
        private SeriesCollection _illnessChartSeries;
        private SeriesCollection _appointmentStatusSeries;

        #endregion

        #region Constructor

        public DoctorDashboard()
        {
            InitializeComponent();

            // Initialize database context and services
            _db = new RosalEHealthcareDbContext();
            _dashboardService = new DashboardService(_db);
            _notificationService = new NotificationService(_db);
            _medicalHistoryService = new MedicalHistoryService(_db);
            _patientService = new PatientService(_db);
            _appointmentService = new AppointmentService(_db);
            _medicineService = new MedicineService(_db);

            SetActiveButton(BtnDashboard);

            // Load dashboard data when window loads
            this.Loaded += DoctorDashboard_Loaded;

            InitializeNotifications();
        }

        public DoctorDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        #endregion

        #region Initialization

        private async void DoctorDashboard_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Doctor";

            // Set initials
            var initials = GetInitials(_currentUser.FullName ?? _currentUser.Email);
            TxtUserInitials.Text = initials;

            // Try to load profile image
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
            if (string.IsNullOrEmpty(name)) return "?";

            var words = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                return $"{words[0][0]}{words[words.Length - 1][0]}".ToUpper();

            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        #endregion

        #region Data Loading

        private async Task LoadDashboardDataAsync()
        {
            ShowLoading(true);

            try
            {
                await Task.Run(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadSummaryCards();
                        LoadIllnessChart();
                        LoadAppointmentStatusChart();
                        LoadConsultations();
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private void LoadSummaryCards()
        {
            try
            {
                // Total Patients
                var totalPatients = _dashboardService.GetTotalPatients();
                var patientGrowth = _dashboardService.GetPatientGrowthPercentage();

                CardTotalPatients.Value = totalPatients.ToString("N0");
                CardTotalPatients.TrendText = $"{(patientGrowth >= 0 ? "+" : "")}{patientGrowth}% from last month";
                CardTotalPatients.TrendIcon = patientGrowth >= 0 ? "✓" : "▼";
                CardTotalPatients.TrendColor = patientGrowth >= 0
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));

                // Today's Appointments
                var todayAppointments = _dashboardService.GetTodayAppointments();
                var appointmentGrowth = _dashboardService.GetAppointmentGrowthPercentage();

                CardTodayAppointments.Value = todayAppointments.ToString();
                CardTodayAppointments.TrendText = $"{(appointmentGrowth >= 0 ? "+" : "")}{appointmentGrowth}% from yesterday";
                CardTodayAppointments.TrendIcon = appointmentGrowth >= 0 ? "✓" : "▼";
                CardTodayAppointments.TrendColor = appointmentGrowth >= 0
                    ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54));

                // Low Stock Medicines
                var lowStock = _dashboardService.GetLowStockMedicines();
                CardLowStock.Value = lowStock.ToString();
                CardLowStock.TrendText = "Requires attention";
                CardLowStock.TrendIcon = "⚠";
                CardLowStock.TrendColor = new SolidColorBrush(Color.FromRgb(255, 152, 0));

                // Expiring Medicines
                var expiring = _dashboardService.GetExpiringMedicines(30);
                CardExpiringMedicines.Value = expiring.ToString();
                CardExpiringMedicines.TrendText = "Expiring within 30 days";
                CardExpiringMedicines.TrendIcon = "⚠";
                CardExpiringMedicines.TrendColor = new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading summary cards: {ex.Message}");
            }
        }

        private void LoadIllnessChart()
        {
            try
            {
                var monthlyData = _dashboardService.GetMonthlyIllnessTrends(6);

                _illnessChartSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Patient Visits",
                        Values = new ChartValues<int>(monthlyData.Values),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 12,
                        Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Fill = new SolidColorBrush(Color.FromArgb(40, 76, 175, 80)),
                        StrokeThickness = 3,
                        LineSmoothness = 0.5
                    }
                };

                IllnessChart.Series = _illnessChartSeries;
                IllnessAxisX.Labels = monthlyData.Keys.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading illness chart: {ex.Message}");
            }
        }

        private void LoadAppointmentStatusChart()
        {
            try
            {
                var statusData = _dashboardService.GetAppointmentStatusDistribution();
                var percentages = _dashboardService.GetAppointmentStatusPercentages();

                // Update legend text
                TxtConfirmedPercent.Text = $"Confirmed ({percentages.GetValueOrDefault("CONFIRMED", 0)}%)";
                TxtPendingPercent.Text = $"Pending ({percentages.GetValueOrDefault("PENDING", 0)}%)";
                TxtCancelledPercent.Text = $"Cancelled ({percentages.GetValueOrDefault("CANCELLED", 0)}%)";
                TxtCompletedPercent.Text = $"Completed ({percentages.GetValueOrDefault("COMPLETED", 0)}%)";

                _appointmentStatusSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Confirmed",
                        Values = new ChartValues<int> { statusData.GetValueOrDefault("CONFIRMED", 0) },
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Pending",
                        Values = new ChartValues<int> { statusData.GetValueOrDefault("PENDING", 0) },
                        Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)),
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Cancelled",
                        Values = new ChartValues<int> { statusData.GetValueOrDefault("CANCELLED", 0) },
                        Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Completed",
                        Values = new ChartValues<int> { statusData.GetValueOrDefault("COMPLETED", 0) },
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                        DataLabels = false,
                        PushOut = 0
                    }
                };

                AppointmentStatusChart.Series = _appointmentStatusSeries;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading appointment status chart: {ex.Message}");
            }
        }

        private void LoadConsultations()
        {
            try
            {
                _totalRecords = _dashboardService.GetTotalConsultationsCount();
                _totalPages = (int)Math.Ceiling((double)_totalRecords / _pageSize);
                if (_totalPages == 0) _totalPages = 1;

                var consultations = _dashboardService.GetRecentConsultationsPaged(_currentPage, _pageSize);
                ConsultationsDataGrid.ItemsSource = consultations;

                UpdatePaginationUI();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading consultations: {ex.Message}");
            }
        }

        #endregion

        #region Pagination

        private void UpdatePaginationUI()
        {
            // Update showing info text
            int startRecord = ((_currentPage - 1) * _pageSize) + 1;
            int endRecord = Math.Min(_currentPage * _pageSize, _totalRecords);
            TxtShowingInfo.Text = $"    Showing {startRecord}-{endRecord} of {_totalRecords:N0} patients";

            // Update page buttons
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;

            // Update page number buttons
            UpdatePageButtons();

            // Update last page button
            BtnPageLast.Content = _totalPages.ToString();
            BtnPageLast.Visibility = _totalPages > 3 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdatePageButtons()
        {
            // Reset all to default style
            BtnPage1.Style = (Style)FindResource("PaginationButton");
            BtnPage2.Style = (Style)FindResource("PaginationButton");
            BtnPage3.Style = (Style)FindResource("PaginationButton");
            BtnPageLast.Style = (Style)FindResource("PaginationButton");

            // Set page numbers
            if (_totalPages <= 3)
            {
                BtnPage1.Content = "1";
                BtnPage1.Visibility = _totalPages >= 1 ? Visibility.Visible : Visibility.Collapsed;
                BtnPage2.Content = "2";
                BtnPage2.Visibility = _totalPages >= 2 ? Visibility.Visible : Visibility.Collapsed;
                BtnPage3.Content = "3";
                BtnPage3.Visibility = _totalPages >= 3 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                if (_currentPage <= 2)
                {
                    BtnPage1.Content = "1";
                    BtnPage2.Content = "2";
                    BtnPage3.Content = "3";
                }
                else if (_currentPage >= _totalPages - 1)
                {
                    BtnPage1.Content = (_totalPages - 2).ToString();
                    BtnPage2.Content = (_totalPages - 1).ToString();
                    BtnPage3.Content = _totalPages.ToString();
                }
                else
                {
                    BtnPage1.Content = (_currentPage - 1).ToString();
                    BtnPage2.Content = _currentPage.ToString();
                    BtnPage3.Content = (_currentPage + 1).ToString();
                }
            }

            // Highlight current page
            if (BtnPage1.Content.ToString() == _currentPage.ToString())
                BtnPage1.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPage2.Content.ToString() == _currentPage.ToString())
                BtnPage2.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPage3.Content.ToString() == _currentPage.ToString())
                BtnPage3.Style = (Style)FindResource("PaginationButtonActive");
            else if (BtnPageLast.Content.ToString() == _currentPage.ToString())
                BtnPageLast.Style = (Style)FindResource("PaginationButtonActive");
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadConsultations();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadConsultations();
            }
        }

        private void PageNumber_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null && int.TryParse(button.Content.ToString(), out int page))
            {
                if (page >= 1 && page <= _totalPages)
                {
                    _currentPage = page;
                    LoadConsultations();
                }
            }
        }

        #endregion

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


        #region Navigation

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

        private async void ShowDashboard()
        {
            txtPageTitle.Text = "Doctor Dashboard";
            HideAllContent();
            DashboardPanel.Visibility = Visibility.Visible;
            SetActiveButton(BtnDashboard);

            // Refresh dashboard data
            await LoadDashboardDataAsync();
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
            MainContent.Content = new UserSystemSettingsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnSettings);
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

        // ADD these missing navigation methods:

        private void PatientManagement_Click(object sender, RoutedEventArgs e)
        {
            PatientRecords_Click(sender, e);
        }

        private void Appointments_Click(object sender, RoutedEventArgs e)
        {
            AppointmentLists_Click(sender, e);
        }

        private void MedicineInventory_Click(object sender, RoutedEventArgs e)
        {
            // If doctor has medicine inventory view, navigate to it
            // Otherwise show a message or do nothing
            MessageBox.Show("Medicine Inventory view.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
                        MedicineInventory_Click(null, null);
                        break;
                    case "Prescriptions":
                        // If you have prescriptions view
                        // Prescriptions_Click(null, null);
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
        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Check if we're already in the process of closing via logout
            if (Application.Current.MainWindow != this)
            {
                // Allow close without prompting (logout animation is handling it)
                e.Cancel = false;
                return;
            }

            // Cancel the default close
            e.Cancel = true;

            // Show exit animation instead
            LogoutHelper.ExitApplication(this);

            NotificationBell?.Stop();

            base.OnClosing(e);
        }

        #region Actions

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogoutHelper.Logout(this);
        }

        private void Notification_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show notifications popup or navigate to notifications page
            MessageBox.Show("Notifications feature coming soon!", "Notifications",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ViewRecord_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag != null && int.TryParse(button.Tag.ToString(), out int recordId))
            {
                // Navigate to patient record or show details dialog
                var consultation = _medicalHistoryService.GetById(recordId);
                if (consultation != null)
                {
                    // Show view dialog or navigate to patient records
                    MessageBox.Show($"View record for consultation ID: {recordId}\n" +
                                    $"Patient: {consultation.Patient?.FullName}\n" +
                                    $"Diagnosis: {consultation.Diagnosis}\n" +
                                    $"Date: {consultation.VisitDate:MMM dd, yyyy}",
                                    "Consultation Details",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                }
            }
        }

        private void AppointmentStatusChart_DataClick(object sender, ChartPoint chartPoint)
        {
            // Handle chart click if needed
            System.Diagnostics.Debug.WriteLine($"Clicked: {chartPoint.SeriesView.Title} - Value: {chartPoint.Y}");
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Cleanup

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _db?.Dispose();
        }

        #endregion
    }


    #region Extension Methods

    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }
    }

    #endregion
}
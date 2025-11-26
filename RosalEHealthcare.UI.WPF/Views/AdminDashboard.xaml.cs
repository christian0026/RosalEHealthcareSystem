using LiveCharts;
using LiveCharts.Wpf;
using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AdminDashboard : MetroWindow
    {
        private Button _activeButton;
        private User _currentUser;
        private RosalEHealthcareDbContext _db;
        private DashboardService _dashboardService;
        private ActivityLogService _activityLogService;

        private int _activitiesCurrentPage = 1;
        private int _activitiesPageSize = 20;
        private int _activitiesTotalPages = 1;

        public AdminDashboard()
        {
            InitializeComponent();
            InitializeServices();
            ShowDashboard();
            SetActiveButton(BtnDashboard);
        }

        public AdminDashboard(User user) : this()
        {
            _currentUser = user;
            SessionManager.CurrentUser = user;
            ApplyUserInfo();
            LoadDashboardData();
        }

        private void InitializeServices()
        {
            _db = new RosalEHealthcareDbContext();
            _dashboardService = new DashboardService(_db);
            _activityLogService = new ActivityLogService(_db);
        }

        #region Load Dashboard Data

        private void LoadDashboardData()
        {
            try
            {
                LoadSummaryCards();
                LoadIllnessesChart();
                LoadRecentActivities();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSummaryCards()
        {
            // Total Patients
            var totalPatients = _dashboardService.GetTotalPatients();
            var totalPatientsLastMonth = _dashboardService.GetTotalPatientsLastMonth();
            var patientsChange = _dashboardService.CalculatePercentageChange(totalPatients, totalPatientsLastMonth);

            CardTotalPatients.Value = totalPatients.ToString("N0");
            if (patientsChange != 0)
            {
                CardTotalPatients.TrendText = $"{(patientsChange > 0 ? "+" : "")}{patientsChange:F1}% from last month";
                CardTotalPatients.TrendIcon = patientsChange > 0 ? "▲" : "▼";
                CardTotalPatients.TrendColor = patientsChange > 0 ?
                    new SolidColorBrush(Color.FromRgb(76, 175, 80)) :
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }

            // Today's Appointments
            var todayAppointments = _dashboardService.GetTodayAppointments();
            var yesterdayAppointments = _dashboardService.GetYesterdayAppointments();
            var appointmentsChange = _dashboardService.CalculatePercentageChange(todayAppointments, yesterdayAppointments);

            CardTodayAppointments.Value = todayAppointments.ToString("N0");
            if (appointmentsChange != 0)
            {
                CardTodayAppointments.TrendText = $"{(appointmentsChange > 0 ? "+" : "")}{appointmentsChange:F1}% from yesterday";
                CardTodayAppointments.TrendIcon = appointmentsChange > 0 ? "▲" : "▼";
                CardTodayAppointments.TrendColor = appointmentsChange > 0 ?
                    new SolidColorBrush(Color.FromRgb(33, 150, 243)) :
                    new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }

            // Low Stock Medicines
            var lowStockCount = _dashboardService.GetLowStockMedicines(50);
            CardLowStock.Value = lowStockCount.ToString("N0");

            // Expiring Medicines
            var expiringCount = _dashboardService.GetExpiringMedicines(30);
            CardExpiringMedicines.Value = expiringCount.ToString("N0");

            // Notification Badge
            var totalNotifications = lowStockCount + expiringCount;
            if (totalNotifications > 0)
            {
                NotificationBadge.Visibility = Visibility.Visible;
                NotificationCount.Text = totalNotifications > 99 ? "99+" : totalNotifications.ToString();
            }
        }

        #endregion

        #region Charts

        private void LoadIllnessesChart()
        {
            try
            {
                var illnesses = _dashboardService.GetTopCommonIllnesses(10).ToList();

                if (!illnesses.Any())
                {
                    // Show "No Data" message
                    return;
                }

                var values = new ChartValues<int>(illnesses.Select(i => i.Count));
                var labels = illnesses.Select(i => i.Illness).ToArray();

                IllnessesChart.Series = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Patients",
                        Values = values,
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y}"
                    }
                };

                IllnessesChart.AxisX[0].Labels = labels;
                txtChartSubtitle.Text = "Top 10 Common Illnesses";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading illnesses chart: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVisitTrendsChart()
        {
            try
            {
                var trends = _dashboardService.GetLast7DaysVisitTrends().ToList();

                if (!trends.Any())
                {
                    return;
                }

                var values = new ChartValues<int>(trends.Select(t => t.Count));
                var labels = trends.Select(t => t.Date).ToArray();

                TrendsChart.Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Patient Visits",
                        Values = values,
                        Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                        Fill = new SolidColorBrush(Color.FromArgb(50, 33, 150, 243)),
                        PointGeometrySize = 10,
                        LineSmoothness = 0.7,
                        DataLabels = true,
                        LabelPoint = point => $"{point.Y}"
                    }
                };

                TrendsChart.AxisX[0].Labels = labels;
                txtChartSubtitle.Text = "Last 7 Days Visit Trends";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading visit trends chart: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnShowIllnesses_Click(object sender, RoutedEventArgs e)
        {
            IllnessesChart.Visibility = Visibility.Visible;
            TrendsChart.Visibility = Visibility.Collapsed;

            btnShowIllnesses.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btnShowIllnesses.Foreground = Brushes.White;
            btnShowTrends.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            btnShowTrends.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));

            LoadIllnessesChart();
        }

        private void btnShowTrends_Click(object sender, RoutedEventArgs e)
        {
            IllnessesChart.Visibility = Visibility.Collapsed;
            TrendsChart.Visibility = Visibility.Visible;

            btnShowTrends.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btnShowTrends.Foreground = Brushes.White;
            btnShowIllnesses.Background = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            btnShowIllnesses.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));

            LoadVisitTrendsChart();
        }

        #endregion

        #region Recent Activities

        private void LoadRecentActivities()
        {
            try
            {
                var totalActivities = _activityLogService.GetTotalCount();
                _activitiesTotalPages = (int)Math.Ceiling((double)totalActivities / _activitiesPageSize);

                if (_activitiesTotalPages == 0) _activitiesTotalPages = 1;

                var activities = _activityLogService.GetActivitiesPaged(_activitiesCurrentPage, _activitiesPageSize)
                    .Select(a => new
                    {
                        a.ActivityType,
                        a.Description,
                        a.PerformedBy,
                        a.TimeAgo,
                        ModuleIcon = GetModuleIcon(a.Module)
                    })
                    .ToList();

                ActivitiesList.ItemsSource = activities;

                // Update pagination
                txtActivitiesPage.Text = $"Page {_activitiesCurrentPage} of {_activitiesTotalPages}";
                btnPrevActivities.IsEnabled = _activitiesCurrentPage > 1;
                btnNextActivities.IsEnabled = _activitiesCurrentPage < _activitiesTotalPages;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading activities: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetModuleIcon(string module)
        {
            switch (module?.ToUpper())
            {
                case "PATIENT": return "👤";
                case "APPOINTMENT": return "📅";
                case "PRESCRIPTION": return "💊";
                case "MEDICINE": return "📦";
                case "USER": return "👥";
                case "REPORT": return "📊";
                case "SYSTEM": return "⚙️";
                default: return "📋";
            }
        }

        private void btnPrevActivities_Click(object sender, RoutedEventArgs e)
        {
            if (_activitiesCurrentPage > 1)
            {
                _activitiesCurrentPage--;
                LoadRecentActivities();
            }
        }

        private void btnNextActivities_Click(object sender, RoutedEventArgs e)
        {
            if (_activitiesCurrentPage < _activitiesTotalPages)
            {
                _activitiesCurrentPage++;
                LoadRecentActivities();
            }
        }

        #endregion

        #region User Info

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Administrator";

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
                catch { }
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
            BtnPatientManagement.Style = (Style)FindResource("SidebarButton");
            BtnMedicineInventory.Style = (Style)FindResource("SidebarButton");
            BtnUserManagement.Style = (Style)FindResource("SidebarButton");
            BtnReports.Style = (Style)FindResource("SidebarButton");
            BtnSettings.Style = (Style)FindResource("SidebarButton");

            clickedButton.Style = (Style)FindResource("SidebarButtonActive");
            _activeButton = clickedButton;
        }

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
            LoadDashboardData();
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
            MainContent.Content = new AdminReportsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnReports);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "System Settings";
            HideAllContent();
            MainContent.Content = new SystemSettingsView();
            MainContent.Visibility = Visibility.Visible;
            SetActiveButton(BtnSettings);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogoutHelper.Logout(this);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Cancel the default close
            e.Cancel = true;

            // Show exit animation instead
            LogoutHelper.ExitApplication(this);
        }

        #endregion
    }
}
using LiveCharts;
using LiveCharts.Wpf;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.ViewModels
{
    public class DoctorDashboardViewModel : INotifyPropertyChanged
    {
        #region Private Fields

        private readonly DashboardService _dashboardService;
        private readonly NotificationService _notificationService;
        private readonly MedicalHistoryService _medicalHistoryService;
        private readonly RosalEHealthcareDbContext _db;

        private bool _isLoading;
        private string _loadingMessage;

        // Summary Card Values
        private int _totalPatients;
        private int _todayAppointments;
        private int _lowStockMedicines;
        private int _expiringMedicines;

        // Trend Values
        private string _patientTrendText;
        private string _appointmentTrendText;
        private bool _patientTrendPositive;
        private bool _appointmentTrendPositive;

        // Charts
        private SeriesCollection _illnessChartSeries;
        private SeriesCollection _appointmentStatusSeries;
        private string[] _illnessChartLabels;

        // Data Grid
        private ObservableCollection<ConsultationDisplayModel> _consultations;
        private int _currentPage = 1;
        private int _pageSize = 5;
        private int _totalPages;
        private int _totalRecords;

        // Notifications
        private int _notificationCount;

        #endregion

        #region Properties

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            set { _loadingMessage = value; OnPropertyChanged(); }
        }

        // Summary Cards
        public int TotalPatients
        {
            get => _totalPatients;
            set { _totalPatients = value; OnPropertyChanged(); OnPropertyChanged(nameof(TotalPatientsFormatted)); }
        }

        public string TotalPatientsFormatted => TotalPatients.ToString("N0");

        public int TodayAppointments
        {
            get => _todayAppointments;
            set { _todayAppointments = value; OnPropertyChanged(); }
        }

        public int LowStockMedicines
        {
            get => _lowStockMedicines;
            set { _lowStockMedicines = value; OnPropertyChanged(); }
        }

        public int ExpiringMedicines
        {
            get => _expiringMedicines;
            set { _expiringMedicines = value; OnPropertyChanged(); }
        }

        // Trends
        public string PatientTrendText
        {
            get => _patientTrendText;
            set { _patientTrendText = value; OnPropertyChanged(); }
        }

        public string AppointmentTrendText
        {
            get => _appointmentTrendText;
            set { _appointmentTrendText = value; OnPropertyChanged(); }
        }

        public bool PatientTrendPositive
        {
            get => _patientTrendPositive;
            set { _patientTrendPositive = value; OnPropertyChanged(); }
        }

        public bool AppointmentTrendPositive
        {
            get => _appointmentTrendPositive;
            set { _appointmentTrendPositive = value; OnPropertyChanged(); }
        }

        // Charts
        public SeriesCollection IllnessChartSeries
        {
            get => _illnessChartSeries;
            set { _illnessChartSeries = value; OnPropertyChanged(); }
        }

        public SeriesCollection AppointmentStatusSeries
        {
            get => _appointmentStatusSeries;
            set { _appointmentStatusSeries = value; OnPropertyChanged(); }
        }

        public string[] IllnessChartLabels
        {
            get => _illnessChartLabels;
            set { _illnessChartLabels = value; OnPropertyChanged(); }
        }

        // Consultations
        public ObservableCollection<ConsultationDisplayModel> Consultations
        {
            get => _consultations;
            set { _consultations = value; OnPropertyChanged(); }
        }

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value >= 1 && value <= TotalPages)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PageInfoText));
                    LoadConsultationsPage();
                }
            }
        }

        public int TotalPages
        {
            get => _totalPages;
            set { _totalPages = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageInfoText)); }
        }

        public int TotalRecords
        {
            get => _totalRecords;
            set { _totalRecords = value; OnPropertyChanged(); OnPropertyChanged(nameof(PageInfoText)); }
        }

        public string PageInfoText => $"Showing {((CurrentPage - 1) * _pageSize) + 1}-{Math.Min(CurrentPage * _pageSize, TotalRecords)} of {TotalRecords} patients";

        public int NotificationCount
        {
            get => _notificationCount;
            set { _notificationCount = value; OnPropertyChanged(); }
        }

        // Appointment Status Percentages for Legend
        public double ConfirmedPercentage { get; private set; }
        public double PendingPercentage { get; private set; }
        public double CancelledPercentage { get; private set; }
        public double CompletedPercentage { get; private set; }

        #endregion

        #region Constructor

        public DoctorDashboardViewModel()
        {
            _db = new RosalEHealthcareDbContext();
            _dashboardService = new DashboardService(_db);
            _notificationService = new NotificationService(_db);
            _medicalHistoryService = new MedicalHistoryService(_db);

            Consultations = new ObservableCollection<ConsultationDisplayModel>();

            // Initialize charts
            IllnessChartSeries = new SeriesCollection();
            AppointmentStatusSeries = new SeriesCollection();
        }

        #endregion

        #region Public Methods

        public async Task LoadDashboardDataAsync()
        {
            IsLoading = true;
            LoadingMessage = "Loading dashboard data...";

            try
            {
                await Task.Run(() =>
                {
                    // Load Summary Card Data
                    LoadSummaryCardData();

                    // Load Charts
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadIllnessChart();
                        LoadAppointmentStatusChart();
                    });

                    // Load Consultations
                    LoadConsultationsPage();

                    // Load Notification Count
                    NotificationCount = _notificationService.GetUnreadCount("All", "Doctor");
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public void LoadDashboardData()
        {
            try
            {
                LoadSummaryCardData();
                LoadIllnessChart();
                LoadAppointmentStatusChart();
                LoadConsultationsPage();
                NotificationCount = _notificationService.GetUnreadCount("All", "Doctor");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshData()
        {
            LoadDashboardData();
        }

        public void NextPage()
        {
            if (CurrentPage < TotalPages)
                CurrentPage++;
        }

        public void PreviousPage()
        {
            if (CurrentPage > 1)
                CurrentPage--;
        }

        public void GoToPage(int page)
        {
            CurrentPage = page;
        }

        #endregion

        #region Private Methods

        private void LoadSummaryCardData()
        {
            // Total Patients
            TotalPatients = _dashboardService.GetTotalPatients();
            var patientGrowth = _dashboardService.GetPatientGrowthPercentage();
            PatientTrendPositive = patientGrowth >= 0;
            PatientTrendText = $"{(patientGrowth >= 0 ? "+" : "")}{patientGrowth}% from last month";

            // Today's Appointments
            TodayAppointments = _dashboardService.GetTodayAppointments();
            var appointmentGrowth = _dashboardService.GetAppointmentGrowthPercentage();
            AppointmentTrendPositive = appointmentGrowth >= 0;
            AppointmentTrendText = $"{(appointmentGrowth >= 0 ? "+" : "")}{appointmentGrowth}% from yesterday";

            // Low Stock Medicines
            LowStockMedicines = _dashboardService.GetLowStockMedicines();

            // Expiring Medicines (within 30 days)
            ExpiringMedicines = _dashboardService.GetExpiringMedicines(30);
        }

        private void LoadIllnessChart()
        {
            try
            {
                var monthlyData = _dashboardService.GetMonthlyIllnessTrends(6);

                IllnessChartSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Patient Visits",
                        Values = new ChartValues<int>(monthlyData.Values),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 10,
                        Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        Fill = new SolidColorBrush(Color.FromArgb(50, 76, 175, 80)),
                        StrokeThickness = 3
                    }
                };

                IllnessChartLabels = monthlyData.Keys.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading illness chart: {ex.Message}");
                // Set empty chart on error
                IllnessChartSeries = new SeriesCollection();
                IllnessChartLabels = new string[0];
            }
        }

        private void LoadAppointmentStatusChart()
        {
            try
            {
                var statusData = _dashboardService.GetAppointmentStatusDistribution();
                var percentages = _dashboardService.GetAppointmentStatusPercentages();

                ConfirmedPercentage = percentages.ContainsKey("CONFIRMED") ? percentages["CONFIRMED"] : 0;
                PendingPercentage = percentages.ContainsKey("PENDING") ? percentages["PENDING"] : 0;
                CancelledPercentage = percentages.ContainsKey("CANCELLED") ? percentages["CANCELLED"] : 0;
                CompletedPercentage = percentages.ContainsKey("COMPLETED") ? percentages["COMPLETED"] : 0;

                OnPropertyChanged(nameof(ConfirmedPercentage));
                OnPropertyChanged(nameof(PendingPercentage));
                OnPropertyChanged(nameof(CancelledPercentage));
                OnPropertyChanged(nameof(CompletedPercentage));

                AppointmentStatusSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "Confirmed",
                        Values = new ChartValues<int> { statusData.ContainsKey("CONFIRMED") ? statusData["CONFIRMED"] : 0 },
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Pending",
                        Values = new ChartValues<int> { statusData.ContainsKey("PENDING") ? statusData["PENDING"] : 0 },
                        Fill = new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Yellow/Amber
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Cancelled",
                        Values = new ChartValues<int> { statusData.ContainsKey("CANCELLED") ? statusData["CANCELLED"] : 0 },
                        Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                        DataLabels = false,
                        PushOut = 0
                    },
                    new PieSeries
                    {
                        Title = "Completed",
                        Values = new ChartValues<int> { statusData.ContainsKey("COMPLETED") ? statusData["COMPLETED"] : 0 },
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Blue
                        DataLabels = false,
                        PushOut = 0
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading appointment status chart: {ex.Message}");
                AppointmentStatusSeries = new SeriesCollection();
            }
        }

        private void LoadConsultationsPage()
        {
            try
            {
                TotalRecords = _dashboardService.GetTotalConsultationsCount();
                TotalPages = (int)Math.Ceiling((double)TotalRecords / _pageSize);

                if (TotalPages == 0) TotalPages = 1;

                var consultations = _dashboardService.GetRecentConsultationsPaged(_currentPage, _pageSize);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Consultations.Clear();
                    foreach (var c in consultations)
                    {
                        Consultations.Add(c);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading consultations: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Cleanup

        public void Dispose()
        {
            _db?.Dispose();
        }

        #endregion
    }
}
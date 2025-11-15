using LiveCharts;
using LiveCharts.Wpf;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.ViewModels
{
    public class AdminReportsViewModel : INotifyPropertyChanged
    {
        private readonly ReportService _reportService;
        private readonly ExportService _exportService;
        private readonly ActivityLogService _activityLogService;

        #region Properties

        // Dashboard Summary Cards
        private string _totalPatients;
        public string TotalPatients
        {
            get => _totalPatients;
            set { _totalPatients = value; OnPropertyChanged(nameof(TotalPatients)); }
        }

        private string _patientsTrend;
        public string PatientsTrend
        {
            get => _patientsTrend;
            set { _patientsTrend = value; OnPropertyChanged(nameof(PatientsTrend)); }
        }

        private string _completedAppointments;
        public string CompletedAppointments
        {
            get => _completedAppointments;
            set { _completedAppointments = value; OnPropertyChanged(nameof(CompletedAppointments)); }
        }

        private string _appointmentsTrend;
        public string AppointmentsTrend
        {
            get => _appointmentsTrend;
            set { _appointmentsTrend = value; OnPropertyChanged(nameof(AppointmentsTrend)); }
        }

        private string _medicinesPrescribed;
        public string MedicinesPrescribed
        {
            get => _medicinesPrescribed;
            set { _medicinesPrescribed = value; OnPropertyChanged(nameof(MedicinesPrescribed)); }
        }

        private string _medicinesTrend;
        public string MedicinesTrend
        {
            get => _medicinesTrend;
            set { _medicinesTrend = value; OnPropertyChanged(nameof(MedicinesTrend)); }
        }

        private string _lowStockAlerts;
        public string LowStockAlerts
        {
            get => _lowStockAlerts;
            set { _lowStockAlerts = value; OnPropertyChanged(nameof(LowStockAlerts)); }
        }

        private string _lowStockMessage;
        public string LowStockMessage
        {
            get => _lowStockMessage;
            set { _lowStockMessage = value; OnPropertyChanged(nameof(LowStockMessage)); }
        }

        // Report Filters
        private string _selectedReportType;
        public string SelectedReportType
        {
            get => _selectedReportType;
            set { _selectedReportType = value; OnPropertyChanged(nameof(SelectedReportType)); }
        }

        private string _selectedDateRange;
        public string SelectedDateRange
        {
            get => _selectedDateRange;
            set
            {
                _selectedDateRange = value;
                OnPropertyChanged(nameof(SelectedDateRange));
                UpdateDateRangeBasedOnSelection();
                LoadVisitTrendsChart();
                LoadTopDiagnosesChart();
            }
        }

        private string _selectedFormat;
        public string SelectedFormat
        {
            get => _selectedFormat;
            set { _selectedFormat = value; OnPropertyChanged(nameof(SelectedFormat)); }
        }

        public ObservableCollection<string> ReportTypes { get; set; }
        public ObservableCollection<string> DateRanges { get; set; }
        public ObservableCollection<string> Formats { get; set; }

        // Weekly Appointment Analysis
        private string _scheduledCount;
        public string ScheduledCount
        {
            get => _scheduledCount;
            set { _scheduledCount = value; OnPropertyChanged(nameof(ScheduledCount)); }
        }

        private string _completedCount;
        public string CompletedCount
        {
            get => _completedCount;
            set { _completedCount = value; OnPropertyChanged(nameof(CompletedCount)); }
        }

        private string _noShowsCount;
        public string NoShowsCount
        {
            get => _noShowsCount;
            set { _noShowsCount = value; OnPropertyChanged(nameof(NoShowsCount)); }
        }

        private string _cancelledCount;
        public string CancelledCount
        {
            get => _cancelledCount;
            set { _cancelledCount = value; OnPropertyChanged(nameof(CancelledCount)); }
        }

        // Common Illness Report
        private string _hypertensionCount;
        public string HypertensionCount
        {
            get => _hypertensionCount;
            set { _hypertensionCount = value; OnPropertyChanged(nameof(HypertensionCount)); }
        }

        private string _diabetesCount;
        public string DiabetesCount
        {
            get => _diabetesCount;
            set { _diabetesCount = value; OnPropertyChanged(nameof(DiabetesCount)); }
        }

        private string _urtiCount;
        public string UrtiCount
        {
            get => _urtiCount;
            set { _urtiCount = value; OnPropertyChanged(nameof(UrtiCount)); }
        }

        private string _utiCount;
        public string UtiCount
        {
            get => _utiCount;
            set { _utiCount = value; OnPropertyChanged(nameof(UtiCount)); }
        }

        // Daily Patient Summary
        private string _newPatientsCount;
        public string NewPatientsCount
        {
            get => _newPatientsCount;
            set { _newPatientsCount = value; OnPropertyChanged(nameof(NewPatientsCount)); }
        }

        private string _followUpsCount;
        public string FollowUpsCount
        {
            get => _followUpsCount;
            set { _followUpsCount = value; OnPropertyChanged(nameof(FollowUpsCount)); }
        }

        private string _completedDailyCount;
        public string CompletedDailyCount
        {
            get => _completedDailyCount;
            set { _completedDailyCount = value; OnPropertyChanged(nameof(CompletedDailyCount)); }
        }

        private string _cancelledDailyCount;
        public string CancelledDailyCount
        {
            get => _cancelledDailyCount;
            set { _cancelledDailyCount = value; OnPropertyChanged(nameof(CancelledDailyCount)); }
        }

        // Medicine Inventory Status
        private string _totalMedicinesCount;
        public string TotalMedicinesCount
        {
            get => _totalMedicinesCount;
            set { _totalMedicinesCount = value; OnPropertyChanged(nameof(TotalMedicinesCount)); }
        }

        private string _lowStockCount;
        public string LowStockCount
        {
            get => _lowStockCount;
            set { _lowStockCount = value; OnPropertyChanged(nameof(LowStockCount)); }
        }

        private string _expiringSoonCount;
        public string ExpiringSoonCount
        {
            get => _expiringSoonCount;
            set { _expiringSoonCount = value; OnPropertyChanged(nameof(ExpiringSoonCount)); }
        }

        private string _outOfStockCount;
        public string OutOfStockCount
        {
            get => _outOfStockCount;
            set { _outOfStockCount = value; OnPropertyChanged(nameof(OutOfStockCount)); }
        }

        // Charts
        private SeriesCollection _weeklyAppointmentSeries;
        public SeriesCollection WeeklyAppointmentSeries
        {
            get => _weeklyAppointmentSeries;
            set { _weeklyAppointmentSeries = value; OnPropertyChanged(nameof(WeeklyAppointmentSeries)); }
        }

        private string[] _weeklyAppointmentLabels;
        public string[] WeeklyAppointmentLabels
        {
            get => _weeklyAppointmentLabels;
            set { _weeklyAppointmentLabels = value; OnPropertyChanged(nameof(WeeklyAppointmentLabels)); }
        }

        private SeriesCollection _inventoryStatusSeries;
        public SeriesCollection InventoryStatusSeries
        {
            get => _inventoryStatusSeries;
            set { _inventoryStatusSeries = value; OnPropertyChanged(nameof(InventoryStatusSeries)); }
        }

        private SeriesCollection _dailyAppointmentSeries;
        public SeriesCollection DailyAppointmentSeries
        {
            get => _dailyAppointmentSeries;
            set { _dailyAppointmentSeries = value; OnPropertyChanged(nameof(DailyAppointmentSeries)); }
        }

        private SeriesCollection _visitTrendsSeries;
        public SeriesCollection VisitTrendsSeries
        {
            get => _visitTrendsSeries;
            set { _visitTrendsSeries = value; OnPropertyChanged(nameof(VisitTrendsSeries)); }
        }

        private string[] _visitTrendsLabels;
        public string[] VisitTrendsLabels
        {
            get => _visitTrendsLabels;
            set { _visitTrendsLabels = value; OnPropertyChanged(nameof(VisitTrendsLabels)); }
        }

        private SeriesCollection _topDiagnosesSeries;
        public SeriesCollection TopDiagnosesSeries
        {
            get => _topDiagnosesSeries;
            set { _topDiagnosesSeries = value; OnPropertyChanged(nameof(TopDiagnosesSeries)); }
        }

        private string[] _topDiagnosesLabels;
        public string[] TopDiagnosesLabels
        {
            get => _topDiagnosesLabels;
            set { _topDiagnosesLabels = value; OnPropertyChanged(nameof(TopDiagnosesLabels)); }
        }

        // Active Report View
        private bool _isWeeklySummaryActive;
        public bool IsWeeklySummaryActive
        {
            get => _isWeeklySummaryActive;
            set
            {
                _isWeeklySummaryActive = value;
                OnPropertyChanged(nameof(IsWeeklySummaryActive));
                if (value) LoadWeeklySummaryView();
            }
        }

        private bool _isMonthlyReportActive;
        public bool IsMonthlyReportActive
        {
            get => _isMonthlyReportActive;
            set
            {
                _isMonthlyReportActive = value;
                OnPropertyChanged(nameof(IsMonthlyReportActive));
                if (value) LoadMonthlyReportView();
            }
        }

        #endregion

        #region Commands

        public ICommand WeeklySummaryCommand { get; set; }
        public ICommand MonthlyReportCommand { get; set; }
        public ICommand SearchReportCommand { get; set; }
        public ICommand ExportAllReportsCommand { get; set; }
        public ICommand ViewWeeklyAnalysisCommand { get; set; }
        public ICommand ExportWeeklyAnalysisCommand { get; set; }
        public ICommand PrintWeeklyAnalysisCommand { get; set; }
        public ICommand ViewCommonIllnessCommand { get; set; }
        public ICommand ExportCommonIllnessCommand { get; set; }
        public ICommand PrintCommonIllnessCommand { get; set; }
        public ICommand ViewDailySummaryCommand { get; set; }
        public ICommand ExportDailySummaryCommand { get; set; }
        public ICommand PrintDailySummaryCommand { get; set; }
        public ICommand ViewInventoryStatusCommand { get; set; }
        public ICommand ExportInventoryStatusCommand { get; set; }
        public ICommand PrintInventoryStatusCommand { get; set; }

        #endregion

        #region Constructor

        public AdminReportsViewModel()
        {
            var db = new RosalEHealthcareDbContext();
            _reportService = new ReportService(db);
            _exportService = new ExportService();
            _activityLogService = new ActivityLogService(db);

            InitializeCollections();
            InitializeCommands();
            LoadDashboardData();
            LoadWeeklySummaryView(); // Default view
        }

        #endregion

        #region Initialization

        private void InitializeCollections()
        {
            ReportTypes = new ObservableCollection<string>
            {
                "Appointment Summary",
                "Patient Demographics",
                "Medicine Inventory",
                "Revenue Report",
                "Daily Patient Summary",
                "Weekly Analysis"
            };

            DateRanges = new ObservableCollection<string>
            {
                "Last 7 Days",
                "Last 30 Days",
                "Last 12 Months",
                "This Week",
                "This Month",
                "This Year",
                "Custom Range"
            };

            Formats = new ObservableCollection<string>
            {
                "PDF Report",
                "Excel Spreadsheet",
                "CSV File"
            };

            // Set defaults
            SelectedReportType = "Appointment Summary";
            SelectedDateRange = "Last 7 Days";
            SelectedFormat = "PDF Report";
        }

        private void InitializeCommands()
        {
            WeeklySummaryCommand = new RelayCommand(ExecuteWeeklySummary);
            MonthlyReportCommand = new RelayCommand(ExecuteMonthlyReport);
            SearchReportCommand = new RelayCommand(ExecuteSearchReport);
            ExportAllReportsCommand = new RelayCommand(ExecuteExportAllReports);

            ViewWeeklyAnalysisCommand = new RelayCommand(ExecuteViewWeeklyAnalysis);
            ExportWeeklyAnalysisCommand = new RelayCommand(ExecuteExportWeeklyAnalysis);
            PrintWeeklyAnalysisCommand = new RelayCommand(ExecutePrintWeeklyAnalysis);

            ViewCommonIllnessCommand = new RelayCommand(ExecuteViewCommonIllness);
            ExportCommonIllnessCommand = new RelayCommand(ExecuteExportCommonIllness);
            PrintCommonIllnessCommand = new RelayCommand(ExecutePrintCommonIllness);

            ViewDailySummaryCommand = new RelayCommand(ExecuteViewDailySummary);
            ExportDailySummaryCommand = new RelayCommand(ExecuteExportDailySummary);
            PrintDailySummaryCommand = new RelayCommand(ExecutePrintDailySummary);

            ViewInventoryStatusCommand = new RelayCommand(ExecuteViewInventoryStatus);
            ExportInventoryStatusCommand = new RelayCommand(ExecuteExportInventoryStatus);
            PrintInventoryStatusCommand = new RelayCommand(ExecutePrintInventoryStatus);
        }

        #endregion

        #region Load Data Methods

        private void LoadDashboardData()
        {
            try
            {
                // Total Patients
                var totalPatients = _reportService.GetTotalPatients();
                var totalPatientsLastWeek = _reportService.GetTotalPatientsLastWeek();
                TotalPatients = totalPatients.ToString("N0");

                var patientChange = totalPatientsLastWeek > 0
                    ? Math.Round(((double)totalPatientsLastWeek / totalPatients) * 100, 0)
                    : 0;
                PatientsTrend = $"+{patientChange}% from last week";

                // Completed Appointments
                var completedAppts = _reportService.GetCompletedAppointments();
                var completedLastWeek = _reportService.GetCompletedAppointmentsLastWeek();
                CompletedAppointments = completedAppts.ToString("N0");

                var apptChange = completedAppts > 0
                    ? Math.Round(((double)completedLastWeek / completedAppts) * 100, 0)
                    : 0;
                AppointmentsTrend = $"+{apptChange}% completion rate";

                // Medicines Prescribed
                var medicinesPrescribed = _reportService.GetMedicinesPrescribed();
                var medicinesLastWeek = _reportService.GetMedicinesPrescribedLastWeek();
                MedicinesPrescribed = medicinesPrescribed.ToString("N0");

                var medicineChange = medicinesPrescribed > 0
                    ? Math.Round(((double)medicinesLastWeek / medicinesPrescribed) * 100, 0)
                    : 0;
                MedicinesTrend = $"+{medicineChange}% from last week";

                // Low Stock Alerts
                var lowStock = _reportService.GetLowStockCount();
                LowStockAlerts = lowStock.ToString();
                LowStockMessage = lowStock > 0 ? "Immediate Restock Needed" : "Stock Levels Good";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadWeeklySummaryView()
        {
            IsWeeklySummaryActive = true;
            IsMonthlyReportActive = false;

            LoadWeeklyAppointmentAnalysis();
            LoadCommonIllnessReport();
            LoadWeeklyAppointmentChart();
            LoadInventoryStatusChart();
        }

        private void LoadMonthlyReportView()
        {
            IsWeeklySummaryActive = false;
            IsMonthlyReportActive = true;

            LoadDailyPatientSummary();
            LoadMedicineInventoryStatus();
            LoadDailyAppointmentChart();
            LoadInventoryStatusChart();
        }

        private void LoadWeeklyAppointmentAnalysis()
        {
            try
            {
                var analysis = _reportService.GetWeeklyAppointmentAnalysis();

                ScheduledCount = analysis.ContainsKey("Scheduled") ? analysis["Scheduled"].ToString() : "0";
                CompletedCount = analysis.ContainsKey("Completed") ? analysis["Completed"].ToString() : "0";
                NoShowsCount = analysis.ContainsKey("No-Shows") ? analysis["No-Shows"].ToString() : "0";
                CancelledCount = analysis.ContainsKey("Cancelled") ? analysis["Cancelled"].ToString() : "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading weekly analysis: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCommonIllnessReport()
        {
            try
            {
                var illnesses = _reportService.GetCommonIllnessReport();

                HypertensionCount = GetIllnessCount(illnesses, "Hypertension");
                DiabetesCount = GetIllnessCount(illnesses, "Diabetes");
                UrtiCount = GetIllnessCount(illnesses, "URTI");
                UtiCount = GetIllnessCount(illnesses, "UTI");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading illness report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetIllnessCount(List<ChartDataPoint> illnesses, string illnessName)
        {
            var illness = illnesses.FirstOrDefault(i =>
                i.Label.IndexOf(illnessName, StringComparison.OrdinalIgnoreCase) >= 0);
            return illness?.Value.ToString() ?? "0";
        }

        private void LoadDailyPatientSummary()
        {
            try
            {
                var summary = _reportService.GetDailyPatientSummary();

                NewPatientsCount = summary.ContainsKey("New Patients") ? summary["New Patients"].ToString() : "0";
                FollowUpsCount = summary.ContainsKey("Follow-ups") ? summary["Follow-ups"].ToString() : "0";
                CompletedDailyCount = summary.ContainsKey("Completed") ? summary["Completed"].ToString() : "0";
                CancelledDailyCount = summary.ContainsKey("Cancelled") ? summary["Cancelled"].ToString() : "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily summary: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMedicineInventoryStatus()
        {
            try
            {
                var inventory = _reportService.GetMedicineInventoryStatus();

                TotalMedicinesCount = inventory.ContainsKey("Total Medicines") ? inventory["Total Medicines"].ToString() : "0";
                LowStockCount = inventory.ContainsKey("Low Stock") ? inventory["Low Stock"].ToString() : "0";
                ExpiringSoonCount = inventory.ContainsKey("Expiring Soon") ? inventory["Expiring Soon"].ToString() : "0";
                OutOfStockCount = inventory.ContainsKey("Out of Stock") ? inventory["Out of Stock"].ToString() : "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory status: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Chart Methods

        private void LoadWeeklyAppointmentChart()
        {
            try
            {
                var trends = _reportService.GetPatientVisitTrends("Last 7 Days");

                WeeklyAppointmentLabels = trends.Select(t => t.Label).ToArray();

                WeeklyAppointmentSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Appointments",
                        Values = new ChartValues<double>(trends.Select(t => t.Value)),
                        Fill = new SolidColorBrush(Color.FromArgb(50, 76, 175, 80)),
                        Stroke = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        StrokeThickness = 3,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointment chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInventoryStatusChart()
        {
            try
            {
                var inventory = _reportService.GetMedicineInventoryStatus();

                InventoryStatusSeries = new SeriesCollection
                {
                    new PieSeries
                    {
                        Title = "In Stock",
                        Values = new ChartValues<double> {
                            inventory.ContainsKey("Total Medicines")
                                ? inventory["Total Medicines"] - (inventory.ContainsKey("Low Stock") ? inventory["Low Stock"] : 0)
                                : 0
                        },
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                        DataLabels = true
                    },
                    new PieSeries
                    {
                        Title = "Low Stock",
                        Values = new ChartValues<double> {
                            inventory.ContainsKey("Low Stock") ? inventory["Low Stock"] : 0
                        },
                        Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                        DataLabels = true
                    },
                    new PieSeries
                    {
                        Title = "Out of Stock",
                        Values = new ChartValues<double> {
                            inventory.ContainsKey("Out of Stock") ? inventory["Out of Stock"] : 0
                        },
                        Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                        DataLabels = true
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading inventory chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDailyAppointmentChart()
        {
            try
            {
                var summary = _reportService.GetDailyPatientSummary();

                var completed = summary.ContainsKey("Completed") ? summary["Completed"] : 0;
                var cancelled = summary.ContainsKey("Cancelled") ? summary["Cancelled"] : 0;
                var followUps = summary.ContainsKey("Follow-ups") ? summary["Follow-ups"] : 0;
                var newPatients = summary.ContainsKey("New Patients") ? summary["New Patients"] : 0;

                DailyAppointmentSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Completed",
                        Values = new ChartValues<double> { completed },
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    },
                    new ColumnSeries
                    {
                        Title = "Follow-ups",
                        Values = new ChartValues<double> { followUps },
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    },
                    new ColumnSeries
                    {
                        Title = "New Patients",
                        Values = new ChartValues<double> { newPatients },
                        Fill = new SolidColorBrush(Color.FromRgb(156, 39, 176))
                    },
                    new ColumnSeries
                    {
                        Title = "Cancelled",
                        Values = new ChartValues<double> { cancelled },
                        Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54))
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading daily chart: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadVisitTrendsChart()
        {
            try
            {
                var trends = _reportService.GetPatientVisitTrends(SelectedDateRange);

                VisitTrendsLabels = trends.Select(t => t.Label).ToArray();

                VisitTrendsSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Patient Visits",
                        Values = new ChartValues<double>(trends.Select(t => t.Value)),
                        Fill = new SolidColorBrush(Color.FromArgb(50, 33, 150, 243)),
                        Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                        StrokeThickness = 3,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading visit trends: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTopDiagnosesChart()
        {
            try
            {
                var diagnoses = _reportService.GetTopDiagnoses(SelectedDateRange);

                TopDiagnosesLabels = diagnoses.Select(d => d.Label).ToArray();

                TopDiagnosesSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Cases",
                        Values = new ChartValues<double>(diagnoses.Select(d => d.Value)),
                        Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                    }
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading top diagnoses: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateDateRangeBasedOnSelection()
        {
            // This method updates any date-dependent data when date range changes
            // Charts are already updating via property change
        }

        #endregion

        #region Command Implementations

        private void ExecuteWeeklySummary()
        {
            LoadWeeklySummaryView();
            LogActivity("Viewed Weekly Summary Report");
        }

        private void ExecuteMonthlyReport()
        {
            LoadMonthlyReportView();
            LogActivity("Viewed Monthly Report");
        }

        private void ExecuteSearchReport()
        {
            try
            {
                var filter = new ReportFilter
                {
                    ReportType = SelectedReportType,
                    DateRange = SelectedDateRange,
                    Format = SelectedFormat,
                    Status = SessionManager.CurrentUser?.FullName ?? "Admin"
                };

                var reportData = _reportService.GenerateCustomReport(filter);

                MessageBox.Show($"Report Generated Successfully!\n\nTitle: {reportData.Title}\nStatistics: {reportData.Statistics.Count}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                LogActivity($"Generated {SelectedReportType} report");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExportAllReports()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"AllReports_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    var filter = new ReportFilter
                    {
                        ReportType = "Comprehensive Report",
                        Status = SessionManager.CurrentUser?.FullName ?? "Admin"
                    };

                    var reportData = _reportService.GenerateCustomReport(filter);
                    _exportService.ExportToCSV(reportData, dialog.FileName);

                    MessageBox.Show("All reports exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LogActivity("Exported all reports");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting reports: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteViewWeeklyAnalysis()
        {
            MessageBox.Show("Weekly Appointment Analysis Details", "View Report",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogActivity("Viewed Weekly Appointment Analysis");
        }

        private void ExecuteExportWeeklyAnalysis()
        {
            ExportReport("Weekly Appointment Analysis");
        }

        private void ExecutePrintWeeklyAnalysis()
        {
            PrintReport("Weekly Appointment Analysis");
        }

        private void ExecuteViewCommonIllness()
        {
            MessageBox.Show("Common Illness Report Details", "View Report",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogActivity("Viewed Common Illness Report");
        }

        private void ExecuteExportCommonIllness()
        {
            ExportReport("Common Illness Report");
        }

        private void ExecutePrintCommonIllness()
        {
            PrintReport("Common Illness Report");
        }

        private void ExecuteViewDailySummary()
        {
            MessageBox.Show("Daily Patient Summary Details", "View Report",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogActivity("Viewed Daily Patient Summary");
        }

        private void ExecuteExportDailySummary()
        {
            ExportReport("Daily Patient Summary");
        }

        private void ExecutePrintDailySummary()
        {
            PrintReport("Daily Patient Summary");
        }

        private void ExecuteViewInventoryStatus()
        {
            MessageBox.Show("Medicine Inventory Status Details", "View Report",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogActivity("Viewed Medicine Inventory Status");
        }

        private void ExecuteExportInventoryStatus()
        {
            ExportReport("Medicine Inventory Status");
        }

        private void ExecutePrintInventoryStatus()
        {
            PrintReport("Medicine Inventory Status");
        }

        #endregion

        #region Helper Methods

        private void ExportReport(string reportName)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"{reportName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}",
                    DefaultExt = ".csv",
                    Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*"
                };

                if (dialog.ShowDialog() == true)
                {
                    var filter = new ReportFilter
                    {
                        ReportType = reportName,
                        Status = SessionManager.CurrentUser?.FullName ?? "Admin"
                    };

                    var reportData = _reportService.GenerateCustomReport(filter);

                    if (dialog.FileName.EndsWith(".xlsx"))
                    {
                        _exportService.ExportToExcel(reportData, dialog.FileName);
                    }
                    else
                    {
                        _exportService.ExportToCSV(reportData, dialog.FileName);
                    }

                    MessageBox.Show($"{reportName} exported successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LogActivity($"Exported {reportName}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintReport(string reportName)
        {
            MessageBox.Show($"Printing {reportName}...\n\nPrint dialog will appear.", "Print",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LogActivity($"Printed {reportName}");
        }

        private void LogActivity(string description)
        {
            try
            {
                _activityLogService.LogActivity(
                    "Report",
                    description,
                    "Reports & Analysis",
                    SessionManager.CurrentUser?.FullName ?? "Admin",
                    SessionManager.CurrentUser?.Role ?? "Administrator"
                );
            }
            catch
            {
                // Silent fail for activity logging
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region RelayCommand Helper

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    #endregion
}
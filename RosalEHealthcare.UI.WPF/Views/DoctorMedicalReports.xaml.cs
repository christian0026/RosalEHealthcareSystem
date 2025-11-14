using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.Core.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorMedicalReports : UserControl
    {
        private readonly ReportService _reportService;
        private readonly RosalEHealthcareDbContext _db;

        public DoctorMedicalReports()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _reportService = new ReportService(_db);

                Loaded += (s, e) => LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Medical Reports:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // LOAD INITIAL DATA
        // ---------------------------------------------------------
        private async void LoadData()
        {
            try
            {
                await Task.Run(() =>
                {
                    var totalPatients = _reportService.GetTotalPatients();
                    var completedAppointments = _reportService.GetCompletedAppointments();
                    var medicinePrescribed = _reportService.GetMedicinesPrescribed();
                    var lowStockCount = _reportService.GetLowStockCount();
                    var totalVisits = _reportService.GetTotalVisits();
                    var totalAppointments = _reportService.GetTotalAppointments();
                    var showRate = _reportService.GetShowRate();
                    var medicineItems = _reportService.GetMedicineItemsCount();
                    var savedTemplates = _reportService.GetSavedTemplates();
                    var generatedReports = _reportService.GetGeneratedReportsCount();

                    Dispatcher.Invoke(() =>
                    {
                        txtTotalPatients.Text = totalPatients.ToString("#,##0");
                        txtCompletedAppointments.Text = completedAppointments.ToString("#,##0");
                        txtMedicinePrescribed.Text = medicinePrescribed.ToString("#,##0");
                        txtLowStockAlerts.Text = lowStockCount.ToString();

                        txtPatientHistoryTotal.Text = totalPatients.ToString("#,##0");
                        txtPatientHistoryVisits.Text = totalVisits.ToString("#,##0");

                        txtAppointmentTotal.Text = totalAppointments.ToString("#,##0");
                        txtAppointmentShowRate.Text = $"{showRate}%";

                        txtInventoryItems.Text = medicineItems.ToString("#,##0");
                        txtInventoryLowStock.Text = lowStockCount.ToString("#,##0");

                        txtCustomSaved.Text = savedTemplates.ToString("#,##0");
                        txtCustomGenerated.Text = generatedReports.ToString("#,##0");
                    });
                });

                dpFromDate.SelectedDate = DateTime.Now.AddDays(-7);
                dpToDate.SelectedDate = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // TOGGLE BUTTONS
        // ---------------------------------------------------------
        private void BtnWeeklySummary_Click(object sender, RoutedEventArgs e)
        {
            btnWeeklySummary.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#25A44A");
            btnWeeklySummary.Foreground = System.Windows.Media.Brushes.White;
            btnWeeklySummary.BorderThickness = new Thickness(0);

            btnMonthlyReport.Background = System.Windows.Media.Brushes.White;
            btnMonthlyReport.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#666");
            btnMonthlyReport.BorderThickness = new Thickness(1);

            dpFromDate.SelectedDate = DateTime.Now.AddDays(-7);
            dpToDate.SelectedDate = DateTime.Now;
        }

        private void BtnMonthlyReport_Click(object sender, RoutedEventArgs e)
        {
            btnMonthlyReport.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#25A44A");
            btnMonthlyReport.Foreground = System.Windows.Media.Brushes.White;
            btnMonthlyReport.BorderThickness = new Thickness(0);

            btnWeeklySummary.Background = System.Windows.Media.Brushes.White;
            btnWeeklySummary.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#666");
            btnWeeklySummary.BorderThickness = new Thickness(1);

            dpFromDate.SelectedDate = DateTime.Now.AddMonths(-1);
            dpToDate.SelectedDate = DateTime.Now;
        }

        // ---------------------------------------------------------
        // RESET FILTERS
        // ---------------------------------------------------------
        private void BtnResetFilters_Click(object sender, RoutedEventArgs e)
        {
            cbConditionSeverity.SelectedIndex = 0;
            cbDataRange.SelectedIndex = 0;

            dpFromDate.SelectedDate = DateTime.Now.AddDays(-7);
            dpToDate.SelectedDate = DateTime.Now;

            MessageBox.Show("Filters reset successfully.", "Reset", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ---------------------------------------------------------
        // AUTO CHANGE DATES WHEN DATA RANGE IS SELECTED
        // ---------------------------------------------------------
        private void cbDataRange_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (cbDataRange.SelectedIndex)
            {
                case 0: dpFromDate.SelectedDate = DateTime.Now.AddDays(-7); break;
                case 1: dpFromDate.SelectedDate = DateTime.Now.AddDays(-30); break;
                case 2: dpFromDate.SelectedDate = DateTime.Now.AddMonths(-3); break;
                case 3: dpFromDate.SelectedDate = DateTime.Now.AddMonths(-6); break;
                case 4: dpFromDate.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1); break;
            }
            dpToDate.SelectedDate = DateTime.Now;
        }

        // ---------------------------------------------------------
        // GENERATE REPORT ENTRY
        // ---------------------------------------------------------
        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var type = (cbConditionSeverity.SelectedItem as ComboBoxItem)?.Content.ToString();
                var range = (cbDataRange.SelectedItem as ComboBoxItem)?.Content.ToString();

                var report = new Report
                {
                    ReportType = type,
                    Title = $"{type} Report - {DateTime.Now:MMM dd yyyy}",
                    Description = $"Report generated for {range} [{dpFromDate.SelectedDate:MMM dd} - {dpToDate.SelectedDate:MMM dd}]",
                    GeneratedBy = "Doctor",
                    Status = "Generated",
                    Parameters = $"From={dpFromDate.SelectedDate}; To={dpToDate.SelectedDate}"
                };

                _reportService.CreateReport(report);

                MessageBox.Show("Report generated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---------------------------------------------------------
        // EXPORT PDF (Placeholder)
        // ---------------------------------------------------------
        private void BtnExportToPDF_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("PDF export feature coming soon.", "PDF Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ---------------------------------------------------------
        // VIEW REPORT BUTTONS
        // ---------------------------------------------------------
        private void BtnViewPatientReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Patient History Report…", "View Report");
        }

        private void BtnPrintPatientReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Printing Patient History Report…", "Print");
        }

        private void BtnViewAppointmentReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Appointment Report…", "View Report");
        }

        private void BtnPrintAppointmentReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Printing Appointment Report…", "Print");
        }

        private void BtnViewInventoryReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Inventory Report…", "View Report");
        }

        private void BtnPrintInventoryReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Printing Inventory Report…", "Print");
        }

        private void BtnCreateCustomReport_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Custom Report Builder…", "Custom Report");
        }

        private void BtnViewTemplates_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Opening Report Templates…", "Templates");
        }
    }
}

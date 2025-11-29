using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class PatientManagementView : UserControl
    {
        private RosalEHealthcareDbContext _db;
        private PatientService _patientService;
        private DashboardService _dashboardService;

        private List<PatientViewModel> _allPatients;
        private List<PatientViewModel> _filteredPatients;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private bool _isInitialized = false;

        static PatientManagementView()
        {
            try { QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community; } catch { }
        }

        public PatientManagementView()
        {
            InitializeComponent();
            InitializeServices();
        }

        public PatientManagementView(User user) : this() { }

        private void InitializeServices()
        {
            try
            {
                _db = new RosalEHealthcareDbContext();
                _patientService = new PatientService(_db);
                _dashboardService = new DashboardService(_db);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing services: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (_allPatients == null) await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            ShowLoading(true);
            try
            {
                await LoadSummaryCards();
                await LoadPatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadSummaryCards()
        {
            await Task.Run(() =>
            {
                try
                {
                    var total = _dashboardService.GetTotalPatients();
                    var today = _dashboardService.GetTodayAppointments();
                    var pending = _dashboardService.GetPendingAppointments();

                    var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    var newPatients = _db.Patients.Count(p => p.DateCreated >= startOfMonth);

                    Dispatcher.Invoke(() =>
                    {
                        CardTotalPatients.Value = total.ToString("N0");
                        CardTodayAppointments.Value = today.ToString("N0");
                        CardPendingAppointments.Value = pending.ToString("N0");
                        CardNewPatients.Value = newPatients.ToString("N0");
                    });
                }
                catch { }
            });
        }

        private async Task LoadPatients()
        {
            await Task.Run(() =>
            {
                try
                {
                    // FIX: Access DB directly to avoid "Missing Method" error
                    var patients = _db.Patients.Where(p => !p.IsArchived).ToList();

                    _allPatients = patients.Select(p => new PatientViewModel
                    {
                        Id = p.Id,
                        PatientId = p.PatientId,
                        FullName = p.FullName,
                        Contact = p.Contact,
                        Email = p.Email,
                        BirthDate = p.BirthDate,
                        Gender = p.Gender,
                        LastVisit = p.LastVisit,
                        PrimaryDiagnosis = p.PrimaryDiagnosis,
                        Status = p.Status,
                        Address = p.Address,
                        Allergies = p.Allergies,
                        BloodType = p.BloodType
                    }).ToList();

                    Dispatcher.Invoke(() => ApplyFilters());
                }
                catch (Exception ex) { Debug.WriteLine(ex.Message); }
            });
        }

        private void ApplyFilters()
        {
            if (_allPatients == null) return;

            var query = txtSearch.Text?.Trim().ToLower() ?? "";
            var status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString();

            _filteredPatients = _allPatients.Where(p =>
            {
                bool matchSearch = string.IsNullOrEmpty(query) ||
                                   p.FullName.ToLower().Contains(query) ||
                                   p.PatientId.ToLower().Contains(query);

                bool matchStatus = status == "All Status" || p.Status == status;
                bool matchGender = gender == "All Gender" || p.Gender == gender;

                return matchSearch && matchStatus && matchGender;
            }).ToList();

            _currentPage = 1;
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            if (_filteredPatients == null) return;

            _totalPages = (int)Math.Ceiling((double)_filteredPatients.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var paged = _filteredPatients.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
            dgPatients.ItemsSource = paged;

            txtResultCount.Text = $"Showing {paged.Count} of {_filteredPatients.Count} patients";
            txtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";

            btnFirst.IsEnabled = _currentPage > 1;
            btnPrevious.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < _totalPages;
            btnLast.IsEnabled = _currentPage < _totalPages;
        }

        private void ShowLoading(bool show) => LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();
        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            cmbStatus.SelectedIndex = 0;
            cmbGender.SelectedIndex = 0;
        }

        private void BtnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var modal = new RegisterPatientModal(SessionManager.CurrentUser);
                if (modal.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
            catch { MessageBox.Show("Patient Registration Modal not implemented or referenced."); }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var patient = _allPatients.FirstOrDefault(p => p.Id == id);
                if (patient == null) return;

                lblViewPatientId.Text = $"ID: {patient.PatientId}";
                patientDetailsPanel.Children.Clear();
                AddDetailRow("Full Name", patient.FullName);
                AddDetailRow("Date of Birth", patient.BirthDate?.ToString("MMMM dd, yyyy") ?? "N/A");
                AddDetailRow("Gender", patient.Gender);
                AddDetailRow("Contact", patient.Contact);
                AddDetailRow("Email", patient.Email);
                AddDetailRow("Address", patient.Address);
                AddDetailRow("Blood Type", patient.BloodType);
                AddDetailRow("Allergies", patient.Allergies);
                AddDetailRow("Last Visit", patient.LastVisitFormatted);

                viewDialog.Visibility = Visibility.Visible;
            }
        }

        private void AddDetailRow(string label, string value)
        {
            var p = new StackPanel { Margin = new Thickness(0, 0, 0, 15) };
            p.Children.Add(new TextBlock { Text = label, Foreground = Brushes.Gray, FontSize = 12 });
            p.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(value) ? "N/A" : value, FontSize = 14, FontWeight = FontWeights.Medium, TextWrapping = TextWrapping.Wrap });
            patientDetailsPanel.Children.Add(p);
        }

        private void CloseViewDialog_Click(object sender, RoutedEventArgs e) => viewDialog.Visibility = Visibility.Collapsed;

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                MessageBox.Show($"Edit Patient ID: {id}", "Edit", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                if (MessageBox.Show("Are you sure you want to archive this patient?", "Archive", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    await Task.Run(() => _patientService.ArchivePatient(id));
                    await LoadDataAsync();
                }
            }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e) { _currentPage = 1; ApplyPagination(); }
        private void BtnPrevious_Click(object sender, RoutedEventArgs e) { _currentPage--; ApplyPagination(); }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { _currentPage++; ApplyPagination(); }
        private void BtnLast_Click(object sender, RoutedEventArgs e) { _currentPage = _totalPages; ApplyPagination(); }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Coming soon.");
        private void BtnExportPdf_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Coming soon.");
    }

    public class PatientViewModel
    {
        public int Id { get; set; }
        public string PatientId { get; set; }
        public string FullName { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }
        public DateTime? LastVisit { get; set; }
        public string PrimaryDiagnosis { get; set; }
        public string Status { get; set; }
        public string Address { get; set; }
        public string BloodType { get; set; }
        public string Allergies { get; set; }

        public string AgeDisplay => BirthDate.HasValue ? $"Age {DateTime.Now.Year - BirthDate.Value.Year}" : "N/A";
        public string LastVisitFormatted => LastVisit?.ToString("MMM dd, yyyy") ?? "No visits";
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "?";
                var parts = FullName.Split(' ');
                return parts.Length > 1 ? (parts[0][0].ToString() + parts[1][0].ToString()).ToUpper() : FullName.Substring(0, 1).ToUpper();
            }
        }

        public Brush StatusBackground
        {
            get
            {
                switch (Status?.ToUpper())
                {
                    case "ACTIVE": return new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    case "PENDING": return new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    default: return new SolidColorBrush(Color.FromRgb(227, 242, 253));
                }
            }
        }

        public Brush StatusForeground
        {
            get
            {
                switch (Status?.ToUpper())
                {
                    case "ACTIVE": return new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    case "PENDING": return new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    default: return new SolidColorBrush(Color.FromRgb(33, 150, 243));
                }
            }
        }
    }
}
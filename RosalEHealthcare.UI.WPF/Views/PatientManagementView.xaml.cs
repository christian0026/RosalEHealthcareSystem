using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public PatientManagementView()
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            _patientService = new PatientService(_db);
            _dashboardService = new DashboardService(_db);
            LoadData();
        }

        private void LoadData()
        {
            LoadSummaryCards();
            LoadPatients();
        }

        private void LoadSummaryCards()
        {
            CardTotalPatients.Value = _dashboardService.GetTotalPatients().ToString("N0");
            CardTodayAppointments.Value = _dashboardService.GetTodayAppointments().ToString("N0");
            CardRemainingPatients.Value = _patientService.GetPatientsWaitingToday().ToString("N0");
            CardExpiringMedicines.Value = _dashboardService.GetExpiringMedicines(30).ToString("N0");
        }

        private void LoadPatients()
        {
            var query = txtSearch.Text?.Trim();
            var status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var gender = (cmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var totalCount = _patientService.GetFilteredCount(query, status, gender);
            _totalPages = (int)Math.Ceiling((double)totalCount / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var patients = _patientService.SearchPaged(query, status, gender, _currentPage, _pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.PatientId,
                    p.FullName,
                    Initials = GetInitials(p.FullName),
                    AgeDisplay = $"Age {p.Age}",
                    p.Contact,
                    p.Email,
                    p.BirthDate,
                    p.Gender,
                    p.LastVisit,
                    p.PrimaryDiagnosis,
                    p.Status,
                    StatusBackground = GetStatusBackground(p.Status),
                    StatusForeground = GetStatusForeground(p.Status)
                }).ToList();

            dgPatients.ItemsSource = patients;

            int start = ((_currentPage - 1) * _pageSize) + 1;
            int end = Math.Min(_currentPage * _pageSize, totalCount);
            txtPatientCount.Text = totalCount == 0 ? "No patients found" : $"Showing {start}-{end} of {totalCount} patients";

            BuildPagination();
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ');
            return parts.Length > 1
                ? $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper()
                : name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private Brush GetStatusBackground(string status) => status?.ToUpper() switch
        {
            "COMPLETED" => new SolidColorBrush(Color.FromRgb(232, 245, 233)),
            "PENDING" => new SolidColorBrush(Color.FromRgb(255, 243, 224)),
            "CANCELLED" => new SolidColorBrush(Color.FromRgb(255, 235, 238)),
            _ => new SolidColorBrush(Color.FromRgb(227, 242, 253))
        };

        private Brush GetStatusForeground(string status) => status?.ToUpper() switch
        {
            "COMPLETED" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            "PENDING" => new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            "CANCELLED" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
            _ => new SolidColorBrush(Color.FromRgb(33, 150, 243))
        };

        private void BuildPagination() 
        {
            paginationPanel.Children.Clear();

            var btnPrev = new Button { Content = "❮❮ Previous", Width = 100, IsEnabled = _currentPage > 1 };
            btnPrev.Click += (s, e) => { _currentPage--; LoadPatients(); };
            paginationPanel.Children.Add(btnPrev);

            int start = Math.Max(1, _currentPage - 2);
            int end = Math.Min(_totalPages, _currentPage + 2);

            if (start > 1)
            {
                AddPageButton(1);
                if (start > 2) paginationPanel.Children.Add(new TextBlock { Text = "...", Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
            }

            for (int i = start; i <= end; i++) AddPageButton(i);

            if (end < _totalPages)
            {
                if (end < _totalPages - 1) paginationPanel.Children.Add(new TextBlock { Text = "...", Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center });
                AddPageButton(_totalPages);
            }

            var btnNext = new Button { Content = "Next ❯❯", Width = 100, IsEnabled = _currentPage < _totalPages };
            btnNext.Click += (s, e) => { _currentPage++; LoadPatients(); };
            paginationPanel.Children.Add(btnNext);
        }

        private void AddPageButton(int page)
        {
            var btn = new Button { Content = page.ToString(), Width = 40, Height = 40, Tag = page };
            btn.Click += (s, e) => { _currentPage = (int)((Button)s).Tag; LoadPatients(); };
            paginationPanel.Children.Add(btn);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e) => LoadPatients();
        private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (dgPatients != null) LoadPatients(); }
        private void Search_Click(object sender, RoutedEventArgs e) => LoadPatients();

        private void View_Click(object sender, RoutedEventArgs e)
        {
             var patient = _patientService.GetById((int)((dynamic)((Button)sender).Tag).Id);
            if (patient == null) return;

            patientDetailsPanel.Children.Clear();
            AddDetail("Patient ID", patient.PatientId);
            AddDetail("Full Name", patient.FullName);
            AddDetail("Age", patient.Age.ToString());
            AddDetail("Gender", patient.Gender);
            AddDetail("Contact", patient.Contact);
            AddDetail("Email", patient.Email ?? "N/A");
            AddDetail("Address", patient.Address ?? "N/A");
            AddDetail("Blood Type", patient.BloodType ?? "N/A");
            AddDetail("Primary Diagnosis", patient.PrimaryDiagnosis ?? "N/A");
            AddDetail("Allergies", patient.Allergies ?? "None");
            AddDetail("Last Visit", patient.LastVisit?.ToString("MMMM dd, yyyy") ?? "No visits yet");

            viewDialog.Visibility = Visibility.Visible;
        }

        private void AddDetail(string label, string value)
        {
            patientDetailsPanel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold, FontSize = 13, Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)), Margin = new Thickness(0, 10, 0, 4) });
            patientDetailsPanel.Children.Add(new TextBlock { Text = value, FontSize = 14, Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)) });
        }

        private void CloseViewDialog_Click(object sender, RoutedEventArgs e) => viewDialog.Visibility = Visibility.Collapsed;

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit functionality: Opens patient edit form", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Archive_Click(object sender, RoutedEventArgs e)
        {
            var patientId = (int)((dynamic)((Button)sender).Tag).Id;
            if (MessageBox.Show("Archive this patient? They will no longer appear in active lists.", "Confirm Archive", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _patientService.ArchivePatient(patientId);
                MessageBox.Show("Patient archived successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadData();
            }
        }

        public int GetPatientsWaitingToday()
        {
            var today = DateTime.Today;
            return _db.Appointments
                .Count(a => DbFunctions.TruncateTime(a.Time) == today &&
                       a.Status == "SCHEDULED");
        }

        public int GetFilteredCount(string query, string status, string gender)
        {
            var q = _db.Patients.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(p => p.FullName.ToLower().Contains(query) ||
                                p.PatientId.ToLower().Contains(query) ||
                                p.Contact.ToLower().Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                q = q.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(gender) && gender != "All Gender")
            {
                q = q.Where(p => p.Gender == gender);
            }

            return q.Count();
        }

        public IEnumerable<Patient> SearchPaged(string query, string status, string gender, int page, int pageSize)
        {
            var q = _db.Patients.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(p => p.FullName.ToLower().Contains(query) ||
                                p.PatientId.ToLower().Contains(query) ||
                                p.Contact.ToLower().Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                q = q.Where(p => p.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(gender) && gender != "All Gender")
            {
                q = q.Where(p => p.Gender == gender);
            }

            return q.OrderByDescending(p => p.LastVisit)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
        }
    }
}
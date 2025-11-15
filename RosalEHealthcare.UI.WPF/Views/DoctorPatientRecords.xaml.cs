using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPatientRecords : UserControl, INotifyPropertyChanged
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly PatientService _patientService;
        private Patient _selectedPatient;
        private string _currentFilter = "All";

        public ObservableCollection<Patient> Patients { get; set; }
        public ObservableCollection<MedicalHistory> MedicalHistories { get; set; }
        public ObservableCollection<MedicalHistory> VisitHistories { get; set; }
        public ObservableCollection<Prescription> Prescriptions { get; set; }

        public Patient SelectedPatient
        {
            get { return _selectedPatient; }
            set
            {
                _selectedPatient = value;
                OnPropertyChanged(nameof(SelectedPatient));
                LoadPatientDetails();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DoctorPatientRecords()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _patientService = new PatientService(_db);

                Patients = new ObservableCollection<Patient>();
                MedicalHistories = new ObservableCollection<MedicalHistory>();
                VisitHistories = new ObservableCollection<MedicalHistory>();
                Prescriptions = new ObservableCollection<Prescription>();

                DataContext = this;

                Loaded += (s, e) => LoadPatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Patient Records:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Data Loading

        private async void LoadPatients()
        {
            try
            {
                ShowLoading(true);

                var patients = await Task.Run(() => _patientService.GetAll().ToList());

                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }

                lvPatients.ItemsSource = Patients;

                if (Patients.Count > 0)
                {
                    lvPatients.SelectedIndex = 0;
                }

                ShowLoading(false);
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                MessageBox.Show($"Error loading patients:\n{ex.Message}",
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPatientDetails()
        {
            if (SelectedPatient == null) return;

            try
            {
                // Load Medical History
                await LoadMedicalHistory();

                // Load Visit History
                await LoadVisitHistory();

                // Load Prescriptions
                await LoadPrescriptions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading patient details:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadMedicalHistory()
        {
            if (SelectedPatient == null) return;

            var histories = await Task.Run(() =>
                _patientService.GetMedicalHistory(SelectedPatient.Id).ToList());

            MedicalHistories.Clear();
            foreach (var history in histories)
            {
                MedicalHistories.Add(history);
            }

            icMedicalHistory.ItemsSource = MedicalHistories;
        }

        private async Task LoadVisitHistory()
        {
            if (SelectedPatient == null) return;

            var visits = await Task.Run(() =>
                _patientService.GetMedicalHistory(SelectedPatient.Id).ToList());

            VisitHistories.Clear();
            foreach (var visit in visits)
            {
                VisitHistories.Add(visit);
            }

            dgVisitHistory.ItemsSource = VisitHistories;
        }

        private async Task LoadPrescriptions()
        {
            if (SelectedPatient == null) return;

            var prescriptions = await Task.Run(() =>
                _patientService.GetPatientPrescriptions(SelectedPatient.Id).ToList());

            Prescriptions.Clear();
            foreach (var prescription in prescriptions)
            {
                Prescriptions.Add(prescription);
            }

            icPrescriptions.ItemsSource = Prescriptions;
        }

        #endregion

        #region Search and Filter

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text?.Trim().ToLower() ?? "";

            if (string.IsNullOrEmpty(searchText))
            {
                ApplyFilter(_currentFilter);
                return;
            }

            var filtered = Patients.Where(p =>
                (p.FullName?.ToLower().Contains(searchText) ?? false) ||
                (p.PatientId?.ToLower().Contains(searchText) ?? false) ||
                (p.Contact?.ToLower().Contains(searchText) ?? false) ||
                (p.Email?.ToLower().Contains(searchText) ?? false) ||
                (p.PrimaryDiagnosis?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            lvPatients.ItemsSource = filtered;
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            _currentFilter = button.Tag?.ToString() ?? "All";

            // Update button styles
            btnFilterAll.Background = System.Windows.Media.Brushes.White;
            btnFilterAll.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));
            btnFilterRecent.Background = System.Windows.Media.Brushes.White;
            btnFilterRecent.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));
            btnFilterFollowUp.Background = System.Windows.Media.Brushes.White;
            btnFilterFollowUp.Foreground = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666"));

            button.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50"));
            button.Foreground = System.Windows.Media.Brushes.White;

            ApplyFilter(_currentFilter);
        }

        private void ApplyFilter(string filter)
        {
            var filteredPatients = Patients.AsEnumerable();

            switch (filter)
            {
                case "Recent":
                    filteredPatients = filteredPatients
                        .Where(p => p.LastVisit.HasValue &&
                               p.LastVisit.Value >= DateTime.Now.AddDays(-30));
                    break;
                case "FollowUp":
                    // Filter patients who need follow-up
                    // You can add a FollowUpRequired property to Patient model
                    break;
                case "All":
                default:
                    // Show all
                    break;
            }

            lvPatients.ItemsSource = filteredPatients.ToList();
        }

        #endregion

        #region Tab Navigation

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (panelPersonal == null) return;

            panelPersonal.Visibility = Visibility.Collapsed;
            panelMedical.Visibility = Visibility.Collapsed;
            panelVisits.Visibility = Visibility.Collapsed;
            panelPrescription.Visibility = Visibility.Collapsed;

            if (sender == rbPersonal)
                panelPersonal.Visibility = Visibility.Visible;
            else if (sender == rbMedical)
                panelMedical.Visibility = Visibility.Visible;
            else if (sender == rbVisits)
                panelVisits.Visibility = Visibility.Visible;
            else if (sender == rbPrescription)
                panelPrescription.Visibility = Visibility.Visible;
        }

        private void LvPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPatients.SelectedItem is Patient patient)
            {
                SelectedPatient = patient;
            }
        }

        #endregion

        #region Button Handlers

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editDialog = new EditPatientDialog(SelectedPatient, _patientService);
            if (editDialog.ShowDialog() == true)
            {
                // Refresh patient list
                LoadPatients();
                MessageBox.Show("Patient updated successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to archive {SelectedPatient.FullName}?\n\nThis will hide the patient from the active list but preserve all records.",
                "Confirm Archive",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _patientService.ArchivePatient(SelectedPatient.Id);

                    // Log activity
                    LogActivity("Archive Patient", $"Archived patient: {SelectedPatient.FullName}");

                    MessageBox.Show("Patient archived successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadPatients();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error archiving patient:\n{ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var pdfService = new PdfExportService();
                var filePath = pdfService.ExportPatientRecord(SelectedPatient,
                    MedicalHistories.ToList(), Prescriptions.ToList());

                var result = MessageBox.Show(
                    $"Patient record exported successfully!\n\nLocation: {filePath}\n\nWould you like to open the file?",
                    "Export Successful",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting patient record:\n{ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAddMedicalHistory_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new AddMedicalHistoryDialog(SelectedPatient, _patientService);
            if (dialog.ShowDialog() == true)
            {
                LoadMedicalHistory();
                MessageBox.Show("Medical history record added successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnViewVisit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var visit = button?.DataContext as MedicalHistory;

            if (visit != null)
            {
                var dialog = new ViewVisitDialog(visit);
                dialog.ShowDialog();
            }
        }

        private void BtnNewPrescription_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Navigate to Prescription Management with patient preselected
            // This would need to be implemented based on your navigation system
            MessageBox.Show($"Opening Prescription Management for {SelectedPatient.FullName}...",
                "New Prescription", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnViewPrescription_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var prescription = button?.DataContext as Prescription;

            if (prescription != null)
            {
                var dialog = new ViewPrescriptionDialog(prescription);
                dialog.ShowDialog();
            }
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            // Implement loading indicator if needed
            Cursor = show ? System.Windows.Input.Cursors.Wait : System.Windows.Input.Cursors.Arrow;
        }

        private void LogActivity(string activityType, string description)
        {
            try
            {
                var log = new ActivityLog
                {
                    ActivityType = activityType,
                    Description = description,
                    Module = "Patient Records",
                    PerformedBy = SessionManager.CurrentUser?.FullName ?? "Unknown",
                    PerformedByRole = SessionManager.CurrentUser?.Role ?? "Unknown",
                    RelatedEntityId = SelectedPatient?.Id.ToString(),
                    PerformedAt = DateTime.Now
                };

                _db.ActivityLogs.Add(log);
                _db.SaveChanges();
            }
            catch
            {
                // Ignore logging errors
            }
        }

        #endregion
    }
}
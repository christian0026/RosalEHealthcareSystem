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
using System.Windows.Media;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPatientRecords : UserControl, INotifyPropertyChanged
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly PatientService _patientService;
        private readonly DispatcherTimer _autoRefreshTimer;
        private Patient _selectedPatient;
        private string _currentFilter = "All";

        public ObservableCollection<Patient> Patients { get; set; }
        public ObservableCollection<Patient> AllPatients { get; set; }
        public ObservableCollection<MedicalHistory> MedicalHistories { get; set; }
        public ObservableCollection<MedicalHistory> VisitHistories { get; set; }
        public ObservableCollection<Prescription> Prescriptions { get; set; }

        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged(nameof(SelectedPatient));
                UpdateHeaderDisplay();
                LoadPatientDetails();
                UpdateButtonStates();
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
                AllPatients = new ObservableCollection<Patient>();
                MedicalHistories = new ObservableCollection<MedicalHistory>();
                VisitHistories = new ObservableCollection<MedicalHistory>();
                Prescriptions = new ObservableCollection<Prescription>();

                DataContext = this;

                // Setup auto-refresh timer (every 30 seconds)
                _autoRefreshTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30)
                };
                _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

                Loaded += DoctorPatientRecords_Loaded;
                Unloaded += DoctorPatientRecords_Unloaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Patient Records:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoctorPatientRecords_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPatients();
            _autoRefreshTimer.Start();
        }

        private void DoctorPatientRecords_Unloaded(object sender, RoutedEventArgs e)
        {
            _autoRefreshTimer.Stop();
        }

        private void AutoRefreshTimer_Tick(object sender, EventArgs e)
        {
            RefreshPatientsQuietly();
        }

        #region Data Loading

        private async void LoadPatients()
        {
            try
            {
                ShowLoading(true);

                var patients = await Task.Run(() =>
                    _patientService.GetAll()
                        .Where(p => !p.IsArchived && p.Status != "Archived")
                        .OrderByDescending(p => p.LastVisit ?? p.DateCreated)
                        .ToList());

                AllPatients.Clear();
                Patients.Clear();

                foreach (var patient in patients)
                {
                    AllPatients.Add(patient);
                    Patients.Add(patient);
                }

                lvPatients.ItemsSource = Patients;
                pnlNoResults.Visibility = Patients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

                if (Patients.Count > 0 && lvPatients.SelectedItem == null)
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

        private async void RefreshPatientsQuietly()
        {
            try
            {
                var currentSelectedId = SelectedPatient?.Id;

                var patients = await Task.Run(() =>
                    _patientService.GetAll()
                        .Where(p => !p.IsArchived && p.Status != "Archived")
                        .OrderByDescending(p => p.LastVisit ?? p.DateCreated)
                        .ToList());

                bool hasChanges = patients.Count != AllPatients.Count ||
                    patients.Any(p => !AllPatients.Any(ap => ap.Id == p.Id));

                if (hasChanges)
                {
                    AllPatients.Clear();
                    foreach (var patient in patients)
                    {
                        AllPatients.Add(patient);
                    }

                    ApplyFilter(_currentFilter);

                    if (currentSelectedId.HasValue)
                    {
                        var previouslySelected = Patients.FirstOrDefault(p => p.Id == currentSelectedId);
                        if (previouslySelected != null)
                        {
                            lvPatients.SelectedItem = previouslySelected;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Silent refresh error: {ex.Message}");
            }
        }

        private void UpdateHeaderDisplay()
        {
            if (SelectedPatient == null)
            {
                txtHeaderInitials.Text = "--";
                txtHeaderName.Text = "No Patient Selected";
                txtHeaderPatientId.Text = "--";
                txtHeaderAgeGender.Text = "-- yrs • --";
                txtHeaderDiagnosis.Text = "";
                return;
            }

            txtHeaderInitials.Text = SelectedPatient.Initials ?? "--";
            txtHeaderName.Text = SelectedPatient.FullName ?? "Unknown";
            txtHeaderPatientId.Text = SelectedPatient.PatientId ?? "--";
            txtHeaderAgeGender.Text = $"{SelectedPatient.Age} yrs • {SelectedPatient.Gender ?? "--"}";
            txtHeaderDiagnosis.Text = SelectedPatient.PrimaryDiagnosis ?? "";
        }

        private void UpdateButtonStates()
        {
            bool hasPatient = SelectedPatient != null;
            btnEdit.IsEnabled = hasPatient;
            btnArchive.IsEnabled = hasPatient;
            btnPrint.IsEnabled = hasPatient;
        }

        private async void LoadPatientDetails()
        {
            if (SelectedPatient == null)
            {
                ClearPatientDisplay();
                return;
            }

            try
            {
                // Update Personal Information tab
                txtFullName.Text = SelectedPatient.FullName ?? "—";
                txtBirthDate.Text = SelectedPatient.BirthDate?.ToString("MMMM dd, yyyy") ?? "—";
                txtAge.Text = SelectedPatient.Age > 0 ? $"{SelectedPatient.Age} years" : "—";
                txtGender.Text = SelectedPatient.Gender ?? "—";
                txtContact.Text = SelectedPatient.Contact ?? "—";
                txtEmail.Text = SelectedPatient.Email ?? "—";
                txtAddress.Text = SelectedPatient.Address ?? "—";
                txtBloodType.Text = SelectedPatient.BloodType ?? "N/A";
                txtHeight.Text = !string.IsNullOrEmpty(SelectedPatient.Height) ? $"{SelectedPatient.Height} cm" : "—";
                txtWeight.Text = !string.IsNullOrEmpty(SelectedPatient.Weight) ? $"{SelectedPatient.Weight} kg" : "—";
                txtAllergies.Text = SelectedPatient.Allergies ?? "None reported";
                txtPrimaryDiagnosis.Text = SelectedPatient.PrimaryDiagnosis ?? "—";
                txtSecondaryDiagnosis.Text = SelectedPatient.SecondaryDiagnosis ?? "—";
                txtLastVisit.Text = SelectedPatient.LastVisit?.ToString("MMMM dd, yyyy") ?? "—";

                await LoadNextAppointment();
                await LoadMedicalHistory();
                await LoadVisitHistory();
                await LoadPrescriptions();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patient details: {ex.Message}");
            }
        }

        private void ClearPatientDisplay()
        {
            txtFullName.Text = "—";
            txtBirthDate.Text = "—";
            txtAge.Text = "—";
            txtGender.Text = "—";
            txtContact.Text = "—";
            txtEmail.Text = "—";
            txtAddress.Text = "—";
            txtBloodType.Text = "N/A";
            txtHeight.Text = "—";
            txtWeight.Text = "—";
            txtAllergies.Text = "None reported";
            txtPrimaryDiagnosis.Text = "—";
            txtSecondaryDiagnosis.Text = "—";
            txtLastVisit.Text = "—";
            txtNextAppointment.Text = "—";

            MedicalHistories.Clear();
            VisitHistories.Clear();
            Prescriptions.Clear();
        }

        private async Task LoadNextAppointment()
        {
            if (SelectedPatient == null) return;

            try
            {
                var nextAppt = await Task.Run(() =>
                    _db.Appointments
                        .Where(a => a.PatientId == SelectedPatient.Id &&
                                   a.Time > DateTime.Now &&
                                   a.Status != "CANCELLED")
                        .OrderBy(a => a.Time)
                        .FirstOrDefault());

                if (nextAppt != null)
                {
                    txtNextAppointment.Text = nextAppt.Time.ToString("MMMM dd, yyyy");
                    txtNextAppointment.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }
                else
                {
                    txtNextAppointment.Text = "No scheduled appointment";
                    txtNextAppointment.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadNextAppointment error: {ex.Message}");
                txtNextAppointment.Text = "—";
            }
        }

        private async Task LoadMedicalHistory()
        {
            if (SelectedPatient == null) return;

            try
            {
                var histories = await Task.Run(() =>
                    _patientService.GetMedicalHistory(SelectedPatient.Id).ToList());

                MedicalHistories.Clear();
                foreach (var history in histories)
                {
                    MedicalHistories.Add(history);
                }

                icMedicalHistory.ItemsSource = MedicalHistories;
                pnlNoMedicalHistory.Visibility = MedicalHistories.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadMedicalHistory error: {ex.Message}");
            }
        }

        private async Task LoadVisitHistory()
        {
            if (SelectedPatient == null) return;

            try
            {
                var visits = await Task.Run(() =>
                    _db.MedicalHistories
                        .Where(m => m.PatientId == SelectedPatient.Id)
                        .OrderByDescending(m => m.VisitDate)
                        .ToList());

                VisitHistories.Clear();
                foreach (var visit in visits)
                {
                    VisitHistories.Add(visit);
                }

                dgVisitHistory.ItemsSource = VisitHistories;
                pnlNoVisits.Visibility = VisitHistories.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadVisitHistory error: {ex.Message}");
            }
        }

        private async Task LoadPrescriptions()
        {
            if (SelectedPatient == null) return;

            try
            {
                var prescriptions = await Task.Run(() =>
                    _db.Prescriptions
                        .Include("Medicines")
                        .Where(p => p.PatientId == SelectedPatient.Id)
                        .OrderByDescending(p => p.CreatedAt)
                        .ToList());

                Prescriptions.Clear();
                foreach (var prescription in prescriptions)
                {
                    Prescriptions.Add(prescription);
                }

                icPrescriptions.ItemsSource = Prescriptions;
                pnlNoPrescriptions.Visibility = Prescriptions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPrescriptions error: {ex.Message}");
            }
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

            var filtered = AllPatients.Where(p =>
                (p.FullName?.ToLower().Contains(searchText) ?? false) ||
                (p.PatientId?.ToLower().Contains(searchText) ?? false) ||
                (p.Contact?.ToLower().Contains(searchText) ?? false) ||
                (p.Email?.ToLower().Contains(searchText) ?? false) ||
                (p.PrimaryDiagnosis?.ToLower().Contains(searchText) ?? false)
            ).ToList();

            Patients.Clear();
            foreach (var patient in filtered)
            {
                Patients.Add(patient);
            }

            lvPatients.ItemsSource = Patients;
            pnlNoResults.Visibility = Patients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;

            _currentFilter = button.Tag?.ToString() ?? "All";

            // Update button styles
            btnFilterAll.Style = (Style)Resources["FilterButtonStyle"];
            btnFilterRecent.Style = (Style)Resources["FilterButtonStyle"];
            btnFilterFollowUp.Style = (Style)Resources["FilterButtonStyle"];

            button.Style = (Style)Resources["FilterButtonActiveStyle"];

            ApplyFilter(_currentFilter);
        }

        private void ApplyFilter(string filter)
        {
            var filtered = AllPatients.AsEnumerable();

            switch (filter)
            {
                case "Recent":
                    filtered = filtered
                        .Where(p => p.LastVisit.HasValue &&
                               p.LastVisit.Value >= DateTime.Now.AddDays(-30))
                        .OrderByDescending(p => p.LastVisit);
                    break;
                case "FollowUp":
                    var patientIdsWithFollowUp = _db.Appointments
                        .Where(a => a.Type != null && a.Type.Contains("Follow") &&
                                   a.Status != "CANCELLED" && a.Status != "COMPLETED" &&
                                   a.Time >= DateTime.Today)
                        .Select(a => a.PatientId)
                        .Distinct()
                        .ToList();

                    filtered = filtered.Where(p => patientIdsWithFollowUp.Contains(p.Id));
                    break;
                case "All":
                default:
                    filtered = filtered.OrderByDescending(p => p.LastVisit ?? p.DateCreated);
                    break;
            }

            Patients.Clear();
            foreach (var patient in filtered)
            {
                Patients.Add(patient);
            }

            lvPatients.ItemsSource = Patients;
            pnlNoResults.Visibility = Patients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadPatients();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var editDialog = new EditPatientDialog(SelectedPatient, _patientService);
                editDialog.Owner = Window.GetWindow(this);

                if (editDialog.ShowDialog() == true)
                {
                    LoadPatients();
                    LogActivity("Edit Patient", $"Updated patient: {SelectedPatient.FullName}");
                    MessageBox.Show("Patient updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit dialog:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

                LogActivity("Print Record", $"Exported record for: {SelectedPatient.FullName}");

                var printResult = MessageBox.Show(
                    $"Patient record exported successfully!\n\nLocation: {filePath}\n\nWould you like to open the file?",
                    "Export Successful",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (printResult == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
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

            try
            {
                var dialog = new AddMedicalHistoryDialog(SelectedPatient, _patientService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    _ = LoadMedicalHistory();
                    _ = LoadVisitHistory();
                    SelectedPatient.LastVisit = DateTime.Now;
                    txtLastVisit.Text = DateTime.Now.ToString("MMMM dd, yyyy");
                    LogActivity("Add Medical History", $"Added record for: {SelectedPatient.FullName}");
                    MessageBox.Show("Medical history record added successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding medical history:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewVisit_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var visit = button?.DataContext as MedicalHistory;

            if (visit != null)
            {
                var dialog = new ViewVisitDialog(visit);
                dialog.Owner = Window.GetWindow(this);
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

            try
            {
                var prescriptionView = new DoctorPrescriptionManagement();
                prescriptionView.SelectedPatient = SelectedPatient;

                var window = new Window
                {
                    Title = $"New Prescription - {SelectedPatient.FullName}",
                    Content = prescriptionView,
                    Width = 1200,
                    Height = 850,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = Window.GetWindow(this),
                    ResizeMode = ResizeMode.CanResize
                };

                window.Closed += (s, args) => { _ = LoadPrescriptions(); };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening prescription management:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnViewPrescription_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var prescription = button?.DataContext as Prescription;

            if (prescription != null)
            {
                if (prescription.Medicines == null || !prescription.Medicines.Any())
                {
                    var fullPrescription = _db.Prescriptions
                        .Include("Medicines")
                        .FirstOrDefault(p => p.Id == prescription.Id);

                    if (fullPrescription != null)
                    {
                        prescription = fullPrescription;
                    }
                }

                var dialog = new PrescriptionPrintPreviewDialog(prescription, SelectedPatient);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
        }

        #endregion

        #region Helper Methods

        private void ShowLoading(bool show)
        {
            pnlLoading.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            lvPatients.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        #endregion
    }
}
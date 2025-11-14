using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPrescriptionManagement : UserControl
    {
        private readonly PrescriptionService _prescriptionSvc;
        private readonly MedicineService _medicineSvc;
        private readonly RosalEHealthcareDbContext _db;

        public class MedicineEntry
        {
            public int? MedicineId { get; set; }
            public string MedicineName { get; set; }
            public string Dosage { get; set; }
            public string Frequency { get; set; }
            public string Duration { get; set; }
            public int? Quantity { get; set; }
            public string Route { get; set; }
        }

        public ObservableCollection<MedicineEntry> Medicines { get; set; } = new ObservableCollection<MedicineEntry>();
        public ObservableCollection<Medicine> MedicineLookup { get; set; } = new ObservableCollection<Medicine>();

        private int? _selectedPatientId = null;
        private string _currentUserEmail = "system";

        public DoctorPrescriptionManagement()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _prescriptionSvc = new PrescriptionService(_db);
                _medicineSvc = new MedicineService(_db);

                DataContext = this;
                Loaded += (s, e) => LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error initializing: {0}", ex.Message),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadData()
        {
            try
            {
                var meds = await Task.Run(() => _medicineSvc.GetAllMedicines().ToList());
                MedicineLookup.Clear();
                foreach (var m in meds) MedicineLookup.Add(m);
            }
            catch { }

            icMedicines.ItemsSource = Medicines;
            txtCreatedDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
        }

        private void TxtSearchPatient_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = (txtSearchPatient.Text ?? "").Trim();

            if (string.IsNullOrEmpty(search))
            {
                ClearPatientHeader();
                return;
            }

            try
            {
                var patient = _db.Patients
                    .Where(p => p.FullName.Contains(search) || p.PatientId.Contains(search))
                    .FirstOrDefault();

                if (patient != null)
                {
                    SetPatientHeader(patient);
                }
                else
                {
                    ClearPatientHeader();
                }
            }
            catch
            {
                ClearPatientHeader();
            }
        }

        private void SetPatientHeader(Patient patient)
        {
            _selectedPatientId = patient.Id;
            txtPatientName.Text = patient.FullName;
            txtPatientMeta.Text = string.Format("ID: {0} • {1} yrs • {2}",
                patient.PatientId, patient.Age, patient.Gender ?? "");
            txtBloodType.Text = patient.BloodType ?? "-";
            txtHeight.Text = patient.Height ?? "-";
            txtWeight.Text = patient.Weight ?? "-";

            // Set initials
            var names = (patient.FullName ?? "").Split(' ');
            if (names.Length >= 2)
            {
                txtInitials.Text = string.Format("{0}{1}", names[0][0], names[names.Length - 1][0]).ToUpper();
            }
            else if (names.Length == 1 && names[0].Length >= 2)
            {
                txtInitials.Text = names[0].Substring(0, 2).ToUpper();
            }
            else
            {
                txtInitials.Text = "?";
            }
        }

        private void ClearPatientHeader()
        {
            _selectedPatientId = null;
            txtPatientName.Text = "Patient Name";
            txtPatientMeta.Text = "ID: -";
            txtBloodType.Text = "-";
            txtHeight.Text = "-";
            txtWeight.Text = "-";
            txtInitials.Text = "?";
        }

        private void BtnNewPrescription_Click(object sender, RoutedEventArgs e)
        {
            txtPrimaryDiagnosis.Text = "";
            txtSecondaryDiagnosis.Text = "";
            cbConditionSeverity.SelectedIndex = 1;
            Medicines.Clear();
            txtSpecialInstructions.Text = "";
            cbFollowUp.SelectedIndex = 1;
            dpNextAppointment.SelectedDate = null;
            cbPriority.SelectedIndex = 0;
            txtCreatedDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            Medicines.Add(new MedicineEntry
            {
                MedicineId = null,
                MedicineName = "",
                Dosage = "",
                Frequency = "Once daily",
                Duration = "",
                Quantity = 1,
                Route = "Oral"
            });
        }

        private void BtnRemoveMedicine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is MedicineEntry me)
            {
                Medicines.Remove(me);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnNewPrescription_Click(null, null);
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Print functionality will be implemented later.",
                "Print", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnSavePrescription_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedPatientId.HasValue)
            {
                MessageBox.Show("Please select a patient first.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPrimaryDiagnosis.Text))
            {
                MessageBox.Show("Please enter primary diagnosis.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var pres = new Prescription
                {
                    PatientId = _selectedPatientId.Value,
                    PatientName = txtPatientName.Text,
                    PrimaryDiagnosis = txtPrimaryDiagnosis.Text.Trim(),
                    SecondaryDiagnosis = (txtSecondaryDiagnosis.Text ?? "").Trim(),
                    ConditionSeverity = cbConditionSeverity.SelectedItem != null ?
                        ((ComboBoxItem)cbConditionSeverity.SelectedItem).Content.ToString() : "Moderate",
                    SpecialInstructions = txtSpecialInstructions.Text,
                    FollowUpRequired = cbFollowUp.SelectedItem != null &&
                        ((ComboBoxItem)cbFollowUp.SelectedItem).Content.ToString() == "Yes",
                    NextAppointment = dpNextAppointment.SelectedDate,
                    PriorityLevel = cbPriority.SelectedItem != null ?
                        ((ComboBoxItem)cbPriority.SelectedItem).Content.ToString() : "Routine",
                    CreatedBy = _currentUserEmail,
                    CreatedAt = DateTime.UtcNow,
                };

                foreach (var m in Medicines)
                {
                    pres.Medicines.Add(new PrescriptionMedicine
                    {
                        MedicineId = m.MedicineId ?? 0,
                        MedicineName = m.MedicineName,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        Duration = m.Duration,
                        Quantity = m.Quantity,
                        Route = m.Route
                    });
                }

                var saved = await Task.Run(() => _prescriptionSvc.SavePrescription(pres));

                MessageBox.Show(string.Format("Prescription saved ({0}).", saved.PrescriptionId),
                    "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                BtnNewPrescription_Click(null, null);
            }
            catch (Exception ex)
            {
                string innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                MessageBox.Show(string.Format("Error: {0}\n\nInner: {1}", ex.Message, innerMsg),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
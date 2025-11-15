using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddMedicalHistoryDialog : Window
    {
        private readonly Patient _patient;
        private readonly PatientService _patientService;

        public AddMedicalHistoryDialog(Patient patient, PatientService patientService)
        {
            InitializeComponent();

            _patient = patient ?? throw new ArgumentNullException(nameof(patient));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            txtPatientName.Text = $"Patient: {_patient.FullName} ({_patient.PatientId})";
            txtDoctorName.Text = SessionManager.CurrentUser?.FullName ?? "";
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                var history = new MedicalHistory
                {
                    PatientId = _patient.Id,
                    VisitDate = dpVisitDate.SelectedDate ?? DateTime.Now,
                    VisitType = (cbVisitType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),
                    Diagnosis = txtDiagnosis.Text.Trim(),
                    Treatment = txtTreatment.Text.Trim(),
                    DoctorName = txtDoctorName.Text.Trim(),
                    Severity = (cbSeverity.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString(),

                    // Vital Signs
                    BloodPressure = txtBloodPressure.Text.Trim(),
                    Temperature = ParseDecimal(txtTemperature.Text),
                    HeartRate = ParseInt(txtHeartRate.Text),
                    RespiratoryRate = ParseInt(txtRespiratoryRate.Text),
                    Weight = ParseDecimal(txtWeight.Text),
                    Height = ParseDecimal(txtHeight.Text),

                    // Clinical Info
                    Symptoms = txtSymptoms.Text.Trim(),
                    ClinicalNotes = txtClinicalNotes.Text.Trim(),
                    Recommendations = txtRecommendations.Text.Trim(),

                    // Lab Tests
                    LabTestName = txtLabTestName.Text.Trim(),
                    LabTestResult = txtLabTestResult.Text.Trim(),
                    LabTestDate = dpLabTestDate.SelectedDate,

                    // Follow-up
                    FollowUpRequired = chkFollowUp.IsChecked ?? false,
                    NextFollowUpDate = chkFollowUp.IsChecked == true ? dpFollowUpDate.SelectedDate : null,

                    CreatedAt = DateTime.Now,
                    CreatedBy = SessionManager.CurrentUser?.FullName ?? "Unknown"
                };

                _patientService.AddMedicalHistory(history);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving medical history:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (!dpVisitDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select visit date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpVisitDate.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDiagnosis.Text))
            {
                MessageBox.Show("Please enter diagnosis.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDiagnosis.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtDoctorName.Text))
            {
                MessageBox.Show("Please enter doctor name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDoctorName.Focus();
                return false;
            }

            return true;
        }

        private decimal? ParseDecimal(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (decimal.TryParse(text, out decimal result))
                return result;

            return null;
        }

        private int? ParseInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (int.TryParse(text, out int result))
                return result;

            return null;
        }
    }
}
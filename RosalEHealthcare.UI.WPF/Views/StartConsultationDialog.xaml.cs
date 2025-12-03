using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class StartConsultationDialog : Window
    {
        private readonly Appointment _appointment;
        private readonly AppointmentService _appointmentService;
        private readonly RosalEHealthcareDbContext _db;

        // Output properties (matching MedicalHistory model)
        public string BloodPressure { get; private set; }
        public decimal? Temperature { get; private set; }
        public int? HeartRate { get; private set; }
        public int? RespiratoryRate { get; private set; }
        public decimal? Weight { get; private set; }
        public decimal? Height { get; private set; }

        public StartConsultationDialog(Appointment appointment)
        {
            InitializeComponent();

            _appointment = appointment;
            _db = new RosalEHealthcareDbContext();
            _appointmentService = new AppointmentService(_db);

            LoadAppointmentData();
        }

        private void LoadAppointmentData()
        {
            if (_appointment == null) return;

            // Patient name and initials
            txtPatientName.Text = _appointment.PatientName ?? "Unknown Patient";
            txtInitials.Text = GetInitials(_appointment.PatientName);

            // Patient ID and demographics
            if (_appointment.PatientId.HasValue)
            {
                var patient = _db.Patients.Find(_appointment.PatientId.Value);
                if (patient != null)
                {
                    txtPatientId.Text = patient.PatientId ?? "N/A";
                    var age = patient.BirthDate.HasValue
                        ? (DateTime.Now.Year - patient.BirthDate.Value.Year).ToString() + " yrs"
                        : "N/A";
                    txtPatientInfo.Text = $"{age} • {patient.Gender ?? "N/A"}";
                }
                else
                {
                    SetAppointmentDemographics();
                }
            }
            else
            {
                SetAppointmentDemographics();
            }

            // Appointment type and chief complaint
            txtAppointmentType.Text = _appointment.Type ?? "General Consultation";
            txtChiefComplaint.Text = !string.IsNullOrEmpty(_appointment.Condition)
                ? _appointment.Condition
                : "No chief complaint specified";
        }

        private void SetAppointmentDemographics()
        {
            txtPatientId.Text = _appointment.AppointmentId ?? "N/A";
            var age = _appointment.BirthDate.HasValue
                ? (DateTime.Now.Year - _appointment.BirthDate.Value.Year).ToString() + " yrs"
                : "N/A";
            txtPatientInfo.Text = $"{age} • {_appointment.Gender ?? "N/A"}";
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        private void BtnStartConsultation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Parse vital signs (all optional)
                BloodPressure = string.IsNullOrWhiteSpace(txtBloodPressure.Text)
                    ? null
                    : txtBloodPressure.Text.Trim();

                if (decimal.TryParse(txtTemperature.Text?.Trim(), out decimal temp))
                    Temperature = temp;

                if (int.TryParse(txtHeartRate.Text?.Trim(), out int hr))
                    HeartRate = hr;

                if (int.TryParse(txtRespiratoryRate.Text?.Trim(), out int rr))
                    RespiratoryRate = rr;

                if (decimal.TryParse(txtWeight.Text?.Trim(), out decimal wt))
                    Weight = wt;

                if (decimal.TryParse(txtHeight.Text?.Trim(), out decimal ht))
                    Height = ht;

                // Update appointment status to IN_PROGRESS
                _appointmentService.StartConsultation(_appointment.Id);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting consultation:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
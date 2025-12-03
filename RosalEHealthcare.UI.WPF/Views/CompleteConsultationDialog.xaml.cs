using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class CompleteConsultationDialog : Window
    {
        private readonly Appointment _appointment;
        private readonly RosalEHealthcareDbContext _db;
        private readonly AppointmentService _appointmentService;

        // Vital signs from StartConsultationDialog
        public string BloodPressure { get; set; }
        public decimal? Temperature { get; set; }
        public int? HeartRate { get; set; }
        public int? RespiratoryRate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }

        // Output properties
        public bool CreatePrescription { get; private set; }
        public bool ScheduleFollowUp { get; private set; }
        public DateTime? FollowUpDate { get; private set; }
        public string PrimaryDiagnosis { get; private set; }
        public string SecondaryDiagnosis { get; private set; }
        public string Treatment { get; private set; }

        public CompleteConsultationDialog(Appointment appointment)
        {
            InitializeComponent();

            _appointment = appointment;
            _db = new RosalEHealthcareDbContext();
            _appointmentService = new AppointmentService(_db);

            // Set default follow-up date
            dpFollowUp.SelectedDate = DateTime.Today.AddDays(7);

            Loaded += (s, e) => LoadAppointmentData();
        }

        private void LoadAppointmentData()
        {
            if (_appointment == null) return;

            // Patient info
            txtPatientName.Text = _appointment.PatientName ?? "Unknown Patient";
            txtInitials.Text = GetInitials(_appointment.PatientName);

            // Get patient ID
            string patientIdText = _appointment.AppointmentId ?? "N/A";
            if (_appointment.PatientId.HasValue)
            {
                var patient = _db.Patients.Find(_appointment.PatientId.Value);
                if (patient != null)
                {
                    patientIdText = patient.PatientId ?? patientIdText;
                }
            }
            txtPatientId.Text = $"{patientIdText} • {_appointment.Type ?? "Consultation"}";

            // Calculate duration
            if (_appointment.ConsultationStartedAt.HasValue)
            {
                var duration = DateTime.Now - _appointment.ConsultationStartedAt.Value;
                txtDuration.Text = $"Duration: {(int)duration.TotalMinutes} minutes";
            }
            else
            {
                txtDuration.Text = "Duration: N/A";
            }

            // Show vital signs badges
            if (!string.IsNullOrEmpty(BloodPressure))
            {
                txtBPBadge.Text = $"BP: {BloodPressure}";
                bpBadge.Visibility = Visibility.Visible;
            }

            if (Temperature.HasValue)
            {
                txtTempBadge.Text = $"{Temperature:0.0}°C";
                tempBadge.Visibility = Visibility.Visible;
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        private void ChkFollowUp_Checked(object sender, RoutedEventArgs e)
        {
            followUpPanel.Visibility = Visibility.Visible;
        }

        private void ChkFollowUp_Unchecked(object sender, RoutedEventArgs e)
        {
            followUpPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(txtPrimaryDiagnosis.Text))
            {
                MessageBox.Show("Please enter a primary diagnosis.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrimaryDiagnosis.Focus();
                return;
            }

            try
            {
                // Save outputs
                PrimaryDiagnosis = txtPrimaryDiagnosis.Text.Trim();
                SecondaryDiagnosis = txtSecondaryDiagnosis.Text?.Trim();
                Treatment = txtTreatment.Text?.Trim();
                CreatePrescription = chkPrescription.IsChecked == true;
                ScheduleFollowUp = chkFollowUp.IsChecked == true;
                FollowUpDate = chkFollowUp.IsChecked == true ? dpFollowUp.SelectedDate : null;

                // Create medical history record if patient is linked
                if (_appointment.PatientId.HasValue)
                {
                    var medicalHistory = new MedicalHistory
                    {
                        PatientId = _appointment.PatientId.Value,
                        AppointmentId = _appointment.Id,
                        VisitDate = DateTime.Now,
                        VisitType = _appointment.Type ?? "Consultation",
                        Diagnosis = PrimaryDiagnosis,
                        Treatment = Treatment,
                        Symptoms = _appointment.Condition,
                        BloodPressure = BloodPressure,
                        Temperature = Temperature,
                        HeartRate = HeartRate,
                        RespiratoryRate = RespiratoryRate,
                        Weight = Weight,
                        Height = Height,
                        DoctorName = SessionManager.CurrentUser?.FullName ?? "Doctor",
                        ClinicalNotes = !string.IsNullOrEmpty(SecondaryDiagnosis)
                            ? $"Secondary Diagnosis: {SecondaryDiagnosis}"
                            : null,
                        FollowUpRequired = ScheduleFollowUp,
                        NextFollowUpDate = FollowUpDate,
                        CreatedAt = DateTime.Now,
                        CreatedBy = SessionManager.CurrentUser?.FullName ?? "Doctor"
                    };

                    _db.MedicalHistories.Add(medicalHistory);

                    // Update patient record
                    var patient = _db.Patients.Find(_appointment.PatientId.Value);
                    if (patient != null)
                    {
                        patient.LastVisit = DateTime.Now;
                        patient.PrimaryDiagnosis = PrimaryDiagnosis;
                        if (!string.IsNullOrEmpty(SecondaryDiagnosis))
                            patient.SecondaryDiagnosis = SecondaryDiagnosis;
                    }
                }

                // Complete the appointment
                _appointmentService.CompleteAppointment(_appointment.Id);

                // Create follow-up appointment if requested
                if (ScheduleFollowUp && FollowUpDate.HasValue)
                {
                    var followUpAppointment = new Appointment
                    {
                        PatientId = _appointment.PatientId,
                        PatientName = _appointment.PatientName,
                        Contact = _appointment.Contact,
                        BirthDate = _appointment.BirthDate,
                        Gender = _appointment.Gender,
                        Email = _appointment.Email,
                        Address = _appointment.Address,
                        Type = "Follow-up Visit",
                        Time = FollowUpDate.Value.Date.AddHours(9),
                        Condition = $"Follow-up for: {PrimaryDiagnosis}",
                        Status = "PENDING",
                        CreatedBy = SessionManager.CurrentUser?.FullName ?? "Doctor",
                        CreatedAt = DateTime.Now
                    };

                    _db.Appointments.Add(followUpAppointment);
                }

                _db.SaveChanges();

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error completing consultation:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
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
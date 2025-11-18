using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class RegisterPatientModal : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly PatientService _patientService;
        private readonly AppointmentService _appointmentService;
        private readonly User _currentUser;

        public Patient RegisteredPatient { get; private set; }
        public Appointment CreatedAppointment { get; private set; }

        public RegisterPatientModal(User currentUser)
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            _patientService = new PatientService(_db);
            _appointmentService = new AppointmentService(_db);
            _currentUser = currentUser;

            // Entrance animation
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
            this.BeginAnimation(OpacityProperty, fadeIn);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateClose();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AnimateClose();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                btnRegister.IsEnabled = false;
                btnRegister.Content = "⏳ Registering...";

                // Create patient
                var patient = new Patient
                {
                    FullName = txtFullName.Text.Trim(),
                    Contact = txtContact.Text.Trim(),
                    BirthDate = dpBirthDate.SelectedDate,
                    Gender = (cmbGender.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(),
                    Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    BloodType = (cmbBloodType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(),
                    PrimaryDiagnosis = string.IsNullOrWhiteSpace(txtPrimaryDiagnosis.Text) ? null : txtPrimaryDiagnosis.Text.Trim(),
                    Allergies = string.IsNullOrWhiteSpace(txtAllergies.Text) ? "None" : txtAllergies.Text.Trim(),
                    Status = "Active",
                    DateCreated = DateTime.Now,
                    LastVisit = DateTime.Now
                };

                // Save patient
                RegisteredPatient = _patientService.AddPatient(patient);

                // Create appointment for today
                var appointment = new Appointment
                {
                    PatientId = RegisteredPatient.Id,
                    PatientName = RegisteredPatient.FullName,
                    Type = "Walk-in",
                    Condition = RegisteredPatient.PrimaryDiagnosis ?? "General Consultation",
                    Status = "CONFIRMED",
                    Time = DateTime.Now,
                    Contact = RegisteredPatient.Contact,
                    CreatedBy = _currentUser?.FullName ?? "Receptionist",
                    CreatedAt = DateTime.Now
                };

                _appointmentService.AddAppointment(appointment);
                CreatedAppointment = appointment;

                this.DialogResult = true;
                AnimateClose();
            }
            catch (Exception ex)
            {
                // Fix: Build error message in memory
                var errorMessage = "Registration failed: " + ex.Message;
                ShowError(errorMessage);
                btnRegister.IsEnabled = true;
                btnRegister.Content = "✓ Register Patient";
            }
        }

        private bool ValidateForm()
        {
            // Reset error
            errorBorder.Visibility = Visibility.Collapsed;

            // Check required fields
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                ShowError("Full name is required");
                txtFullName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContact.Text))
            {
                ShowError("Contact number is required");
                txtContact.Focus();
                return false;
            }

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                ShowError("Birth date is required");
                dpBirthDate.Focus();
                return false;
            }

            if (cmbGender.SelectedItem == null)
            {
                ShowError("Gender is required");
                cmbGender.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                ShowError("Address is required");
                txtAddress.Focus();
                return false;
            }

            // Validate contact format
            var contact = txtContact.Text.Trim();
            if (!contact.All(char.IsDigit) || contact.Length < 10)
            {
                ShowError("Please enter a valid contact number (at least 10 digits)");
                txtContact.Focus();
                return false;
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                var email = txtEmail.Text.Trim();
                if (!email.Contains("@") || !email.Contains("."))
                {
                    ShowError("Please enter a valid email address");
                    txtEmail.Focus();
                    return false;
                }
            }

            // Validate age
            if (dpBirthDate.SelectedDate.HasValue)
            {
                var age = DateTime.Now.Year - dpBirthDate.SelectedDate.Value.Year;
                if (dpBirthDate.SelectedDate.Value.Date > DateTime.Now.AddYears(-age))
                {
                    age--;
                }

                if (age < 0 || age > 150)
                {
                    ShowError("Please enter a valid birth date");
                    dpBirthDate.Focus();
                    return false;
                }
            }

            return true;
        }

        private void ShowError(string message)
        {
            txtError.Text = message;
            errorBorder.Visibility = Visibility.Visible;
        }

        private void AnimateClose()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
            fadeOut.Completed += (s, e) => this.Close();
            this.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
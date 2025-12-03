using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorAppointmentLists : UserControl
    {
        private readonly AppointmentService _svc;
        private readonly RosalEHealthcareDbContext _db;

        // Temp vital signs storage
        private string _tempBloodPressure;
        private decimal? _tempTemperature;
        private int? _tempHeartRate;
        private int? _tempRespiratoryRate;
        private decimal? _tempWeight;
        private decimal? _tempHeight;

        public DoctorAppointmentLists()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _svc = new AppointmentService(_db);
                Loaded += (s, e) => LoadAppointmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadAppointmentsAsync(string search = "", string status = "", string type = "", DateTime? date = null)
        {
            try
            {
                var list = await Task.Run(() => _svc.GetAllAppointments());

                var filtered = list.Where(a =>
                    (string.IsNullOrEmpty(search) ||
                     (a.PatientName != null && a.PatientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)) &&
                    (status == "All Status" || string.IsNullOrEmpty(status) ||
                     (a.Status != null && a.Status.Equals(status, StringComparison.OrdinalIgnoreCase))) &&
                    (type == "All Types" || string.IsNullOrEmpty(type) ||
                     (a.Type != null && a.Type.Equals(type, StringComparison.OrdinalIgnoreCase))) &&
                    (!date.HasValue || a.Time.Date == date.Value.Date)
                ).OrderByDescending(a => a.Time).ToList();

                UpdateSummaryCounts(list);

                wpAppointments.Children.Clear();
                foreach (var appt in filtered)
                {
                    var card = new AppointmentCard();
                    card.SetAppointment(appt);

                    card.ConfirmClicked += Card_ConfirmClicked;
                    card.StartConsultationClicked += Card_StartConsultationClicked;
                    card.CompleteClicked += Card_CompleteClicked;
                    card.ViewDetailsClicked += Card_ViewDetailsClicked;
                    card.CancelClicked += Card_CancelClicked;

                    wpAppointments.Children.Add(card);
                }

                // Show empty state if no appointments
                if (!filtered.Any())
                {
                    var emptyText = new TextBlock
                    {
                        Text = "No appointments found",
                        FontSize = 16,
                        Foreground = System.Windows.Media.Brushes.Gray,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 50, 0, 0)
                    };
                    wpAppointments.Children.Add(emptyText);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCounts(IEnumerable<Appointment> appointments)
        {
            var completed = appointments.Count(a => a.Status == "COMPLETED");
            var confirmed = appointments.Count(a => a.Status == "CONFIRMED");
            var inProgress = appointments.Count(a => a.Status == "IN_PROGRESS");
            var pending = appointments.Count(a => a.Status == "PENDING");
            var cancelled = appointments.Count(a => a.Status == "CANCELLED");

            CardCompleted.Value = completed.ToString();
            CardConfirmed.Value = (confirmed + inProgress).ToString();
            CardPending.Value = pending.ToString();
            CardCancelled.Value = cancelled.ToString();

            CardCompleted.TrendText = "Consultations done";
            CardConfirmed.TrendText = inProgress > 0 ? $"{inProgress} in progress" : "Ready to start";
            CardPending.TrendText = "Awaiting confirmation";
            CardCancelled.TrendText = "Cancelled";
        }

        #region Filter Events

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void CbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null) ApplyFilters();
        }

        private void CbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null) ApplyFilters();
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null) ApplyFilters();
        }

        private void ApplyFilters()
        {
            LoadAppointmentsAsync(
                txtSearch?.Text ?? "",
                (cbStatus?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                (cbType?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                dpDate?.SelectedDate
            );
        }

        #endregion

        #region Card Actions

        private void Card_ConfirmClicked(object sender, Appointment appointment)
        {
            var result = MessageBox.Show(
                $"Confirm appointment for {appointment.PatientName}?\n\n" +
                $"Date: {appointment.Time:MMMM dd, yyyy}\n" +
                $"Time: {appointment.Time:hh:mm tt}\n" +
                $"Type: {appointment.Type}",
                "Confirm Appointment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _svc.ConfirmAppointment(appointment.Id);
                LoadAppointmentsAsync();

                MessageBox.Show(
                    "✓ Appointment confirmed!\n\n" +
                    "The appointment status is now CONFIRMED.\n" +
                    "Click 'Start Consultation' when the patient arrives.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Card_StartConsultationClicked(object sender, Appointment appointment)
        {
            try
            {
                var dialog = new StartConsultationDialog(appointment);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    // Store vital signs
                    _tempBloodPressure = dialog.BloodPressure;
                    _tempTemperature = dialog.Temperature;
                    _tempHeartRate = dialog.HeartRate;
                    _tempRespiratoryRate = dialog.RespiratoryRate;
                    _tempWeight = dialog.Weight;
                    _tempHeight = dialog.Height;

                    LoadAppointmentsAsync();

                    MessageBox.Show(
                        $"✓ Consultation started for {appointment.PatientName}\n\n" +
                        "The appointment status is now IN PROGRESS.\n\n" +
                        "When finished, click 'Complete Consultation' to:\n" +
                        "• Record diagnosis and treatment\n" +
                        "• Save to medical history\n" +
                        "• Optionally create a prescription",
                        "Consultation Started",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Card_CompleteClicked(object sender, Appointment appointment)
        {
            try
            {
                var dialog = new CompleteConsultationDialog(appointment);
                dialog.Owner = Window.GetWindow(this);

                // Pass vital signs
                dialog.BloodPressure = _tempBloodPressure;
                dialog.Temperature = _tempTemperature;
                dialog.HeartRate = _tempHeartRate;
                dialog.RespiratoryRate = _tempRespiratoryRate;
                dialog.Weight = _tempWeight;
                dialog.Height = _tempHeight;

                if (dialog.ShowDialog() == true)
                {
                    // Clear temp vital signs
                    ClearTempVitalSigns();

                    LoadAppointmentsAsync();

                    // Build success message
                    string message = "✓ Consultation completed successfully!\n\n";

                    if (appointment.PatientId.HasValue)
                        message += "• Medical history record saved\n";
                    else
                        message += "• Note: Patient not linked - no medical history saved\n";

                    if (dialog.ScheduleFollowUp && dialog.FollowUpDate.HasValue)
                        message += $"• Follow-up scheduled for {dialog.FollowUpDate.Value:MMMM dd, yyyy}\n";

                    // Ask about prescription
                    if (dialog.CreatePrescription && appointment.PatientId.HasValue)
                    {
                        var result = MessageBox.Show(
                            message + "\nWould you like to create a prescription now?",
                            "Consultation Completed",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            OpenPrescriptionWindow(appointment, dialog.PrimaryDiagnosis);
                        }
                    }
                    else
                    {
                        MessageBox.Show(message, "Consultation Completed",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearTempVitalSigns()
        {
            _tempBloodPressure = null;
            _tempTemperature = null;
            _tempHeartRate = null;
            _tempRespiratoryRate = null;
            _tempWeight = null;
            _tempHeight = null;
        }

        private void OpenPrescriptionWindow(Appointment appointment, string diagnosis)
        {
            try
            {
                var patient = _db.Patients.Find(appointment.PatientId.Value);
                if (patient == null)
                {
                    MessageBox.Show("Patient record not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var prescriptionView = new DoctorPrescriptionManagement();

                // Try to set patient context if ViewModel exists
                try
                {
                    var viewModel = prescriptionView.DataContext as ViewModels.DoctorPrescriptionViewModel;
                    if (viewModel != null)
                    {
                        viewModel.SelectedPatient = patient;
                        viewModel.PrimaryDiagnosis = diagnosis;
                    }
                }
                catch { }

                var window = new Window
                {
                    Title = $"New Prescription - {patient.FullName}",
                    Content = prescriptionView,
                    Width = 1150,
                    Height = 850,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Owner = Window.GetWindow(this)
                };

                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening prescription: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Card_ViewDetailsClicked(object sender, Appointment appointment)
        {
            var age = appointment.BirthDate.HasValue
                ? (DateTime.Now.Year - appointment.BirthDate.Value.Year).ToString()
                : "N/A";

            string duration = "";
            if (appointment.ConsultationStartedAt.HasValue && appointment.ConsultationCompletedAt.HasValue)
            {
                var dur = appointment.ConsultationCompletedAt.Value - appointment.ConsultationStartedAt.Value;
                duration = $"\n\n📊 Consultation Duration: {(int)dur.TotalMinutes} minutes";
            }

            string patientLink = appointment.PatientId.HasValue
                ? "✓ Linked to patient record"
                : "⚠ Not linked to patient record";

            MessageBox.Show(
                $"══════════════════════════════════\n" +
                $"         APPOINTMENT DETAILS\n" +
                $"══════════════════════════════════\n\n" +
                $"📋 Appointment ID: {appointment.AppointmentId}\n" +
                $"📊 Status: {appointment.Status}\n" +
                $"🔗 {patientLink}\n\n" +
                $"──────────────────────────────────\n" +
                $"         PATIENT INFORMATION\n" +
                $"──────────────────────────────────\n\n" +
                $"👤 Name: {appointment.PatientName}\n" +
                $"🎂 Age: {age} years old\n" +
                $"⚥ Gender: {appointment.Gender ?? "N/A"}\n" +
                $"📞 Contact: {appointment.Contact}\n" +
                $"📧 Email: {appointment.Email ?? "N/A"}\n" +
                $"🏠 Address: {appointment.Address ?? "N/A"}\n\n" +
                $"──────────────────────────────────\n" +
                $"         APPOINTMENT INFO\n" +
                $"──────────────────────────────────\n\n" +
                $"📝 Type: {appointment.Type}\n" +
                $"📅 Date: {appointment.Time:MMMM dd, yyyy}\n" +
                $"⏰ Time: {appointment.Time:hh:mm tt}\n" +
                $"🩺 Complaint: {appointment.Condition ?? "None specified"}" +
                duration,
                "Appointment Details",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Card_CancelClicked(object sender, Appointment appointment)
        {
            var result = MessageBox.Show(
                $"Cancel appointment for {appointment.PatientName}?\n\n" +
                $"Date: {appointment.Time:MMMM dd, yyyy hh:mm tt}\n\n" +
                "⚠ This action cannot be undone.",
                "Cancel Appointment",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appointment.Id, "CANCELLED");
                LoadAppointmentsAsync();
                MessageBox.Show("Appointment cancelled.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
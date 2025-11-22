using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Controls;
using System;
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
                MessageBox.Show("Error initializing Appointment Lists:\n" + ex.Message + "\n\nInner: " + ex.InnerException?.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadAppointmentsAsync(string search = "", string status = "", string type = "", DateTime? date = null)
        {
            try
            {
                var list = await Task.Run(() => _svc.GetAllAppointments());

                var filtered = list.Where(a =>
                    (string.IsNullOrEmpty(search) || a.PatientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (status == "All Status" || string.IsNullOrEmpty(status) || a.Status.Equals(status, StringComparison.OrdinalIgnoreCase)) &&
                    (type == "All Types" || string.IsNullOrEmpty(type) || a.Type.Equals(type, StringComparison.OrdinalIgnoreCase)) &&
                    (!date.HasValue || a.Time.Date == date.Value.Date)
                ).OrderByDescending(a => a.Time).ToList();

                UpdateSummaryCounts(list);

                wpAppointments.Children.Clear();
                foreach (var appt in filtered)
                {
                    var card = new AppointmentCard();
                    card.SetAppointment(appt);

                    // Wire up events
                    card.ConfirmClicked += Card_ConfirmClicked;
                    card.CompleteClicked += Card_CompleteClicked;
                    card.ViewDetailsClicked += Card_ViewDetailsClicked;
                    card.CancelClicked += Card_CancelClicked;

                    wpAppointments.Children.Add(card);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading appointments:\n" + ex.Message + "\n\nInner: " + ex.InnerException?.Message,
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCounts(System.Collections.Generic.IEnumerable<Appointment> appointments)
        {
            var completed = appointments.Count(a => a.Status == "COMPLETED");
            var confirmed = appointments.Count(a => a.Status == "CONFIRMED");
            var pending = appointments.Count(a => a.Status == "PENDING");
            var cancelled = appointments.Count(a => a.Status == "CANCELLED");

            CardCompleted.Value = completed.ToString();
            CardConfirmed.Value = confirmed.ToString();
            CardPending.Value = pending.ToString();
            CardCancelled.Value = cancelled.ToString();

            CardCompleted.TrendText = "Finished consultations";
            CardConfirmed.TrendText = confirmed > 0 ? "Ready for consultation" : "No confirmed appointments";
            CardPending.TrendText = pending > 0 ? "Awaiting confirmation" : "All confirmed";
            CardCancelled.TrendText = cancelled > 0 ? "Cancelled appointments" : "No cancellations";
        }

        #region Event Handlers

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadAppointmentsAsync(
                txtSearch.Text,
                (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                (cbType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                dpDate.SelectedDate
            );
        }

        private void CbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null)
            {
                LoadAppointmentsAsync(
                    txtSearch.Text,
                    (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    (cbType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    dpDate.SelectedDate
                );
            }
        }

        private void CbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null)
            {
                LoadAppointmentsAsync(
                    txtSearch.Text,
                    (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    (cbType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    dpDate.SelectedDate
                );
            }
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null)
            {
                LoadAppointmentsAsync(
                    txtSearch.Text,
                    (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    (cbType.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    dpDate.SelectedDate
                );
            }
        }

        #endregion

        #region Card Actions

        private void Card_ConfirmClicked(object sender, Appointment appointment)
        {
            var result = MessageBox.Show("Confirm appointment for " + appointment.PatientName + "?",
                "Confirm Appointment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appointment.Id, "CONFIRMED");
                LoadAppointmentsAsync();
                MessageBox.Show("Appointment confirmed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Card_CompleteClicked(object sender, Appointment appointment)
        {
            var result = MessageBox.Show("Mark appointment for " + appointment.PatientName + " as completed?",
                "Complete Appointment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appointment.Id, "COMPLETED");
                LoadAppointmentsAsync();
                MessageBox.Show("Appointment marked as completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Card_ViewDetailsClicked(object sender, Appointment appointment)
        {
            var age = appointment.BirthDate.HasValue
                ? (DateTime.Now.Year - appointment.BirthDate.Value.Year).ToString()
                : "N/A";

            MessageBox.Show(
                "APPOINTMENT DETAILS\n\n" +
                "Appointment ID: " + appointment.AppointmentId + "\n" +
                "Status: " + appointment.Status + "\n\n" +
                "PATIENT INFORMATION\n" +
                "Name: " + appointment.PatientName + "\n" +
                "Age: " + age + " years old\n" +
                "Gender: " + (appointment.Gender ?? "N/A") + "\n" +
                "Contact: " + appointment.Contact + "\n" +
                "Email: " + (appointment.Email ?? "N/A") + "\n" +
                "Address: " + (appointment.Address ?? "N/A") + "\n\n" +
                "APPOINTMENT DETAILS\n" +
                "Type: " + appointment.Type + "\n" +
                "Date & Time: " + appointment.Time.ToString("MMMM dd, yyyy - hh:mm tt") + "\n" +
                "Chief Complaint: " + appointment.Condition + "\n\n" +
                "Scheduled By: " + appointment.CreatedBy + "\n" +
                "Scheduled At: " + appointment.CreatedAt.ToString("MMMM dd, yyyy hh:mm tt"),
                "Appointment Details",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Card_CancelClicked(object sender, Appointment appointment)
        {
            var result = MessageBox.Show("Cancel appointment for " + appointment.PatientName + "?\n\nThis action cannot be undone.",
                "Cancel Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appointment.Id, "CANCELLED");
                LoadAppointmentsAsync();
                MessageBox.Show("Appointment cancelled.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
    }
}
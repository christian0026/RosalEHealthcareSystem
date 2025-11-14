using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorAppointmentLists : UserControl
    {
        private readonly AppointmentService _svc;

        public DoctorAppointmentLists()
        {
            InitializeComponent();

            try
            {
                var db = new RosalEHealthcareDbContext();
                _svc = new AppointmentService(db);
                Loaded += (s, e) => _ = LoadAppointmentsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Appointment Lists:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadAppointmentsAsync(string search = "", string status = "", string type = "")
        {
            try
            {
                var list = await Task.Run(() => _svc.GetAllAppointments());

                var filtered = list.Where(a =>
                    (string.IsNullOrEmpty(search) || a.PatientName.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) &&
                    (status == "All Status" || string.IsNullOrEmpty(status) || a.Status.Equals(status, StringComparison.OrdinalIgnoreCase)) &&
                    (type == "All Types" || string.IsNullOrEmpty(type) || a.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                UpdateSummaryCounts(list);

                wpAppointments.Children.Clear();
                foreach (var appt in filtered)
                    wpAppointments.Children.Add(CreateAppointmentCard(appt));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading appointments:\n{ex.Message}\n\nInner: {ex.InnerException?.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSummaryCounts(System.Collections.Generic.IEnumerable<Appointment> appointments)
        {
            txtCompletedCount.Text = appointments.Count(a => a.Status == "Completed").ToString();
            txtConfirmedCount.Text = appointments.Count(a => a.Status == "Confirmed").ToString();
            txtPendingCount.Text = appointments.Count(a => a.Status == "Pending").ToString();
            txtCancelledCount.Text = appointments.Count(a => a.Status == "Cancelled").ToString();
        }

        private Card CreateAppointmentCard(Appointment appt)
        {
            // background colors according to your design
            string barColor = appt.Status switch
            {
                "Completed" => "#2E7D32",
                "Confirmed" => "#1565C0",
                "Pending" => "#FFA000",
                "Cancelled" => "#D32F2F",
                _ => "Gray"
            };

            var card = new Card
            {
                Width = 520,
                Margin = new Thickness(10),
                Padding = new Thickness(20),
                Background = Brushes.White,
                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#E0E0E0"),
                BorderThickness = new Thickness(1),
               
            };

            var container = new StackPanel();

            // HEADER (name + status chip)
            var header = new DockPanel();
            header.Children.Add(new TextBlock
            {
                Text = appt.PatientName,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            var chip = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString(barColor),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(10, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = appt.Status.ToUpper(),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11
                }
            };
            DockPanel.SetDock(chip, Dock.Right);
            header.Children.Add(chip);

            container.Children.Add(header);

            // DETAILS
            container.Children.Add(MakeText($"ID: {appt.AppointmentId}", 12, "#777"));
            container.Children.Add(MakeText($"Today, {appt.Time:hh:mm tt}", 14, "Black", true));
            container.Children.Add(MakeText($"Type: {appt.Type}", 12, "#777"));
            container.Children.Add(MakeText($"Condition: {appt.Condition}", 12, "#777"));

            if (appt.LastVisit.HasValue)
                container.Children.Add(MakeText($"Last Visit: {appt.LastVisit:MMM dd, yyyy}", 12, "#777"));

            container.Children.Add(MakeText($"Contact: {appt.Contact}", 12, "#777"));

            // BUTTON ROW
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            if (appt.Status == "Pending")
                btnRow.Children.Add(MakeButton("Confirm Appointment", "#4CAF50", ConfirmAppointment, appt));

            if (appt.Status == "Confirmed")
                btnRow.Children.Add(MakeButton("Complete", "#2E7D32", CompleteAppointment, appt));

            btnRow.Children.Add(MakeButton("View Details", "#2196F3", ViewAppointment, appt));
            btnRow.Children.Add(MakeButton("Print Report", "#757575", PrintReport, appt));

            if (appt.Status != "Completed" && appt.Status != "Cancelled")
                btnRow.Children.Add(MakeButton("Cancel", "#E53935", CancelAppointment, appt));

            container.Children.Add(btnRow);

            card.Content = container;
            return card;
        }

        private TextBlock MakeText(string text, int size, string color, bool bold = false)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = size,
                Foreground = (Brush)new BrushConverter().ConvertFromString(color),
                FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Margin = new Thickness(0, 4, 0, 0)
            };
        }

        private Button MakeButton(string text, string colorHex, Action<Appointment> clickAction, Appointment appt)
        {
            var btn = new Button
            {
                Content = text,
                Background = (Brush)new BrushConverter().ConvertFromString(colorHex),
                Foreground = Brushes.White,
                Margin = new Thickness(0, 10, 10, 0),
                Padding = new Thickness(14, 6, 14, 6),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            btn.Click += (s, e) => clickAction(appt);
            return btn;
        }


        private Button MakeButton(string text, string bg, Brush fg, RoutedEventHandler click)
        {
            var button = new Button
            {
                Content = text,
                Margin = new Thickness(0, 4, 8, 0),
                Padding = new Thickness(12, 6, 12, 6),
                Background = (Brush)new BrushConverter().ConvertFromString(bg),
                Foreground = fg,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += click;
            return button;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = LoadAppointmentsAsync(
                txtSearch.Text,
                (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                (cbType.SelectedItem as ComboBoxItem)?.Content.ToString()
            );
        }

        private void CbStatus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null) // Check if service is initialized
            {
                _ = LoadAppointmentsAsync(
                    txtSearch.Text,
                    (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    (cbType.SelectedItem as ComboBoxItem)?.Content.ToString()
                );
            }
        }

        private void CbType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_svc != null) // Check if service is initialized
            {
                _ = LoadAppointmentsAsync(
                    txtSearch.Text,
                    (cbStatus.SelectedItem as ComboBoxItem)?.Content.ToString(),
                    (cbType.SelectedItem as ComboBoxItem)?.Content.ToString()
                );
            }
        }

        private void ConfirmAppointment(Appointment appt)
        {
            var result = MessageBox.Show($"Confirm appointment for {appt.PatientName}?",
                "Confirm Appointment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appt.Id, "Confirmed");
                _ = LoadAppointmentsAsync();
            }
        }

        private void CompleteAppointment(Appointment appt)
        {
            var result = MessageBox.Show($"Mark appointment for {appt.PatientName} as completed?",
                "Complete Appointment", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appt.Id, "Completed");
                _ = LoadAppointmentsAsync();
            }
        }

        private void CancelAppointment(Appointment appt)
        {
            var result = MessageBox.Show($"Cancel appointment for {appt.PatientName}?",
                "Cancel Appointment", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _svc.UpdateStatus(appt.Id, "Cancelled");
                _ = LoadAppointmentsAsync();
            }
        }

        private void ViewAppointment(Appointment appt)
        {
            MessageBox.Show($"Patient: {appt.PatientName}\nID: {appt.AppointmentId}\nType: {appt.Type}\nStatus: {appt.Status}\nCondition: {appt.Condition}\nTime: {appt.Time:MMM dd, yyyy - hh:mm tt}",
                "Appointment Details", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintReport(Appointment appt)
        {
            MessageBox.Show($"Printing report for {appt.PatientName}...\n\nThis feature will be implemented soon.",
                "Print Report", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
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
            // Dynamic colors - using if-else instead of switch expression
            string bg, statusColor;
            if (appt.Status == "Completed")
            {
                bg = "#E8F5E9";
                statusColor = "#2E7D32";
            }
            else if (appt.Status == "Confirmed")
            {
                bg = "#E3F2FD";
                statusColor = "#1565C0";
            }
            else if (appt.Status == "Pending")
            {
                bg = "#FFF8E1";
                statusColor = "#FFB300";
            }
            else if (appt.Status == "Cancelled")
            {
                bg = "#FFEBEE";
                statusColor = "#D32F2F";
            }
            else
            {
                bg = "#EEEEEE";
                statusColor = "Gray";
            }

            var card = new Card
            {
                Margin = new Thickness(10),
                Padding = new Thickness(16),
                Background = Brushes.White,
                Width = 460
            };

            var stack = new StackPanel();

            // Header
            var header = new DockPanel { LastChildFill = true };

            var patientName = new TextBlock
            {
                Text = appt.PatientName,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };
            header.Children.Add(patientName);

            var statusChip = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFromString(statusColor),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(6, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Child = new TextBlock
                {
                    Text = appt.Status.ToUpper(),
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11
                }
            };
            DockPanel.SetDock(statusChip, Dock.Right);
            header.Children.Add(statusChip);

            stack.Children.Add(header);

            // Content
            stack.Children.Add(new TextBlock
            {
                Text = string.Format("ID: {0}", appt.AppointmentId),
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = appt.Time.ToString("MMM dd, yyyy - hh:mm tt"),
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 6, 0, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = string.Format("Type: {0}", appt.Type),
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            });

            stack.Children.Add(new TextBlock
            {
                Text = string.Format("Condition: {0}", appt.Condition),
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 0)
            });

            if (appt.LastVisit.HasValue)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = string.Format("Last Visit: {0}", appt.LastVisit.Value.ToString("MMM dd, yyyy")),
                    Foreground = Brushes.Gray,
                    FontSize = 12,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = string.Format("Contact: {0}", appt.Contact),
                Foreground = Brushes.Gray,
                FontSize = 12,
                Margin = new Thickness(0, 2, 0, 10)
            });

            // Buttons
            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };

            if (appt.Status == "Pending")
                btnRow.Children.Add(MakeButton("Confirm", "#4CAF50", Brushes.White, (s, e) => ConfirmAppointment(appt)));

            if (appt.Status == "Confirmed")
                btnRow.Children.Add(MakeButton("Complete", "#2E7D32", Brushes.White, (s, e) => CompleteAppointment(appt)));

            btnRow.Children.Add(MakeButton("View Details", "#2196F3", Brushes.White, (s, e) => ViewAppointment(appt)));
            btnRow.Children.Add(MakeButton("Print Report", "#757575", Brushes.White, (s, e) => PrintReport(appt)));

            if (appt.Status != "Completed" && appt.Status != "Cancelled")
                btnRow.Children.Add(MakeButton("Cancel", "#E05A4F", Brushes.White, (s, e) => CancelAppointment(appt)));

            stack.Children.Add(btnRow);
            card.Content = stack;
            return card;
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
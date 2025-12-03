using RosalEHealthcare.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class AppointmentCard : UserControl
    {
        public Appointment Appointment { get; private set; }

        // Events
        public event EventHandler<Appointment> ConfirmClicked;
        public event EventHandler<Appointment> StartConsultationClicked;
        public event EventHandler<Appointment> CompleteClicked;
        public event EventHandler<Appointment> ViewDetailsClicked;
        public event EventHandler<Appointment> CancelClicked;

        public AppointmentCard()
        {
            InitializeComponent();
        }

        public void SetAppointment(Appointment appointment)
        {
            Appointment = appointment;

            // Set patient info
            txtPatientName.Text = appointment.PatientName ?? "Unknown";
            txtInitials.Text = GetInitials(appointment.PatientName);

            // Set age and gender
            var age = appointment.BirthDate.HasValue
                ? $"{DateTime.Now.Year - appointment.BirthDate.Value.Year} yrs"
                : "N/A";
            txtPatientInfo.Text = $"{age} • {appointment.Gender ?? "N/A"}";

            // Set appointment details
            txtAppointmentType.Text = appointment.Type ?? "Consultation";
            txtDateTime.Text = appointment.Time.ToString("MMM dd, yyyy • hh:mm tt");
            txtContact.Text = appointment.Contact ?? "N/A";

            // Set chief complaint
            txtCondition.Text = !string.IsNullOrEmpty(appointment.Condition)
                ? appointment.Condition
                : "No complaint specified";

            // Patient link indicator
            if (appointment.PatientId.HasValue)
            {
                txtPatientLink.Text = "✓ Patient Record Linked";
                txtPatientLink.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            }
            else
            {
                txtPatientLink.Text = "⚠ No Patient Record";
                txtPatientLink.Foreground = new SolidColorBrush(Color.FromRgb(255, 160, 0));
            }

            UpdateStatusAppearance(appointment.Status);
            BuildActionButtons(appointment.Status);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }

        private void UpdateStatusAppearance(string status)
        {
            switch (status?.ToUpper())
            {
                case "PENDING":
                    txtStatus.Text = "PENDING";
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(255, 160, 0));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 160, 0));
                    break;

                case "CONFIRMED":
                    txtStatus.Text = "CONFIRMED";
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    break;

                case "IN_PROGRESS":
                    txtStatus.Text = "IN PROGRESS";
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;

                case "COMPLETED":
                    txtStatus.Text = "COMPLETED";
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;

                case "CANCELLED":
                    txtStatus.Text = "CANCELLED";
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    break;

                default:
                    txtStatus.Text = status?.ToUpper() ?? "UNKNOWN";
                    StatusBar.Background = Brushes.Gray;
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    txtStatus.Foreground = Brushes.Gray;
                    break;
            }
        }

        private void BuildActionButtons(string status)
        {
            ActionButtons.Children.Clear();

            switch (status?.ToUpper())
            {
                case "PENDING":
                    ActionButtons.Children.Add(CreateButton("✓ Confirm", "#4CAF50", (s, e) => ConfirmClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("Cancel", "#E53935", (s, e) => CancelClicked?.Invoke(this, Appointment)));
                    break;

                case "CONFIRMED":
                    ActionButtons.Children.Add(CreateButton("▶ Start Consultation", "#FF9800", (s, e) => StartConsultationClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("Cancel", "#E53935", (s, e) => CancelClicked?.Invoke(this, Appointment)));
                    break;

                case "IN_PROGRESS":
                    ActionButtons.Children.Add(CreateButton("✓ Complete Consultation", "#2E7D32", (s, e) => CompleteClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    break;

                case "COMPLETED":
                case "CANCELLED":
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    break;
            }
        }

        private Button CreateButton(string text, string hexColor, RoutedEventHandler click)
        {
            var color = (Color)ColorConverter.ConvertFromString(hexColor);
            var brush = new SolidColorBrush(color);

            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = brush,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            btn.Click += click;

            // Create rounded corners template
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            template.VisualTree = border;
            btn.Template = template;

            return btn;
        }
    }
}
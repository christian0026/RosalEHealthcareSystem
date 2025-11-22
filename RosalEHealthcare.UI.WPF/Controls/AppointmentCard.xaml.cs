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

        public event EventHandler<Appointment> ConfirmClicked;
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

            txtPatientName.Text = appointment.PatientName;
            txtAppointmentId.Text = appointment.AppointmentId;
            txtInitials.Text = GetInitials(appointment.PatientName);

            txtDateTime.Text = appointment.Time.ToString("MMMM dd, yyyy - hh:mm tt");
            txtType.Text = appointment.Type ?? "N/A";
            txtCondition.Text = appointment.Condition ?? "N/A";
            txtContact.Text = appointment.Contact ?? "N/A";

            if (appointment.BirthDate.HasValue)
            {
                var age = CalculateAge(appointment.BirthDate.Value);
                txtAge.Text = age + " years old";
            }
            else
            {
                txtAge.Text = "Age N/A";
            }
            txtGender.Text = appointment.Gender ?? "N/A";

            UpdateStatusAppearance(appointment.Status);
            BuildActionButtons(appointment.Status);
        }

        private void UpdateStatusAppearance(string status)
        {
            txtStatus.Text = status?.ToUpper() ?? "UNKNOWN";

            switch (status?.ToUpper())
            {
                case "PENDING":
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(255, 160, 0));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(255, 160, 0));
                    break;
                case "CONFIRMED":
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    break;
                case "COMPLETED":
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;
                case "CANCELLED":
                    StatusBar.Background = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    StatusChip.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    break;
                default:
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
                    ActionButtons.Children.Add(CreateButton("Confirm Appointment", "#4CAF50", (s, e) => ConfirmClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("Cancel", "#E53935", (s, e) => CancelClicked?.Invoke(this, Appointment)));
                    break;

                case "CONFIRMED":
                    ActionButtons.Children.Add(CreateButton("Complete", "#2E7D32", (s, e) => CompleteClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    ActionButtons.Children.Add(CreateButton("Cancel", "#E53935", (s, e) => CancelClicked?.Invoke(this, Appointment)));
                    break;

                case "COMPLETED":
                case "CANCELLED":
                    ActionButtons.Children.Add(CreateButton("View Details", "#2196F3", (s, e) => ViewDetailsClicked?.Invoke(this, Appointment)));
                    break;
            }
        }

        private Button CreateButton(string text, string colorHex, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = text,
                Background = (Brush)new BrushConverter().ConvertFromString(colorHex),
                Foreground = Brushes.White,
                Padding = new Thickness(14, 8, 14, 8),
                Margin = new Thickness(0, 0, 8, 0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Simple style with rounded corners
            var style = new Style(typeof(Button));
            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            borderFactory.SetValue(Border.PaddingProperty, new Thickness(14, 8, 14, 8));

            var presenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            presenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(presenterFactory);
            template.VisualTree = borderFactory;

            style.Setters.Add(new Setter(Button.TemplateProperty, template));
            button.Style = style;
            button.Click += clickHandler;

            return button;
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(' ');
            if (parts.Length > 1)
                return (parts[0][0].ToString() + parts[parts.Length - 1][0].ToString()).ToUpper();
            return name.Substring(0, Math.Min(2, name.Length)).ToUpper();
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
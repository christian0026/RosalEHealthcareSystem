using RosalEHealthcare.Core.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    // Simple Visit Details Dialog
    public class ViewVisitDialog : Window
    {
        public ViewVisitDialog(MedicalHistory visit)
        {
            Title = "Visit Details";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.White;

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(30)
            };

            var stackPanel = new StackPanel();

            // Title
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Visit Details",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E7D32")),
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Visit Info
            AddInfoField(stackPanel, "Visit Date", visit.VisitDate.ToString("MMMM dd, yyyy"));
            AddInfoField(stackPanel, "Visit Type", visit.VisitType);
            AddInfoField(stackPanel, "Doctor", visit.DoctorName);
            AddInfoField(stackPanel, "Diagnosis", visit.Diagnosis);
            AddInfoField(stackPanel, "Treatment", visit.Treatment);

            if (!string.IsNullOrEmpty(visit.Symptoms))
                AddInfoField(stackPanel, "Symptoms", visit.Symptoms);

            if (!string.IsNullOrEmpty(visit.BloodPressure))
            {
                AddSectionTitle(stackPanel, "Vital Signs");
                AddInfoField(stackPanel, "Blood Pressure", visit.BloodPressure);
                if (visit.Temperature.HasValue)
                    AddInfoField(stackPanel, "Temperature", $"{visit.Temperature}°C");
                if (visit.HeartRate.HasValue)
                    AddInfoField(stackPanel, "Heart Rate", $"{visit.HeartRate} BPM");
            }

            if (!string.IsNullOrEmpty(visit.ClinicalNotes))
                AddInfoField(stackPanel, "Clinical Notes", visit.ClinicalNotes);

            if (!string.IsNullOrEmpty(visit.Recommendations))
                AddInfoField(stackPanel, "Recommendations", visit.Recommendations);

            if (visit.FollowUpRequired)
            {
                AddInfoField(stackPanel, "Follow-up Required", "Yes");
                if (visit.NextFollowUpDate.HasValue)
                    AddInfoField(stackPanel, "Next Follow-up", visit.NextFollowUpDate.Value.ToString("MMMM dd, yyyy"));
            }

            // Close Button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 120,
                Height = 40,
                Margin = new Thickness(0, 20, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Medium
            };
            closeButton.Click += (s, e) => Close();

            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            Content = scrollViewer;
        }

        private void AddSectionTitle(StackPanel panel, string title)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333")),
                Margin = new Thickness(0, 15, 0, 10)
            });
        }

        private void AddInfoField(StackPanel panel, string label, string value)
        {
            var labelBlock = new TextBlock
            {
                Text = label,
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#999")),
                Margin = new Thickness(0, 10, 0, 3)
            };

            var valueBlock = new TextBlock
            {
                Text = value ?? "N/A",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333"))
            };

            panel.Children.Add(labelBlock);
            panel.Children.Add(valueBlock);
        }
    }

    // Simple Prescription Details Dialog
    public class ViewPrescriptionDialog : Window
    {
        public ViewPrescriptionDialog(Prescription prescription)
        {
            Title = "Prescription Details";
            Width = 650;
            Height = 550;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.White;

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(30)
            };

            var stackPanel = new StackPanel();

            // Title
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Prescription Details",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E7D32")),
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Prescription Info
            AddInfoField(stackPanel, "Prescription ID", prescription.PrescriptionId);
            AddInfoField(stackPanel, "Patient", prescription.PatientName);
            AddInfoField(stackPanel, "Date Prescribed", prescription.CreatedAt.ToString("MMMM dd, yyyy"));
            AddInfoField(stackPanel, "Prescribed By", prescription.CreatedBy);
            AddInfoField(stackPanel, "Primary Diagnosis", prescription.PrimaryDiagnosis);

            if (!string.IsNullOrEmpty(prescription.SecondaryDiagnosis))
                AddInfoField(stackPanel, "Secondary Diagnosis", prescription.SecondaryDiagnosis);

            // Medicines
            AddSectionTitle(stackPanel, "Prescribed Medicines");

            if (prescription.Medicines != null && prescription.Medicines.Any())
            {
                foreach (var medicine in prescription.Medicines)
                {
                    var medicinePanel = new StackPanel
                    {
                        Background = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F9F9F9")),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var border = new Border
                    {
                        BorderBrush = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0E0")),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(15),
                        Child = medicinePanel
                    };

                    medicinePanel.Children.Add(new TextBlock
                    {
                        Text = medicine.MedicineName,
                        FontSize = 15,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333"))
                    });

                    medicinePanel.Children.Add(new TextBlock
                    {
                        Text = $"Dosage: {medicine.Dosage} • {medicine.Frequency} • {medicine.Duration}",
                        FontSize = 13,
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#666")),
                        Margin = new Thickness(0, 5, 0, 0)
                    });

                    if (!string.IsNullOrEmpty(medicine.Route))
                    {
                        medicinePanel.Children.Add(new TextBlock
                        {
                            Text = $"Route: {medicine.Route}",
                            FontSize = 12,
                            Foreground = new System.Windows.Media.SolidColorBrush(
                                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#999")),
                            Margin = new Thickness(0, 3, 0, 0)
                        });
                    }

                    stackPanel.Children.Add(border);
                }
            }

            if (!string.IsNullOrEmpty(prescription.SpecialInstructions))
            {
                AddSectionTitle(stackPanel, "Special Instructions");
                AddInfoField(stackPanel, "", prescription.SpecialInstructions);
            }

            if (prescription.FollowUpRequired)
            {
                AddInfoField(stackPanel, "Follow-up Required", "Yes");
                if (prescription.NextAppointment.HasValue)
                    AddInfoField(stackPanel, "Next Appointment", prescription.NextAppointment.Value.ToString("MMMM dd, yyyy"));
            }

            // Close Button
            var closeButton = new Button
            {
                Content = "Close",
                Width = 120,
                Height = 40,
                Margin = new Thickness(0, 20, 0, 0),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4CAF50")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14,
                FontWeight = FontWeights.Medium
            };
            closeButton.Click += (s, e) => Close();

            stackPanel.Children.Add(closeButton);

            scrollViewer.Content = stackPanel;
            Content = scrollViewer;
        }

        private void AddSectionTitle(StackPanel panel, string title)
        {
            panel.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333")),
                Margin = new Thickness(0, 15, 0, 10)
            });
        }

        private void AddInfoField(StackPanel panel, string label, string value)
        {
            if (!string.IsNullOrEmpty(label))
            {
                var labelBlock = new TextBlock
                {
                    Text = label,
                    FontSize = 12,
                    Foreground = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#999")),
                    Margin = new Thickness(0, 10, 0, 3)
                };
                panel.Children.Add(labelBlock);
            }

            var valueBlock = new TextBlock
            {
                Text = value ?? "N/A",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#333"))
            };

            panel.Children.Add(valueBlock);
        }
    }
}
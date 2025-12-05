using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Helpers;
using RosalEHealthcare.UI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class PrescriptionPrintPreviewDialog : Window
    {
        private readonly Prescription _prescription;
        private readonly Patient _patient;

        /// <summary>
        /// Constructor with Prescription and Patient (preferred)
        /// </summary>
        public PrescriptionPrintPreviewDialog(Prescription prescription, Patient patient)
        {
            InitializeComponent();

            _prescription = prescription;
            _patient = patient;

            LoadPreview();
        }

        /// <summary>
        /// Constructor with Prescription only (for viewing saved prescriptions)
        /// </summary>
        public PrescriptionPrintPreviewDialog(Prescription prescription)
        {
            InitializeComponent();

            _prescription = prescription;
            _patient = null;

            LoadPreview();
        }

        private void LoadPreview()
        {
            try
            {
                // Set prescription ID
                txtPrescriptionId.Text = _prescription?.PrescriptionId ?? "NEW";

                // Set date
                txtDate.Text = (_prescription?.CreatedAt ?? DateTime.Now).ToString("MMMM dd, yyyy");

                // Set patient info
                if (_patient != null)
                {
                    txtPatientName.Text = _patient.FullName ?? "N/A";
                    txtPatientId.Text = _patient.PatientId ?? "N/A";
                    txtPatientAge.Text = _patient.Age > 0 ? $"{_patient.Age} years" : "N/A";
                    txtPatientGender.Text = _patient.Gender ?? "N/A";
                    txtPatientContact.Text = _patient.Contact ?? "N/A";
                }
                else if (_prescription != null)
                {
                    // Fall back to prescription's PatientName
                    txtPatientName.Text = _prescription.PatientName ?? "N/A";
                    txtPatientId.Text = "N/A";
                    txtPatientAge.Text = "N/A";
                    txtPatientGender.Text = "N/A";
                    txtPatientContact.Text = "N/A";
                }

                // Set diagnosis
                txtDiagnosis.Text = _prescription?.PrimaryDiagnosis ?? "N/A";

                if (!string.IsNullOrEmpty(_prescription?.SecondaryDiagnosis))
                {
                    txtSecondaryDiagnosis.Text = $"Secondary: {_prescription.SecondaryDiagnosis}";
                    txtSecondaryDiagnosis.Visibility = Visibility.Visible;
                }
                else
                {
                    txtSecondaryDiagnosis.Visibility = Visibility.Collapsed;
                }

                // Set severity badge
                txtSeverity.Text = _prescription?.ConditionSeverity ?? "Moderate";
                SetSeverityColor(_prescription?.ConditionSeverity);

                // Set medicines - convert from PrescriptionMedicine to display format
                if (_prescription?.Medicines != null && _prescription.Medicines.Any())
                {
                    var medicineList = _prescription.Medicines.Select(m => new
                    {
                        MedicineName = m.MedicineName,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        Duration = m.Duration,
                        Quantity = m.Quantity,
                        Route = m.Route
                    }).ToList();

                    icMedicines.ItemsSource = medicineList;
                }

                // Set instructions
                txtInstructions.Text = !string.IsNullOrEmpty(_prescription?.SpecialInstructions)
                    ? _prescription.SpecialInstructions
                    : "Take medications as prescribed. Complete the full course of treatment.";

                // Set follow-up
                if (_prescription?.FollowUpRequired == true && _prescription.NextAppointment.HasValue)
                {
                    pnlFollowUp.Visibility = Visibility.Visible;
                    txtFollowUpDate.Text = _prescription.NextAppointment.Value.ToString("MMMM dd, yyyy");
                }
                else
                {
                    pnlFollowUp.Visibility = Visibility.Collapsed;
                }

                // Set doctor info
                txtDoctorName.Text = _prescription?.CreatedBy ??
                    SessionManager.CurrentUser?.FullName ?? "Doctor";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadPreview error: {ex.Message}");
            }
        }

        private void SetSeverityColor(string severity)
        {
            switch (severity?.ToLower())
            {
                case "mild":
                    bdrSeverity.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtSeverity.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    break;
                case "moderate":
                    bdrSeverity.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtSeverity.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    break;
                case "severe":
                    bdrSeverity.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    txtSeverity.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    break;
                case "critical":
                    bdrSeverity.Background = new SolidColorBrush(Color.FromRgb(183, 28, 28));
                    txtSeverity.Foreground = Brushes.White;
                    break;
                default:
                    bdrSeverity.Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    txtSeverity.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    break;
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Print the content
                    printDialog.PrintVisual(PrintArea, "Prescription - " + (_prescription?.PrescriptionId ?? "NEW"));

                    MessageBox.Show("Prescription printed successfully!", "Print",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing:\n{ex.Message}",
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
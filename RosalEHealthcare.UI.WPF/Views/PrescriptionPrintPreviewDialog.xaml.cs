using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Helpers;
using RosalEHealthcare.UI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class PrescriptionPrintPreviewDialog : Window
    {
        private readonly Patient _patient;
        private readonly string _primaryDiagnosis;
        private readonly string _secondaryDiagnosis;
        private readonly string _conditionSeverity;
        private readonly List<MedicineEntryViewModel> _medicines;
        private readonly string _specialInstructions;
        private readonly bool _followUpRequired;
        private readonly DateTime? _nextAppointment;
        private readonly string _priorityLevel;

        public PrescriptionPrintPreviewDialog(
            Patient patient,
            string primaryDiagnosis,
            string secondaryDiagnosis,
            string conditionSeverity,
            List<MedicineEntryViewModel> medicines,
            string specialInstructions,
            bool followUpRequired,
            DateTime? nextAppointment,
            string priorityLevel)
        {
            InitializeComponent();

            _patient = patient;
            _primaryDiagnosis = primaryDiagnosis;
            _secondaryDiagnosis = secondaryDiagnosis;
            _conditionSeverity = conditionSeverity;
            _medicines = medicines;
            _specialInstructions = specialInstructions;
            _followUpRequired = followUpRequired;
            _nextAppointment = nextAppointment;
            _priorityLevel = priorityLevel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadPrescriptionData();
        }

        private void LoadPrescriptionData()
        {
            // Patient Information
            txtPrintPatientName.Text = _patient.FullName;
            txtPrintPatientId.Text = _patient.PatientId;
            txtPrintAgeGender.Text = $"{_patient.Age} years, {_patient.Gender}";
            txtPrintDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
            txtPrintDoctor.Text = SessionManager.GetUserFullName();
            txtPrintDoctorSignature.Text = SessionManager.GetUserFullName();

            // Diagnosis
            var diagnosisText = _primaryDiagnosis;
            if (!string.IsNullOrWhiteSpace(_secondaryDiagnosis))
            {
                diagnosisText += $"\nSecondary: {_secondaryDiagnosis}";
            }
            diagnosisText += $"\nSeverity: {_conditionSeverity}";
            txtPrintDiagnosis.Text = diagnosisText;

            // Medications
            icPrintMedicines.ItemsSource = _medicines.Where(m => !string.IsNullOrEmpty(m.MedicineName));

            // Special Instructions
            if (string.IsNullOrWhiteSpace(_specialInstructions))
            {
                pnlInstructions.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtPrintInstructions.Text = _specialInstructions;
            }

            // Follow-up
            if (_followUpRequired && _nextAppointment.HasValue)
            {
                txtPrintFollowUp.Text = _nextAppointment.Value.ToString("MMMM dd, yyyy");
                pnlFollowUp.Visibility = Visibility.Visible;
            }
            else
            {
                pnlFollowUp.Visibility = Visibility.Collapsed;
            }

            // Priority
            txtPrintPriority.Text = _priorityLevel;
            switch (_priorityLevel)
            {
                case "Routine":
                    priorityBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtPrintPriority.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;
                case "Urgent":
                    priorityBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtPrintPriority.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                    break;
                case "High":
                    priorityBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    txtPrintPriority.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    break;
            }

            // Generate Prescription ID (for display only)
            txtPrescriptionId.Text = $"PR-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}";
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Get the printable area
                    var visual = printableArea;

                    // Set print settings
                    printDialog.PrintTicket.PageOrientation = PageOrientation.Portrait;

                    // Print the visual
                    printDialog.PrintVisual(visual, "Prescription - " + _patient.FullName);

                    MessageBox.Show("Prescription printed successfully!", "Print",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing prescription: {ex.Message}",
                    "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
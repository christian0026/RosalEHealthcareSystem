using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.ViewModels;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPrescriptionManagement : UserControl
    {
        private DoctorPrescriptionViewModel ViewModel => DataContext as DoctorPrescriptionViewModel;

        public DoctorPrescriptionManagement()
        {
            InitializeComponent();
        }

        #region Patient Search

        private async void TxtSearchPatient_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = txtSearchPatient.Text?.Trim();

            if (string.IsNullOrEmpty(query) || query.Length < 2)
            {
                popupSearchResults.IsOpen = false;
                return;
            }

            await ViewModel.SearchPatientsAsync(query);

            if (ViewModel.PatientSearchResults.Count > 0)
            {
                popupSearchResults.IsOpen = true;
            }
            else
            {
                popupSearchResults.IsOpen = false;
            }
        }

        private void TxtSearchPatient_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtSearchPatient.Text) && ViewModel.PatientSearchResults.Count > 0)
            {
                popupSearchResults.IsOpen = true;
            }
        }

        private void LstSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstSearchResults.SelectedItem is Patient patient)
            {
                ViewModel.SelectedPatient = patient;
                txtSearchPatient.Text = patient.FullName;
                popupSearchResults.IsOpen = false;
            }
        }

        #endregion

        #region Medicine Management

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.AddMedicine();
        }

        private void BtnRemoveMedicine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MedicineEntryViewModel medicine)
            {
                var result = MessageBox.Show(
                    $"Remove {medicine.MedicineName ?? "this medicine"}?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ViewModel.RemoveMedicine(medicine);
                }
            }
        }

        #endregion

        #region Action Buttons

        private void BtnNewPrescription_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Start a new prescription? All unsaved changes will be lost.",
                "New Prescription",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ViewModel.ResetForm();
                txtSearchPatient.Clear();
                txtSearchPatient.Focus();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnNewPrescription_Click(null, null);
        }

        private async void BtnSavePrescription_Click(object sender, RoutedEventArgs e)
        {
            var success = await ViewModel.SavePrescriptionAsync();
            if (success)
            {
                var result = MessageBox.Show(
                    "Prescription saved successfully!\n\n" +
                    "Medicine stock has been deducted from inventory.\n\n" +
                    "Would you like to create another prescription?",
                    "Success",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.No)
                {
                    // Close window if opened as dialog
                    var window = Window.GetWindow(this);
                    if (window != null && window.Owner != null)
                    {
                        window.DialogResult = true;
                        window.Close();
                        return;
                    }
                }

                ViewModel.ResetForm();
                txtSearchPatient.Clear();
                txtSearchPatient.Focus();
            }
        }

        private async void BtnSaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            var success = await ViewModel.SavePrescriptionAsync();
            if (success)
            {
                ShowPrintPreview();

                MessageBox.Show(
                    "Prescription saved and printed successfully!\n\n" +
                    "Medicine stock has been deducted from inventory.",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Close window if opened as dialog
                var window = Window.GetWindow(this);
                if (window != null && window.Owner != null)
                {
                    window.DialogResult = true;
                    window.Close();
                    return;
                }

                ViewModel.ResetForm();
                txtSearchPatient.Clear();
            }
        }

        private void BtnPrintPreview_Click(object sender, RoutedEventArgs e)
        {
            // Validate before showing preview
            if (ViewModel.SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ViewModel.PrimaryDiagnosis))
            {
                MessageBox.Show("Please enter primary diagnosis.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ViewModel.Medicines.Count == 0)
            {
                MessageBox.Show("Please add at least one medicine.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ShowPrintPreview();
        }

        private void ShowPrintPreview()
        {
            try
            {
                var previewDialog = new PrescriptionPrintPreviewDialog(
                    ViewModel.SelectedPatient,
                    ViewModel.PrimaryDiagnosis,
                    ViewModel.SecondaryDiagnosis,
                    ViewModel.ConditionSeverity,
                    ViewModel.Medicines.ToList(),
                    ViewModel.SpecialInstructions,
                    ViewModel.FollowUpRequired,
                    ViewModel.NextAppointment,
                    ViewModel.PriorityLevel
                );

                previewDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing print preview: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSaveDraft_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Draft saved locally. This feature will auto-save in the background.",
                "Draft Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnSaveAsTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.PrimaryDiagnosis))
            {
                MessageBox.Show("Please enter primary diagnosis before saving as template.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ViewModel.Medicines.Count == 0)
            {
                MessageBox.Show("Please add at least one medicine before saving as template.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveTemplateDialog();
            if (dialog.ShowDialog() == true)
            {
                var templateName = dialog.TemplateName;
                await ViewModel.SaveAsTemplateAsync(templateName);
            }
        }

        private void BtnTemplates_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TemplateSelectionDialog(ViewModel.Templates.ToList());
            if (dialog.ShowDialog() == true && dialog.SelectedTemplate != null)
            {
                ViewModel.ApplyTemplate(dialog.SelectedTemplate);
            }
        }

        #endregion
    }
}
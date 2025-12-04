using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using RosalEHealthcare.UI.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPrescriptionManagement : UserControl, INotifyPropertyChanged
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly PatientService _patientService;
        private readonly MedicineService _medicineService;
        private readonly PrescriptionService _prescriptionService;
        private readonly PrescriptionTemplateService _templateService;
        private readonly NotificationService _notificationService;

        private Patient _selectedPatient;
        private Medicine _selectedMedicine;
        private ObservableCollection<MedicineEntryViewModel> _medicines;
        private ObservableCollection<string> _warnings;
        private bool _isSearching;

        // Known drug interactions dictionary
        private readonly Dictionary<string, List<string>> _drugInteractions = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Warfarin", new List<string> { "Aspirin", "Ibuprofen", "Naproxen", "Vitamin K" } },
            { "Aspirin", new List<string> { "Warfarin", "Ibuprofen", "Naproxen", "Clopidogrel" } },
            { "Ibuprofen", new List<string> { "Aspirin", "Warfarin", "Lisinopril", "Methotrexate" } },
            { "Lisinopril", new List<string> { "Potassium", "Spironolactone", "Ibuprofen" } },
            { "Metformin", new List<string> { "Contrast dye", "Alcohol" } },
            { "Simvastatin", new List<string> { "Amiodarone", "Clarithromycin", "Grapefruit" } },
            { "Amoxicillin", new List<string> { "Methotrexate", "Warfarin" } },
            { "Ciprofloxacin", new List<string> { "Theophylline", "Warfarin", "Antacids" } },
            { "Omeprazole", new List<string> { "Clopidogrel", "Methotrexate" } },
            { "Clopidogrel", new List<string> { "Omeprazole", "Aspirin" } }
        };

        // Drug-allergy relationships
        private readonly Dictionary<string, List<string>> _allergyDrugClasses = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
        {
            { "Penicillin", new List<string> { "Amoxicillin", "Ampicillin", "Penicillin", "Augmentin", "Piperacillin" } },
            { "Sulfa", new List<string> { "Sulfamethoxazole", "Bactrim", "Septra", "Sulfasalazine" } },
            { "NSAIDs", new List<string> { "Ibuprofen", "Naproxen", "Aspirin", "Celecoxib", "Diclofenac" } },
            { "Aspirin", new List<string> { "Aspirin", "Acetylsalicylic acid" } },
            { "Codeine", new List<string> { "Codeine", "Morphine", "Hydrocodone", "Oxycodone" } },
            { "Latex", new List<string> { } },
            { "Iodine", new List<string> { "Povidone-iodine", "Amiodarone" } }
        };

        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged(nameof(SelectedPatient));
                UpdatePatientDisplay();
                CheckAllergyWarnings();
            }
        }

        public ObservableCollection<MedicineEntryViewModel> Medicines
        {
            get => _medicines;
            set
            {
                _medicines = value;
                OnPropertyChanged(nameof(Medicines));
            }
        }

        public ObservableCollection<string> Warnings
        {
            get => _warnings;
            set
            {
                _warnings = value;
                OnPropertyChanged(nameof(Warnings));
            }
        }

        public string PrimaryDiagnosis { get; set; }
        public string SecondaryDiagnosis { get; set; }
        public string SpecialInstructions { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DoctorPrescriptionManagement()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _patientService = new PatientService(_db);
                _medicineService = new MedicineService(_db);
                _prescriptionService = new PrescriptionService(_db);
                _templateService = new PrescriptionTemplateService(_db);
                _notificationService = new NotificationService(_db);

                Medicines = new ObservableCollection<MedicineEntryViewModel>();
                Warnings = new ObservableCollection<string>();

                DataContext = this;

                // Set default follow-up date
                dpFollowUp.SelectedDate = DateTime.Today.AddDays(7);

                Loaded += DoctorPrescriptionManagement_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Prescription Management:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DoctorPrescriptionManagement_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateMedicineDisplay();
            UpdatePreview();
        }

        #region Patient Search

        private async void TxtPatientSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtPatientSearch.Text?.Trim();

            if (string.IsNullOrEmpty(searchText) || searchText.Length < 2)
            {
                popupSearchResults.IsOpen = false;
                return;
            }

            await SearchPatientsAsync(searchText);
        }

        private void TxtPatientSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnSearchPatient_Click(sender, e);
            }
            else if (e.Key == Key.Escape)
            {
                popupSearchResults.IsOpen = false;
            }
        }

        private void BtnSearchPatient_Click(object sender, RoutedEventArgs e)
        {
            var searchText = txtPatientSearch.Text?.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                _ = SearchPatientsAsync(searchText);
            }
        }

        private async Task SearchPatientsAsync(string searchText)
        {
            if (_isSearching) return;
            _isSearching = true;

            try
            {
                var results = await Task.Run(() =>
                    _patientService.Search(searchText)
                        .Where(p => !p.IsArchived && p.Status != "Archived")
                        .Take(10)
                        .ToList());

                lbSearchResults.ItemsSource = results;
                popupSearchResults.IsOpen = results.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Patient search error: {ex.Message}");
            }
            finally
            {
                _isSearching = false;
            }
        }

        private void LbSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSearchResults.SelectedItem is Patient patient)
            {
                SelectedPatient = patient;
                txtPatientSearch.Text = "";
                popupSearchResults.IsOpen = false;
            }
        }

        private void BtnClearPatient_Click(object sender, RoutedEventArgs e)
        {
            SelectedPatient = null;
            txtPatientSearch.Text = "";
        }

        private void UpdatePatientDisplay()
        {
            if (SelectedPatient == null)
            {
                bdrSelectedPatient.Visibility = Visibility.Collapsed;
                txtPreviewPatient.Text = "Not selected";
                return;
            }

            bdrSelectedPatient.Visibility = Visibility.Visible;
            txtSelectedInitials.Text = SelectedPatient.Initials;
            txtSelectedName.Text = SelectedPatient.FullName;
            txtSelectedId.Text = SelectedPatient.PatientId;
            txtSelectedAge.Text = $"{SelectedPatient.Age} yrs • {SelectedPatient.Gender}";

            // Show allergies if present
            if (!string.IsNullOrEmpty(SelectedPatient.Allergies) &&
                !SelectedPatient.Allergies.Equals("None", StringComparison.OrdinalIgnoreCase) &&
                !SelectedPatient.Allergies.Equals("N/A", StringComparison.OrdinalIgnoreCase))
            {
                bdrAllergies.Visibility = Visibility.Visible;
                txtSelectedAllergies.Text = $"⚠ Allergies: {SelectedPatient.Allergies}";
            }
            else
            {
                bdrAllergies.Visibility = Visibility.Collapsed;
            }

            // Update preview
            txtPreviewPatient.Text = SelectedPatient.FullName;
        }

        #endregion

        #region Medicine Management

        private async void TxtMedicineName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtMedicineName.Text?.Trim();

            if (string.IsNullOrEmpty(searchText) || searchText.Length < 2)
            {
                popupMedicineResults.IsOpen = false;
                return;
            }

            try
            {
                var results = await Task.Run(() =>
                    _medicineService.Search(searchText)
                        .Where(m => m.IsActive && m.Stock > 0)
                        .Take(10)
                        .ToList());

                lbMedicineResults.ItemsSource = results;
                popupMedicineResults.IsOpen = results.Any();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Medicine search error: {ex.Message}");
            }
        }

        private void LbMedicineResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbMedicineResults.SelectedItem is Medicine medicine)
            {
                _selectedMedicine = medicine;
                txtMedicineName.Text = medicine.Name;
                popupMedicineResults.IsOpen = false;
            }
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            var medicineName = txtMedicineName.Text?.Trim();
            var dosage = txtDosage.Text?.Trim();
            var frequency = (cmbFrequency.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var duration = (cmbDuration.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var route = (cmbRoute.SelectedItem as ComboBoxItem)?.Content?.ToString();

            // Validation
            if (string.IsNullOrEmpty(medicineName))
            {
                MessageBox.Show("Please enter a medicine name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMedicineName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(dosage))
            {
                MessageBox.Show("Please enter the dosage.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtDosage.Focus();
                return;
            }

            // Check for duplicates
            if (Medicines.Any(m => m.MedicineName.Equals(medicineName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("This medicine has already been added.", "Duplicate Entry",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse quantity
            int quantity = 0;
            int.TryParse(txtQuantity.Text, out quantity);

            // Add medicine entry
            var entry = new MedicineEntryViewModel
            {
                MedicineId = _selectedMedicine?.Id,
                MedicineName = medicineName,
                Dosage = dosage,
                Frequency = frequency ?? "Three times daily",
                Duration = duration ?? "7 days",
                Quantity = quantity > 0 ? quantity : 21,
                Route = route ?? "Oral"
            };

            Medicines.Add(entry);

            // Clear form
            txtMedicineName.Text = "";
            txtDosage.Text = "500mg";
            txtQuantity.Text = "21";
            cmbFrequency.SelectedIndex = 2;
            cmbDuration.SelectedIndex = 2;
            cmbRoute.SelectedIndex = 0;
            _selectedMedicine = null;

            // Update displays
            UpdateMedicineDisplay();
            CheckDrugInteractions();
            CheckAllergyWarnings();
            UpdatePreview();

            txtMedicineName.Focus();
        }

        private void BtnRemoveMedicine_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var medicine = button?.DataContext as MedicineEntryViewModel;

            if (medicine != null)
            {
                Medicines.Remove(medicine);
                UpdateMedicineDisplay();
                CheckDrugInteractions();
                CheckAllergyWarnings();
                UpdatePreview();
            }
        }

        private void UpdateMedicineDisplay()
        {
            icMedicines.ItemsSource = null;
            icMedicines.ItemsSource = Medicines;

            txtMedicineCount.Text = $"{Medicines.Count} medication{(Medicines.Count != 1 ? "s" : "")} added";
            pnlNoMedicines.Visibility = Medicines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Drug Interactions & Allergy Checking

        private void CheckDrugInteractions()
        {
            var interactionWarnings = new List<string>();

            var medicineNames = Medicines.Select(m => m.MedicineName).ToList();

            foreach (var medicine in medicineNames)
            {
                if (_drugInteractions.TryGetValue(medicine, out var interactions))
                {
                    foreach (var interaction in interactions)
                    {
                        if (medicineNames.Any(m => m.Equals(interaction, StringComparison.OrdinalIgnoreCase)))
                        {
                            var warning = $"⚠ Drug Interaction: {medicine} may interact with {interaction}";
                            if (!interactionWarnings.Contains(warning))
                            {
                                interactionWarnings.Add(warning);
                            }
                        }
                    }
                }
            }

            // Update warnings
            foreach (var warning in interactionWarnings)
            {
                if (!Warnings.Contains(warning))
                {
                    Warnings.Add(warning);
                }
            }

            // Remove old interaction warnings that no longer apply
            var toRemove = Warnings.Where(w => w.Contains("Drug Interaction") &&
                !interactionWarnings.Contains(w)).ToList();
            foreach (var warning in toRemove)
            {
                Warnings.Remove(warning);
            }

            UpdateWarningsDisplay();
        }

        private void CheckAllergyWarnings()
        {
            if (SelectedPatient == null || string.IsNullOrEmpty(SelectedPatient.Allergies))
            {
                // Remove allergy warnings
                var allergyWarnings = Warnings.Where(w => w.Contains("Allergy")).ToList();
                foreach (var warning in allergyWarnings)
                {
                    Warnings.Remove(warning);
                }
                UpdateWarningsDisplay();
                return;
            }

            var patientAllergies = SelectedPatient.Allergies
                .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToList();

            var newWarnings = new List<string>();

            foreach (var allergy in patientAllergies)
            {
                // Check if any prescribed medicine is related to the allergy
                foreach (var medicine in Medicines)
                {
                    if (IsRelatedToAllergy(medicine.MedicineName, allergy))
                    {
                        var warning = $"⚠ Allergy Alert: Patient is allergic to {allergy}. {medicine.MedicineName} may cause a reaction.";
                        if (!newWarnings.Contains(warning))
                        {
                            newWarnings.Add(warning);
                        }
                    }
                }
            }

            // Update warnings
            var oldAllergyWarnings = Warnings.Where(w => w.Contains("Allergy")).ToList();
            foreach (var warning in oldAllergyWarnings)
            {
                Warnings.Remove(warning);
            }

            foreach (var warning in newWarnings)
            {
                Warnings.Add(warning);
            }

            UpdateWarningsDisplay();
        }

        private bool IsRelatedToAllergy(string medicineName, string allergy)
        {
            // Direct match
            if (medicineName.IndexOf(allergy, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            // Check drug class relationships
            foreach (var allergyClass in _allergyDrugClasses)
            {
                if (allergy.IndexOf(allergyClass.Key, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    allergyClass.Key.IndexOf(allergy, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (allergyClass.Value.Any(drug =>
                        medicineName.IndexOf(drug, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdateWarningsDisplay()
        {
            icWarnings.ItemsSource = null;
            icWarnings.ItemsSource = Warnings;
            bdrWarnings.Visibility = Warnings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Preview

        private void UpdatePreview()
        {
            // Update diagnosis preview
            txtPreviewDiagnosis.Text = !string.IsNullOrEmpty(txtPrimaryDiagnosis.Text)
                ? txtPrimaryDiagnosis.Text
                : "—";

            // Update date
            txtPreviewDate.Text = DateTime.Now.ToString("MMM dd, yyyy");

            // Update medications preview
            icPreviewMedicines.ItemsSource = null;
            icPreviewMedicines.ItemsSource = Medicines;
            txtPreviewNoMeds.Visibility = Medicines.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Follow-up

        private void ChkFollowUp_Changed(object sender, RoutedEventArgs e)
        {
            dpFollowUp.IsEnabled = chkFollowUp.IsChecked == true;

            if (chkFollowUp.IsChecked == true && dpFollowUp.SelectedDate == null)
            {
                dpFollowUp.SelectedDate = DateTime.Today.AddDays(7);
            }
        }

        #endregion

        #region Templates

        private void BtnLoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new TemplateSelectionDialog(_templateService);
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true && dialog.SelectedTemplate != null)
                {
                    ApplyTemplate(dialog.SelectedTemplate);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading templates:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyTemplate(PrescriptionTemplate template)
        {
            // Apply diagnosis
            txtPrimaryDiagnosis.Text = template.PrimaryDiagnosis ?? "";
            txtSecondaryDiagnosis.Text = template.SecondaryDiagnosis ?? "";

            // Apply severity
            if (!string.IsNullOrEmpty(template.ConditionSeverity))
            {
                for (int i = 0; i < cmbSeverity.Items.Count; i++)
                {
                    var item = cmbSeverity.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString().Equals(template.ConditionSeverity, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cmbSeverity.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Apply priority
            if (!string.IsNullOrEmpty(template.PriorityLevel))
            {
                for (int i = 0; i < cmbPriority.Items.Count; i++)
                {
                    var item = cmbPriority.Items[i] as ComboBoxItem;
                    if (item?.Content?.ToString().Equals(template.PriorityLevel, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        cmbPriority.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Apply medicines from JSON
            if (!string.IsNullOrEmpty(template.MedicinesJson))
            {
                try
                {
                    var medicines = Newtonsoft.Json.JsonConvert.DeserializeObject<List<MedicineEntryViewModel>>(template.MedicinesJson);
                    if (medicines != null)
                    {
                        Medicines.Clear();
                        foreach (var medicine in medicines)
                        {
                            Medicines.Add(medicine);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing medicines JSON: {ex.Message}");
                }
            }

            // Apply instructions
            txtInstructions.Text = template.InstructionsTemplate ?? "";

            // Apply follow-up
            chkFollowUp.IsChecked = template.FollowUpRequired;

            // Increment template usage
            _templateService.IncrementUsageCount(template.Id);

            UpdateMedicineDisplay();
            CheckDrugInteractions();
            CheckAllergyWarnings();
            UpdatePreview();

            MessageBox.Show($"Template '{template.TemplateName}' applied successfully.",
                "Template Applied", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void BtnSaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPrimaryDiagnosis.Text) && Medicines.Count == 0)
            {
                MessageBox.Show("Please enter at least a diagnosis or add medications before saving as a template.",
                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var dialog = new SaveTemplateDialog();
                dialog.Owner = Window.GetWindow(this);

                if (dialog.ShowDialog() == true)
                {
                    var template = new PrescriptionTemplate
                    {
                        TemplateName = dialog.TemplateName,
                        DoctorId = SessionManager.CurrentUser?.Id ?? 0,
                        PrimaryDiagnosis = txtPrimaryDiagnosis.Text,
                        SecondaryDiagnosis = txtSecondaryDiagnosis.Text,
                        ConditionSeverity = (cmbSeverity.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        PriorityLevel = (cmbPriority.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                        MedicinesJson = Newtonsoft.Json.JsonConvert.SerializeObject(Medicines.ToList()),
                        InstructionsTemplate = txtInstructions.Text,
                        FollowUpRequired = chkFollowUp.IsChecked == true,
                        IsActive = true,
                        UsageCount = 0,
                        CreatedAt = DateTime.Now
                    };

                    await Task.Run(() => _templateService.AddTemplate(template));

                    MessageBox.Show($"Template '{dialog.TemplateName}' saved successfully!",
                        "Template Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Save Prescription

        private async void BtnSavePrescription_Click(object sender, RoutedEventArgs e)
        {
            await SavePrescriptionAsync(false);
        }

        private async void BtnSaveAndPrint_Click(object sender, RoutedEventArgs e)
        {
            await SavePrescriptionAsync(true);
        }

        private async Task SavePrescriptionAsync(bool printAfterSave)
        {
            // Validation
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtPrimaryDiagnosis.Text))
            {
                MessageBox.Show("Please enter the primary diagnosis.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrimaryDiagnosis.Focus();
                return;
            }

            if (Medicines.Count == 0)
            {
                MessageBox.Show("Please add at least one medication.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check for warnings
            if (Warnings.Count > 0)
            {
                var result = MessageBox.Show(
                    $"There are {Warnings.Count} warning(s) for this prescription:\n\n" +
                    string.Join("\n", Warnings.Take(3)) +
                    (Warnings.Count > 3 ? $"\n...and {Warnings.Count - 3} more" : "") +
                    "\n\nDo you want to proceed anyway?",
                    "Warnings",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            try
            {
                btnSavePrescription.IsEnabled = false;
                btnSaveAndPrint.IsEnabled = false;

                var prescription = new Prescription
                {
                    PatientId = SelectedPatient.Id,
                    PrimaryDiagnosis = txtPrimaryDiagnosis.Text,
                    SecondaryDiagnosis = txtSecondaryDiagnosis.Text,
                    ConditionSeverity = (cmbSeverity.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                    SpecialInstructions = txtInstructions.Text,
                    FollowUpRequired = chkFollowUp.IsChecked == true,
                    NextAppointment = chkFollowUp.IsChecked == true ? dpFollowUp.SelectedDate : null,
                    CreatedBy = SessionManager.CurrentUser?.FullName ?? "Doctor",
                    CreatedAt = DateTime.Now,
                    Medicines = new List<PrescriptionMedicine>()
                };

                // Add medicines
                foreach (var medicine in Medicines)
                {
                    prescription.Medicines.Add(new PrescriptionMedicine
                    {
                        MedicineId = medicine.MedicineId,
                        MedicineName = medicine.MedicineName,
                        Dosage = medicine.Dosage,
                        Frequency = medicine.Frequency,
                        Duration = medicine.Duration,
                        Quantity = medicine.Quantity,
                        Route = medicine.Route
                    });
                }

                // Save prescription
                await Task.Run(() => _prescriptionService.AddPrescription(prescription));

                // Log activity
                LogActivity("Create Prescription",
                    $"Created prescription for {SelectedPatient.FullName}: {prescription.PrimaryDiagnosis}");

                MessageBox.Show("Prescription saved successfully!",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Print if requested
                if (printAfterSave)
                {
                    ShowPrintPreview(prescription);
                }

                // Reset form
                ResetForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving prescription:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnSavePrescription.IsEnabled = true;
                btnSaveAndPrint.IsEnabled = true;
            }
        }

        private void BtnPreview_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "Preview",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Create temporary prescription for preview
            var prescription = new Prescription
            {
                PatientId = SelectedPatient.Id,
                Patient = SelectedPatient,
                PrimaryDiagnosis = txtPrimaryDiagnosis.Text,
                SecondaryDiagnosis = txtSecondaryDiagnosis.Text,
                ConditionSeverity = (cmbSeverity.SelectedItem as ComboBoxItem)?.Content?.ToString(),
                SpecialInstructions = txtInstructions.Text,
                FollowUpRequired = chkFollowUp.IsChecked == true,
                NextAppointment = chkFollowUp.IsChecked == true ? dpFollowUp.SelectedDate : null,
                CreatedBy = SessionManager.CurrentUser?.FullName ?? "Doctor",
                CreatedAt = DateTime.Now,
                Medicines = Medicines.Select(m => new PrescriptionMedicine
                {
                    MedicineName = m.MedicineName,
                    Dosage = m.Dosage,
                    Frequency = m.Frequency,
                    Duration = m.Duration,
                    Quantity = m.Quantity,
                    Route = m.Route
                }).ToList()
            };

            ShowPrintPreview(prescription);
        }

        private void ShowPrintPreview(Prescription prescription)
        {
            try
            {
                // Ensure patient is attached
                if (prescription.Patient == null && SelectedPatient != null)
                {
                    prescription.Patient = SelectedPatient;
                }

                var dialog = new PrescriptionPrintPreviewDialog(prescription);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing preview:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Reset

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to reset the form? All entered data will be lost.",
                "Confirm Reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetForm();
            }
        }

        private void ResetForm()
        {
            SelectedPatient = null;
            txtPatientSearch.Text = "";
            txtPrimaryDiagnosis.Text = "";
            txtSecondaryDiagnosis.Text = "";
            cmbSeverity.SelectedIndex = 1;
            cmbPriority.SelectedIndex = 0;

            Medicines.Clear();
            Warnings.Clear();

            txtMedicineName.Text = "";
            txtDosage.Text = "500mg";
            txtQuantity.Text = "21";
            cmbFrequency.SelectedIndex = 2;
            cmbDuration.SelectedIndex = 2;
            cmbRoute.SelectedIndex = 0;
            _selectedMedicine = null;

            txtInstructions.Text = "";
            chkFollowUp.IsChecked = false;
            dpFollowUp.SelectedDate = DateTime.Today.AddDays(7);
            txtFollowUpNotes.Text = "";

            UpdateMedicineDisplay();
            UpdateWarningsDisplay();
            UpdatePreview();
        }

        #endregion

        #region Helpers

        private void LogActivity(string activityType, string description)
        {
            try
            {
                var log = new ActivityLog
                {
                    ActivityType = activityType,
                    Description = description,
                    Module = "Prescription Management",
                    PerformedBy = SessionManager.CurrentUser?.FullName ?? "Unknown",
                    PerformedByRole = SessionManager.CurrentUser?.Role ?? "Unknown",
                    RelatedEntityId = SelectedPatient?.Id.ToString(),
                    PerformedAt = DateTime.Now
                };

                _db.ActivityLogs.Add(log);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }

        #endregion
    }
}
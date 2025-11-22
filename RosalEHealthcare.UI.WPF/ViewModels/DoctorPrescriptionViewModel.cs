using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.ViewModels
{
    public class DoctorPrescriptionViewModel : INotifyPropertyChanged
    {
        private readonly PrescriptionService _prescriptionService;
        private readonly MedicineService _medicineService;
        private readonly PatientService _patientService;
        private readonly PrescriptionTemplateService _templateService;
        private readonly RosalEHealthcareDbContext _db;

        #region Properties

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasPatientSelected));
                LoadPatientAllergies();
            }
        }

        public bool HasPatientSelected => SelectedPatient != null;

        private string _searchQuery;
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                _searchQuery = value;
                OnPropertyChanged();
            }
        }

        private string _primaryDiagnosis;
        public string PrimaryDiagnosis
        {
            get => _primaryDiagnosis;
            set { _primaryDiagnosis = value; OnPropertyChanged(); }
        }

        private string _secondaryDiagnosis;
        public string SecondaryDiagnosis
        {
            get => _secondaryDiagnosis;
            set { _secondaryDiagnosis = value; OnPropertyChanged(); }
        }

        private string _conditionSeverity = "Moderate";
        public string ConditionSeverity
        {
            get => _conditionSeverity;
            set { _conditionSeverity = value; OnPropertyChanged(); }
        }

        private string _specialInstructions;
        public string SpecialInstructions
        {
            get => _specialInstructions;
            set { _specialInstructions = value; OnPropertyChanged(); }
        }

        private bool _followUpRequired;
        public bool FollowUpRequired
        {
            get => _followUpRequired;
            set { _followUpRequired = value; OnPropertyChanged(); }
        }

        private DateTime? _nextAppointment;
        public DateTime? NextAppointment
        {
            get => _nextAppointment;
            set { _nextAppointment = value; OnPropertyChanged(); }
        }

        private string _priorityLevel = "Routine";
        public string PriorityLevel
        {
            get => _priorityLevel;
            set { _priorityLevel = value; OnPropertyChanged(); }
        }

        private string _patientAllergies;
        public string PatientAllergies
        {
            get => _patientAllergies;
            set { _patientAllergies = value; OnPropertyChanged(); }
        }

        public ObservableCollection<MedicineEntryViewModel> Medicines { get; set; }
        public ObservableCollection<Medicine> MedicineLookup { get; set; }
        public ObservableCollection<PrescriptionTemplate> Templates { get; set; }
        public ObservableCollection<Patient> PatientSearchResults { get; set; }
        public ObservableCollection<string> DrugInteractionWarnings { get; set; }
        public ObservableCollection<string> AllergyWarnings { get; set; }

        public string CurrentDate => DateTime.Now.ToString("MMMM dd, yyyy");
        public string CurrentDoctor => SessionManager.GetUserFullName();

        #endregion

        #region Constructor

        public DoctorPrescriptionViewModel()
        {
            _db = new RosalEHealthcareDbContext();
            _prescriptionService = new PrescriptionService(_db);
            _medicineService = new MedicineService(_db);
            _patientService = new PatientService(_db);
            _templateService = new PrescriptionTemplateService(_db);

            Medicines = new ObservableCollection<MedicineEntryViewModel>();
            MedicineLookup = new ObservableCollection<Medicine>();
            Templates = new ObservableCollection<PrescriptionTemplate>();
            PatientSearchResults = new ObservableCollection<Patient>();
            DrugInteractionWarnings = new ObservableCollection<string>();
            AllergyWarnings = new ObservableCollection<string>();

            LoadInitialData();
        }

        #endregion

        #region Data Loading

        private async void LoadInitialData()
        {
            IsLoading = true;

            try
            {
                await Task.Run(() =>
                {
                    // Load medicines
                    var medicines = _medicineService.GetAllMedicines()
                        .Where(m => m.Status == "Available" || m.Status == "Low Stock")
                        .OrderBy(m => m.Name)
                        .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MedicineLookup.Clear();
                        foreach (var med in medicines)
                        {
                            MedicineLookup.Add(med);
                        }
                    });

                    // Load templates
                    var templates = _templateService.GetAllTemplates().ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Templates.Clear();
                        foreach (var template in templates)
                        {
                            Templates.Add(template);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading initial data: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadPatientAllergies()
        {
            if (SelectedPatient == null)
            {
                PatientAllergies = null;
                return;
            }

            PatientAllergies = SelectedPatient.Allergies;
            CheckAllergyWarnings();
        }

        #endregion

        #region Patient Search

        public async Task SearchPatientsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                PatientSearchResults.Clear();
                return;
            }

            try
            {
                await Task.Run(() =>
                {
                    var results = _patientService.Search(query, "Active")
                        .Take(10)
                        .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PatientSearchResults.Clear();
                        foreach (var patient in results)
                        {
                            PatientSearchResults.Add(patient);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching patients: {ex.Message}",
                    "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Medicine Management

        public void AddMedicine()
        {
            var newMedicine = new MedicineEntryViewModel
            {
                MedicineId = null,
                MedicineName = "",
                Dosage = "",
                Frequency = "Once daily",
                Duration = "",
                Quantity = 1,
                Route = "Oral"
            };

            newMedicine.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MedicineEntryViewModel.MedicineName))
                {
                    CheckDrugInteractions();
                    CheckAllergyWarnings();
                }
            };

            Medicines.Add(newMedicine);
        }

        public void RemoveMedicine(MedicineEntryViewModel medicine)
        {
            Medicines.Remove(medicine);
            CheckDrugInteractions();
            CheckAllergyWarnings();
        }

        #endregion

        #region Drug Interaction & Allergy Checking

        private void CheckDrugInteractions()
        {
            DrugInteractionWarnings.Clear();

            var selectedMedicines = Medicines
                .Where(m => !string.IsNullOrEmpty(m.MedicineName))
                .Select(m => m.MedicineName.ToLower())
                .ToList();

            // Known drug interactions (simplified - in production, use a comprehensive database)
            var interactions = new Dictionary<string, List<string>>
            {
                { "warfarin", new List<string> { "aspirin", "ibuprofen", "naproxen" } },
                { "aspirin", new List<string> { "warfarin", "ibuprofen" } },
                { "metformin", new List<string> { "alcohol" } },
                { "amlodipine", new List<string> { "simvastatin" } },
                { "omeprazole", new List<string> { "clopidogrel" } }
            };

            foreach (var medicine in selectedMedicines)
            {
                foreach (var interaction in interactions)
                {
                    if (medicine.Contains(interaction.Key.ToLower()))
                    {
                        foreach (var interactsWith in interaction.Value)
                        {
                            if (selectedMedicines.Any(m => m.Contains(interactsWith.ToLower())))
                            {
                                var warning = $"⚠ Possible interaction: {interaction.Key} and {interactsWith}";
                                if (!DrugInteractionWarnings.Contains(warning))
                                {
                                    DrugInteractionWarnings.Add(warning);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void CheckAllergyWarnings()
        {
            AllergyWarnings.Clear();

            if (string.IsNullOrWhiteSpace(PatientAllergies)) return;

            var allergies = PatientAllergies.ToLower().Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToList();

            foreach (var medicine in Medicines.Where(m => !string.IsNullOrEmpty(m.MedicineName)))
            {
                var medicineName = medicine.MedicineName.ToLower();

                foreach (var allergy in allergies)
                {
                    if (medicineName.Contains(allergy) ||
                        IsRelatedToAllergy(medicineName, allergy))
                    {
                        var warning = $"⚠ ALLERGY ALERT: Patient is allergic to {allergy}! Prescribed: {medicine.MedicineName}";
                        if (!AllergyWarnings.Contains(warning))
                        {
                            AllergyWarnings.Add(warning);
                        }
                    }
                }
            }
        }

        private bool IsRelatedToAllergy(string medicineName, string allergy)
        {
            // Common drug class relationships
            var relationships = new Dictionary<string, List<string>>
            {
                { "penicillin", new List<string> { "amoxicillin", "ampicillin", "penicillin" } },
                { "sulfa", new List<string> { "sulfamethoxazole", "trimethoprim" } },
                { "nsaid", new List<string> { "ibuprofen", "naproxen", "aspirin", "diclofenac" } }
            };

            if (relationships.ContainsKey(allergy))
            {
                return relationships[allergy].Any(related => medicineName.Contains(related));
            }

            return false;
        }

        #endregion

        #region Template Management

        public void ApplyTemplate(PrescriptionTemplate template)
        {
            if (template == null) return;

            try
            {
                PrimaryDiagnosis = template.PrimaryDiagnosis;
                SecondaryDiagnosis = template.SecondaryDiagnosis;
                ConditionSeverity = template.ConditionSeverity ?? "Moderate";
                SpecialInstructions = template.InstructionsTemplate;
                FollowUpRequired = template.FollowUpRequired;
                PriorityLevel = template.PriorityLevel ?? "Routine";

                // Parse and load medicines from JSON
                if (!string.IsNullOrEmpty(template.MedicinesJson))
                {
                    Medicines.Clear();
                    var medicinesData = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(template.MedicinesJson);

                    foreach (var medData in medicinesData)
                    {
                        var medicine = new MedicineEntryViewModel
                        {
                            MedicineName = medData.ContainsKey("MedicineName") ? medData["MedicineName"].ToString() : "",
                            Dosage = medData.ContainsKey("Dosage") ? medData["Dosage"].ToString() : "",
                            Frequency = medData.ContainsKey("Frequency") ? medData["Frequency"].ToString() : "Once daily",
                            Duration = medData.ContainsKey("Duration") ? medData["Duration"].ToString() : "",
                            Quantity = medData.ContainsKey("Quantity") ? Convert.ToInt32(medData["Quantity"]) : 1,
                            Route = medData.ContainsKey("Route") ? medData["Route"].ToString() : "Oral"
                        };

                        medicine.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(MedicineEntryViewModel.MedicineName))
                            {
                                CheckDrugInteractions();
                                CheckAllergyWarnings();
                            }
                        };

                        Medicines.Add(medicine);
                    }
                }

                // Increment usage count
                _templateService.IncrementUsageCount(template.Id);

                CheckDrugInteractions();
                CheckAllergyWarnings();

                MessageBox.Show($"Template '{template.TemplateName}' applied successfully!",
                    "Template Applied", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying template: {ex.Message}",
                    "Template Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task SaveAsTemplateAsync(string templateName)
        {
            if (string.IsNullOrWhiteSpace(templateName))
            {
                MessageBox.Show("Please enter a template name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsLoading = true;

                var medicinesData = Medicines.Select(m => new
                {
                    m.MedicineName,
                    m.Dosage,
                    m.Frequency,
                    m.Duration,
                    m.Quantity,
                    m.Route
                }).ToList();

                var medicinesJson = Newtonsoft.Json.JsonConvert.SerializeObject(medicinesData);

                var template = new PrescriptionTemplate
                {
                    TemplateName = templateName,
                    DoctorId = SessionManager.CurrentUser?.Id,
                    PrimaryDiagnosis = PrimaryDiagnosis,
                    SecondaryDiagnosis = SecondaryDiagnosis,
                    ConditionSeverity = ConditionSeverity,
                    MedicinesJson = medicinesJson,
                    InstructionsTemplate = SpecialInstructions,
                    FollowUpRequired = FollowUpRequired,
                    PriorityLevel = PriorityLevel,
                    CreatedBy = SessionManager.GetUserFullName()
                };

                await Task.Run(() =>
                {
                    _templateService.AddTemplate(template);
                });

                Templates.Add(template);

                MessageBox.Show("Template saved successfully!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving template: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Save Prescription

        public async Task<bool> SavePrescriptionAsync()
        {
            // Validation
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PrimaryDiagnosis))
            {
                MessageBox.Show("Please enter primary diagnosis.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (Medicines.Count == 0)
            {
                MessageBox.Show("Please add at least one medicine.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check for allergy warnings
            if (AllergyWarnings.Count > 0)
            {
                var result = MessageBox.Show(
                    "⚠ ALLERGY WARNINGS DETECTED!\n\n" + string.Join("\n", AllergyWarnings) + "\n\nDo you want to continue?",
                    "Allergy Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return false;
            }

            // Check for drug interactions
            if (DrugInteractionWarnings.Count > 0)
            {
                var result = MessageBox.Show(
                    "⚠ DRUG INTERACTION WARNINGS!\n\n" + string.Join("\n", DrugInteractionWarnings) + "\n\nDo you want to continue?",
                    "Drug Interaction Warning",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                    return false;
            }

            try
            {
                IsLoading = true;

                var prescription = new Prescription
                {
                    PatientId = SelectedPatient.Id,
                    PatientName = SelectedPatient.FullName,
                    PrimaryDiagnosis = PrimaryDiagnosis.Trim(),
                    SecondaryDiagnosis = SecondaryDiagnosis?.Trim(),
                    ConditionSeverity = ConditionSeverity,
                    SpecialInstructions = SpecialInstructions,
                    FollowUpRequired = FollowUpRequired,
                    NextAppointment = NextAppointment,
                    PriorityLevel = PriorityLevel,
                    CreatedBy = SessionManager.GetUserFullName(),
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var m in Medicines)
                {
                    if (string.IsNullOrWhiteSpace(m.MedicineName)) continue;

                    // Find medicine ID from lookup
                    var medicine = MedicineLookup.FirstOrDefault(med =>
                        med.Name.Equals(m.MedicineName, StringComparison.OrdinalIgnoreCase));

                    prescription.Medicines.Add(new PrescriptionMedicine
                    {
                        MedicineId = medicine?.Id,
                        MedicineName = m.MedicineName,
                        Dosage = m.Dosage,
                        Frequency = m.Frequency,
                        Duration = m.Duration,
                        Quantity = m.Quantity,
                        Route = m.Route
                    });
                }

                Prescription saved = null;
                await Task.Run(() =>
                {
                    saved = _prescriptionService.SavePrescription(prescription);
                });

                MessageBox.Show($"Prescription saved successfully!\n\nPrescription ID: {saved.PrescriptionId}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                // Update patient's last visit
                SelectedPatient.LastVisit = DateTime.Now;
                await Task.Run(() =>
                {
                    _patientService.UpdatePatient(SelectedPatient);
                });

                return true;
            }
            catch (Exception ex)
            {
                string innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                MessageBox.Show($"Error saving prescription: {ex.Message}\n\nInner: {innerMsg}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Reset Form

        public void ResetForm()
        {
            SelectedPatient = null;
            PrimaryDiagnosis = "";
            SecondaryDiagnosis = "";
            ConditionSeverity = "Moderate";
            SpecialInstructions = "";
            FollowUpRequired = false;
            NextAppointment = null;
            PriorityLevel = "Routine";
            Medicines.Clear();
            DrugInteractionWarnings.Clear();
            AllergyWarnings.Clear();
            PatientSearchResults.Clear();
            SearchQuery = "";
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Medicine Entry ViewModel

    public class MedicineEntryViewModel : INotifyPropertyChanged
    {
        private int? _medicineId;
        public int? MedicineId
        {
            get => _medicineId;
            set { _medicineId = value; OnPropertyChanged(); }
        }

        private string _medicineName;
        public string MedicineName
        {
            get => _medicineName;
            set { _medicineName = value; OnPropertyChanged(); }
        }

        private string _dosage;
        public string Dosage
        {
            get => _dosage;
            set { _dosage = value; OnPropertyChanged(); }
        }

        private string _frequency;
        public string Frequency
        {
            get => _frequency;
            set { _frequency = value; OnPropertyChanged(); }
        }

        private string _duration;
        public string Duration
        {
            get => _duration;
            set { _duration = value; OnPropertyChanged(); }
        }

        private int? _quantity;
        public int? Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); }
        }

        private string _route;
        public string Route
        {
            get => _route;
            set { _route = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #endregion
}
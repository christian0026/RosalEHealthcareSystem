using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class PatientRegistrationView : UserControl
    {
        private RosalEHealthcareDbContext _db;
        private PatientService _patientService;
        private User _currentUser;
        private int? _editingPatientId = null;

        public PatientRegistrationView()
        {
            InitializeComponent();
            InitializeServices();
            LoadRecentPatients();
        }

        public PatientRegistrationView(User user) : this()
        {
            _currentUser = user;
        }

        private void InitializeServices()
        {
            _db = new RosalEHealthcareDbContext();
            _patientService = new PatientService(_db);
        }

        #region Tab Navigation

        private void TabRecentPatients_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(btnRecentPatients);
            LoadRecentPatients();
        }

        private void TabTodayRegistration_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(btnTodayRegistration);
            LoadTodayRegistrations();
        }

        private void TabPendingAppointments_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(btnPendingAppointments);
            LoadPendingAppointments();
        }

        private void SetActiveTab(Button activeButton)
        {
            btnRecentPatients.Style = (Style)FindResource("TabButton");
            btnTodayRegistration.Style = (Style)FindResource("TabButton");
            btnPendingAppointments.Style = (Style)FindResource("TabButton");
            activeButton.Style = (Style)FindResource("TabButtonActive");
        }

        #endregion

        #region Load Data

        private void LoadRecentPatients()
        {
            try
            {
                var patients = _patientService.GetAll()
                    .Where(p => p.Status != "Draft" && p.Status != "Archived")
                    .OrderByDescending(p => p.DateCreated)
                    .Take(20)
                    .ToList();

                dgSearchResults.ItemsSource = patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent patients: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTodayRegistrations()
        {
            try
            {
                var today = DateTime.Today;
                var patients = _patientService.GetAll()
                    .Where(p => p.DateCreated.Date == today && p.Status != "Draft")
                    .OrderByDescending(p => p.DateCreated)
                    .ToList();

                dgSearchResults.ItemsSource = patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading today's registrations: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPendingAppointments()
        {
            try
            {
                // Get patients with pending appointments
                var patients = _patientService.GetAll()
                    .Where(p => p.Status == "Pending" || p.Status == "Active")
                    .OrderByDescending(p => p.LastVisit)
                    .Take(20)
                    .ToList();

                dgSearchResults.ItemsSource = patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading pending appointments: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search

        private void SearchPatient_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            try
            {
                string query = txtSearch.Text?.Trim();

                if (string.IsNullOrWhiteSpace(query))
                {
                    LoadRecentPatients();
                    return;
                }

                var results = _patientService.Search(query, status: null);
                dgSearchResults.ItemsSource = results;

                if (!results.Any())
                {
                    MessageBox.Show("No patients found matching your search.",
                                    "Search Results", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching patients: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgSearchResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgSearchResults.SelectedItem is Patient patient)
            {
                LoadPatientForEdit(patient);
            }
        }

        private void LoadPatientForEdit(Patient patient)
        {
            _editingPatientId = patient.Id;

            // Split FullName into parts
            var nameParts = (patient.FullName ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            txtFirstName.Text = patient.FirstName ?? (nameParts.Length > 0 ? nameParts[0] : "");
            txtLastName.Text = patient.LastName ?? (nameParts.Length > 1 ? nameParts[nameParts.Length - 1] : "");

            if (nameParts.Length > 2)
            {
                txtMiddleName.Text = string.Join(" ", nameParts.Skip(1).Take(nameParts.Length - 2));
            }

            dpBirthDate.SelectedDate = patient.BirthDate;

            if (!string.IsNullOrEmpty(patient.Gender))
            {
                foreach (ComboBoxItem item in cmbGender.Items)
                {
                    if (item.Content.ToString() == patient.Gender)
                    {
                        cmbGender.SelectedItem = item;
                        break;
                    }
                }
            }

            txtPhone.Text = patient.Contact ?? "";
            txtEmail.Text = patient.Email ?? "";
            txtAddress.Text = patient.Address ?? "";
            txtChiefComplaint.Text = patient.PrimaryDiagnosis ?? "";
            txtInitialCondition.Text = patient.SecondaryDiagnosis ?? "";
            txtAllergies.Text = patient.Allergies ?? "";

            MessageBox.Show($"Loaded patient: {patient.FullName}\nYou can now update their information.",
                            "Patient Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Validation

        private bool ValidateForm(bool isDraft = false)
        {
            if (isDraft)
            {
                // For drafts, only require first name
                if (string.IsNullOrWhiteSpace(txtFirstName.Text))
                {
                    MessageBox.Show("First Name is required even for drafts.",
                                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtFirstName.Focus();
                    return false;
                }
                return true;
            }

            // Required fields for registration
            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            {
                MessageBox.Show("First Name is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFirstName.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            {
                MessageBox.Show("Last Name is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLastName.Focus();
                return false;
            }

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Date of Birth is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }

            // Validate age (1-120 years)
            var age = CalculateAge(dpBirthDate.SelectedDate.Value);
            if (age < 0 || age > 120)
            {
                MessageBox.Show("Please enter a valid Date of Birth.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }

            if (cmbGender.SelectedIndex <= 0)
            {
                MessageBox.Show("Please select a Gender.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbGender.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtPhone.Text))
            {
                MessageBox.Show("Phone Number is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return false;
            }

            // Validate Philippine phone number format
            if (!ValidatePhilippinePhone(txtPhone.Text))
            {
                MessageBox.Show("Please enter a valid Philippine phone number.\nFormat: +63 9XX XXX XXXX or 09XX XXX XXXX",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPhone.Focus();
                return false;
            }

            // Validate email if provided
            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !ValidateEmail(txtEmail.Text))
            {
                MessageBox.Show("Please enter a valid email address.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtEmail.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtAddress.Text))
            {
                MessageBox.Show("Complete Address is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtAddress.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtChiefComplaint.Text))
            {
                MessageBox.Show("Chief Complaint / Reason for Visit is required.",
                                "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtChiefComplaint.Focus();
                return false;
            }

            //if (chkConsent.IsChecked != true)
            //{
            //    MessageBox.Show("Data Privacy Consent is required to proceed with registration.",
            //                    "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            //    chkConsent.Focus();
            //    return false;
            //}

            return true;
        }

        private bool ValidatePhilippinePhone(string phone)
        {
            // Remove spaces and special characters
            string cleaned = Regex.Replace(phone, @"[\s\-\(\)]", "");

            // Check formats: +639XXXXXXXXX or 09XXXXXXXXX or 9XXXXXXXXX
            return Regex.IsMatch(cleaned, @"^(\+639|09|9)\d{9}$");
        }

        private bool ValidateEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        #endregion

        #region Save Operations

        private void RegisterPatient_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(isDraft: false))
                return;

            try
            {
                var patient = CreatePatientFromForm();
                patient.Status = "Active";
                patient.DateCreated = DateTime.Now;
                patient.LastVisit = DateTime.Now;

                if (_editingPatientId.HasValue)
                {
                    // Update existing patient
                    patient.Id = _editingPatientId.Value;
                    var existing = _patientService.GetById(_editingPatientId.Value);
                    patient.PatientId = existing.PatientId; // Keep existing ID
                    patient.DateCreated = existing.DateCreated; // Keep original date

                    _patientService.UpdatePatient(patient);
                    MessageBox.Show($"Patient updated successfully!\nPatient ID: {patient.PatientId}",
                                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Generate new Patient ID
                    patient.PatientId = GeneratePatientId();
                    _patientService.AddPatient(patient);

                    // Auto-link any pending appointments with same name/contact
                    LinkPendingAppointmentsToPatient(patient);

                    // Ask if they want to schedule an appointment
                    var result = MessageBox.Show(
                        $"Patient registered successfully!\n\nPatient ID: {patient.PatientId}\nName: {patient.FullName}\n\nWould you like to schedule an appointment for this patient now?",
                        "Registration Successful",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Find the parent window and navigate to appointments
                        // Or you could open a quick appointment dialog here
                        try
                        {
                            var mainWindow = Window.GetWindow(this);
                            // If you have a navigation system, trigger it here
                            // For now, just inform the user
                            MessageBox.Show("Please use the Appointments tab to schedule an appointment.",
                                "Navigate to Appointments", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        catch { }
                    }
                }

                ClearForm();
                LoadRecentPatients();
                _editingPatientId = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error registering patient: {ex.Message}\n\n{ex.InnerException?.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsDraft_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(isDraft: true))
                return;

            try
            {
                var patient = CreatePatientFromForm();
                patient.Status = "Draft";
                patient.DateCreated = DateTime.Now;

                if (_editingPatientId.HasValue)
                {
                    patient.Id = _editingPatientId.Value;
                    var existing = _patientService.GetById(_editingPatientId.Value);
                    patient.PatientId = existing.PatientId;
                    patient.DateCreated = existing.DateCreated;
                    _patientService.UpdatePatient(patient);
                }
                else
                {
                    patient.PatientId = GeneratePatientId();
                    _patientService.AddPatient(patient);
                }

                MessageBox.Show("Patient information saved as draft successfully!",
                                "Draft Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                ClearForm();
                _editingPatientId = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving draft: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Patient CreatePatientFromForm()
        {
            string firstName = txtFirstName.Text?.Trim() ?? "";
            string middleName = txtMiddleName.Text?.Trim() ?? "";
            string lastName = txtLastName.Text?.Trim() ?? "";

            string fullName = string.IsNullOrEmpty(middleName)
                ? $"{firstName} {lastName}"
                : $"{firstName} {middleName} {lastName}";

            var gender = cmbGender.SelectedItem as ComboBoxItem;
            string genderValue = gender?.Content?.ToString();
            if (genderValue == "Select Gender") genderValue = null;

            // Format phone number to Philippine format
            string formattedPhone = FormatPhilippinePhone(txtPhone.Text);

            return new Patient
            {
                FirstName = firstName,
                LastName = lastName,
                FullName = fullName.Trim(),
                BirthDate = dpBirthDate.SelectedDate,
                Gender = genderValue,
                Contact = formattedPhone,
                Email = txtEmail.Text?.Trim(),
                Address = txtAddress.Text?.Trim(),
                PrimaryDiagnosis = txtChiefComplaint.Text?.Trim(),
                SecondaryDiagnosis = txtInitialCondition.Text?.Trim() +
                                     (string.IsNullOrWhiteSpace(txtClinicalNotes.Text) ? "" : "\n\nClinical Notes:\n" + txtClinicalNotes.Text?.Trim()),
                Allergies = string.IsNullOrWhiteSpace(txtAllergies.Text) ? "None" : txtAllergies.Text?.Trim(),
                BloodType = null // Will be filled later by doctor
            };
        }

        private string GeneratePatientId()
        {
            try
            {
                var year = DateTime.Now.Year;
                var lastPatient = _patientService.GetAll()
                    .Where(p => p.PatientId.StartsWith($"P-{year}"))
                    .OrderByDescending(p => p.PatientId)
                    .FirstOrDefault();

                int nextNumber = 1;
                if (lastPatient != null && !string.IsNullOrEmpty(lastPatient.PatientId))
                {
                    var parts = lastPatient.PatientId.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                    {
                        nextNumber = lastNumber + 1;
                    }
                }

                return $"P-{year}-{nextNumber:D4}";
            }
            catch
            {
                return $"P-{DateTime.Now.Year}-{new Random().Next(1, 9999):D4}";
            }
        }

        private string FormatPhilippinePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return phone;

            // Remove all non-digit characters
            string digits = Regex.Replace(phone, @"\D", "");

            // Format based on length
            if (digits.StartsWith("63") && digits.Length == 12)
            {
                // Already has country code
                return $"+{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 3)} {digits.Substring(8)}";
            }
            else if (digits.StartsWith("0") && digits.Length == 11)
            {
                // Add country code
                return $"+63 {digits.Substring(1, 3)} {digits.Substring(4, 3)} {digits.Substring(7)}";
            }
            else if (digits.StartsWith("9") && digits.Length == 10)
            {
                // Add country code and 0
                return $"+63 {digits.Substring(0, 3)} {digits.Substring(3, 3)} {digits.Substring(6)}";
            }

            return phone; // Return original if format is unexpected
        }

        #endregion

        #region Print Form

        private void PrintForm_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm(isDraft: true))
            {
                MessageBox.Show("Please fill in at least the basic patient information before printing.",
                                "Print Form", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var result = MessageBox.Show("Do you want to print the registration form?",
                                             "Print Form",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    PrintRegistrationForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing form: {ex.Message}",
                                "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintRegistrationForm()
        {
            var printDialog = new PrintDialog();

            if (printDialog.ShowDialog() == true)
            {
                // Create a FlowDocument for printing
                var doc = new FlowDocument();
                doc.PagePadding = new Thickness(50);
                doc.FontFamily = new FontFamily("Segoe UI");
                doc.FontSize = 12;

                // Header
                var header = new Paragraph();
                header.Inlines.Add(new Bold(new Run("ROSAL MEDICAL CLINIC AND SILANG MEDICAL LABORATORY")));
                header.FontSize = 18;
                header.TextAlignment = TextAlignment.Center;
                header.Margin = new Thickness(0, 0, 0, 10);
                doc.Blocks.Add(header);

                var subtitle = new Paragraph(new Run("Patient Registration Form"));
                subtitle.FontSize = 14;
                subtitle.TextAlignment = TextAlignment.Center;
                subtitle.Margin = new Thickness(0, 0, 0, 20);
                doc.Blocks.Add(subtitle);

                // Patient Information
                AddPrintSection(doc, "Personal Information");
                AddPrintField(doc, "Name", $"{txtFirstName.Text} {txtMiddleName.Text} {txtLastName.Text}".Trim());
                AddPrintField(doc, "Date of Birth", dpBirthDate.SelectedDate?.ToString("MMMM dd, yyyy") ?? "");
                AddPrintField(doc, "Age", dpBirthDate.SelectedDate.HasValue ? CalculateAge(dpBirthDate.SelectedDate.Value).ToString() : "");
                AddPrintField(doc, "Gender", (cmbGender.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "");

                AddPrintSection(doc, "Contact Information");
                AddPrintField(doc, "Phone Number", txtPhone.Text);
                AddPrintField(doc, "Email Address", txtEmail.Text);
                AddPrintField(doc, "Address", txtAddress.Text);

                AddPrintSection(doc, "Clinical Information");
                AddPrintField(doc, "Chief Complaint", txtChiefComplaint.Text);
                AddPrintField(doc, "Initial Condition", txtInitialCondition.Text);
                AddPrintField(doc, "Clinical Notes", txtClinicalNotes.Text);

                AddPrintSection(doc, "Basic Information");
                AddPrintField(doc, "Known Allergies", txtAllergies.Text);
                AddPrintField(doc, "Current Medications", txtCurrentMedications.Text);

                // Footer
                var footer = new Paragraph();
                footer.Inlines.Add(new Run($"\nDate Printed: {DateTime.Now:MMMM dd, yyyy hh:mm tt}"));
                footer.FontSize = 10;
                footer.Foreground = Brushes.Gray;
                footer.Margin = new Thickness(0, 20, 0, 0);
                doc.Blocks.Add(footer);

                // Print
                IDocumentPaginatorSource paginator = doc;
                printDialog.PrintDocument(paginator.DocumentPaginator, "Patient Registration Form");

                MessageBox.Show("Form printed successfully!", "Print Complete",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddPrintSection(FlowDocument doc, string title)
        {
            var section = new Paragraph();
            section.Inlines.Add(new Bold(new Run(title)));
            section.FontSize = 14;
            section.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            section.Margin = new Thickness(0, 15, 0, 10);
            section.BorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 50));
            section.BorderThickness = new Thickness(0, 0, 0, 2);
            doc.Blocks.Add(section);
        }

        private void AddPrintField(FlowDocument doc, string label, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;

            var field = new Paragraph();
            field.Inlines.Add(new Bold(new Run($"{label}: ")));
            field.Inlines.Add(new Run(value));
            field.Margin = new Thickness(0, 5, 0, 5);
            doc.Blocks.Add(field);
        }

        #endregion

        #region Helper Methods

        private void ClearForm()
        {
            txtFirstName.Clear();
            txtMiddleName.Clear();
            txtLastName.Clear();
            dpBirthDate.SelectedDate = null;
            cmbGender.SelectedIndex = 0;
            txtPhone.Clear();
            txtEmail.Clear();
            txtAddress.Clear();
            txtChiefComplaint.Clear();
            txtInitialCondition.Clear();
            txtClinicalNotes.Clear();
            txtAllergies.Clear();
            txtCurrentMedications.Clear();
            //chkConsent.IsChecked = false;
            txtSearch.Clear();
            _editingPatientId = null;
        }

        private void dpBirthDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Optional: Show age when date is selected
            if (dpBirthDate.SelectedDate.HasValue)
            {
                int age = CalculateAge(dpBirthDate.SelectedDate.Value);
                // You could display this in a label if needed
            }
        }

        #endregion

        private void LinkPendingAppointments(Patient patient)
        {
            try
            {
                var pendingAppointments = _db.Appointments
                    .Where(a => a.PatientId == null &&
                                a.PatientName.ToLower() == patient.FullName.ToLower() &&
                                a.Contact == patient.Contact)
                    .ToList();

                foreach (var appt in pendingAppointments)
                {
                    appt.PatientId = patient.Id;
                }

                if (pendingAppointments.Any())
                    _db.SaveChanges();
            }
            catch { /* Ignore linking errors */ }
        }

        #region Auto-Link Appointments

        /// <summary>
        /// Links any pending appointments to the newly registered patient
        /// Matches by name and contact number
        /// </summary>
        private void LinkPendingAppointmentsToPatient(Patient patient)
        {
            try
            {
                if (patient == null || string.IsNullOrEmpty(patient.FullName)) return;

                // Normalize contact for comparison
                var normalizedPatientContact = NormalizePhoneNumber(patient.Contact);

                var pendingAppointments = _db.Appointments
                    .Where(a => a.PatientId == null)
                    .ToList()
                    .Where(a =>
                        a.PatientName?.Trim().ToLower() == patient.FullName.Trim().ToLower() &&
                        NormalizePhoneNumber(a.Contact) == normalizedPatientContact)
                    .ToList();

                if (pendingAppointments.Any())
                {
                    foreach (var appt in pendingAppointments)
                    {
                        appt.PatientId = patient.Id;
                    }
                    _db.SaveChanges();

                    // Notify user
                    MessageBox.Show(
                        $"Found and linked {pendingAppointments.Count} existing appointment(s) to this patient.",
                        "Appointments Linked",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LinkPendingAppointments error: {ex.Message}");
            }
        }

        /// <summary>
        /// Normalizes phone number for comparison
        /// </summary>
        private string NormalizePhoneNumber(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";
            return System.Text.RegularExpressions.Regex.Replace(phone, @"[\s\-\(\)\+]", "");
        }

        #endregion
    }


}
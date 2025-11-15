using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class EditPatientDialog : Window
    {
        private readonly Patient _patient;
        private readonly PatientService _patientService;

        public EditPatientDialog(Patient patient, PatientService patientService)
        {
            InitializeComponent();

            _patient = patient ?? throw new ArgumentNullException(nameof(patient));
            _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));

            LoadPatientData();
        }

        private void LoadPatientData()
        {
            txtFullName.Text = _patient.FullName;
            dpBirthDate.SelectedDate = _patient.BirthDate;
            cbGender.Text = _patient.Gender;
            txtContact.Text = _patient.Contact;
            txtEmail.Text = _patient.Email;
            txtAddress.Text = _patient.Address;
            cbBloodType.Text = _patient.BloodType;
            txtHeight.Text = _patient.Height;
            txtWeight.Text = _patient.Weight;
            txtPrimaryDiagnosis.Text = _patient.PrimaryDiagnosis;
            txtSecondaryDiagnosis.Text = _patient.SecondaryDiagnosis;
            txtAllergies.Text = _patient.Allergies;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                _patient.FullName = txtFullName.Text.Trim();
                _patient.BirthDate = dpBirthDate.SelectedDate;
                _patient.Gender = cbGender.Text;
                _patient.Contact = txtContact.Text.Trim();
                _patient.Email = txtEmail.Text.Trim();
                _patient.Address = txtAddress.Text.Trim();
                _patient.BloodType = cbBloodType.Text;
                _patient.Height = txtHeight.Text.Trim();
                _patient.Weight = txtWeight.Text.Trim();
                _patient.PrimaryDiagnosis = txtPrimaryDiagnosis.Text.Trim();
                _patient.SecondaryDiagnosis = txtSecondaryDiagnosis.Text.Trim();
                _patient.Allergies = txtAllergies.Text.Trim();

                _patientService.UpdatePatient(_patient);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving patient:\n{ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter patient's full name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtFullName.Focus();
                return false;
            }

            if (!dpBirthDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select date of birth.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpBirthDate.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(cbGender.Text))
            {
                MessageBox.Show("Please select gender.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cbGender.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(txtContact.Text))
            {
                MessageBox.Show("Please enter contact number.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtContact.Focus();
                return false;
            }

            return true;
        }
    }
}
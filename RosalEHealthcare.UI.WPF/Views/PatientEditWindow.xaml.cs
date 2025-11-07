using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class PatientEditWindow : Window
    {
        private readonly PatientService _service;
        private Patient _patient;

        public PatientEditWindow(int patientId)
        {
            InitializeComponent();
            var db = new RosalEHealthcareDbContext();
            _service = new PatientService(db);
            Load(patientId);
        }

        private void Load(int id)
        {
            _patient = _service.GetById(id);
            if (_patient == null)
            {
                MessageBox.Show("Patient not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            TxtFullName.Text = _patient.FullName;
            TxtPatientId.Text = _patient.PatientId;
            DpBirthDate.SelectedDate = _patient.BirthDate;
            CbGender.SelectedValue = _patient.Gender;
            TxtContact.Text = _patient.Contact;
            TxtEmail.Text = _patient.Email;
            TxtAddress.Text = _patient.Address;
            TxtPrimaryDiagnosis.Text = _patient.PrimaryDiagnosis;
            TxtSecondaryDiagnosis.Text = _patient.SecondaryDiagnosis;
            TxtAllergies.Text = _patient.Allergies;
            TxtBloodType.Text = _patient.BloodType;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            _patient.FullName = TxtFullName.Text?.Trim();
            _patient.PatientId = TxtPatientId.Text?.Trim();
            _patient.BirthDate = DpBirthDate.SelectedDate;
            _patient.Gender = (CbGender.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _patient.Contact = TxtContact.Text?.Trim();
            _patient.Email = TxtEmail.Text?.Trim();
            _patient.Address = TxtAddress.Text?.Trim();
            _patient.PrimaryDiagnosis = TxtPrimaryDiagnosis.Text?.Trim();
            _patient.SecondaryDiagnosis = TxtSecondaryDiagnosis.Text?.Trim();
            _patient.Allergies = TxtAllergies.Text?.Trim();
            _patient.BloodType = TxtBloodType.Text?.Trim();

            try
            {
                _service.UpdatePatient(_patient);
                MessageBox.Show("Patient updated.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}

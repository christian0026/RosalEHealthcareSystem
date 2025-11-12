using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPatientRecords : UserControl
    {
        private readonly PatientService _svc;
        public Patient SelectedPatient { get; private set; }

        public DoctorPatientRecords()
        {
            InitializeComponent();

            try
            {
                var db = new RosalEHealthcareDbContext();
                _svc = new PatientService(db);
                LoadPatients();
                DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Patient Records:\n{ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatients()
        {
            try
            {
                var list = _svc.GetAll()?.ToList() ?? new System.Collections.Generic.List<Patient>();
                lvPatients.ItemsSource = list;
                if (list.Any())
                {
                    lvPatients.SelectedIndex = 0;
                    SelectedPatient = list.First();
                }
                else
                {
                    SelectedPatient = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading patients:\n{ex.Message}",
                                "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = txtSearch.Text ?? "";
            var results = _svc.Search(q);
            lvPatients.ItemsSource = results;
        }

        private void LvPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedPatient = lvPatients.SelectedItem as Patient;
            DataContext = null;
            DataContext = this;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Editing patient: {SelectedPatient.FullName}");
        }

        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to archive {SelectedPatient.FullName}?",
                                "Confirm Archive", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _svc.ArchivePatient(SelectedPatient.Id);
                LoadPatients();
                MessageBox.Show("Patient archived successfully.", "Archived", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            panelPersonal.Visibility = tbPersonal.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelMedical.Visibility = tbMedical.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelVisits.Visibility = tbVisits.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            panelPresc.Visibility = tbPresc.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}

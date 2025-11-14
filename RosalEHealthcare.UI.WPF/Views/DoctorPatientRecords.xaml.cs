using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorPatientRecords : UserControl, INotifyPropertyChanged
    {
        private readonly RosalEHealthcareDbContext _db;
        private Patient _selectedPatient;

        public ObservableCollection<Patient> Patients { get; set; }

        public Patient SelectedPatient
        {
            get { return _selectedPatient; }
            set
            {
                _selectedPatient = value;
                OnPropertyChanged(nameof(SelectedPatient));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public DoctorPatientRecords()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                Patients = new ObservableCollection<Patient>();

                DataContext = this;

                Loaded += (s, e) => LoadPatients();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error initializing Patient Records:\n{0}", ex.Message),
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadPatients()
        {
            try
            {
                var patients = await Task.Run(() => _db.Patients.OrderBy(p => p.FullName).ToList());

                Patients.Clear();
                foreach (var patient in patients)
                {
                    Patients.Add(patient);
                }

                lvPatients.ItemsSource = Patients;

                if (Patients.Count > 0)
                {
                    lvPatients.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error loading patients:\n{0}\n\nInner: {1}",
                    ex.Message, ex.InnerException != null ? ex.InnerException.Message : ""),
                    "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = txtSearch.Text;
            if (searchText == null) searchText = "";
            searchText = searchText.Trim().ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                lvPatients.ItemsSource = Patients;
                return;
            }

            var filtered = Patients.Where(p =>
                (p.FullName != null && p.FullName.ToLower().Contains(searchText)) ||
                (p.PatientId != null && p.PatientId.ToLower().Contains(searchText)) ||
                (p.PrimaryDiagnosis != null && p.PrimaryDiagnosis.ToLower().Contains(searchText))
            ).ToList();

            lvPatients.ItemsSource = filtered;
        }

        private void LvPatients_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvPatients.SelectedItem is Patient patient)
            {
                SelectedPatient = patient;
            }
        }

        private void Tab_Checked(object sender, RoutedEventArgs e)
        {
            if (panelPersonal == null) return; // Controls not initialized yet

            panelPersonal.Visibility = Visibility.Collapsed;
            panelMedical.Visibility = Visibility.Collapsed;
            panelVisits.Visibility = Visibility.Collapsed;
            panelPresc.Visibility = Visibility.Collapsed;

            if (sender == tbPersonal)
                panelPersonal.Visibility = Visibility.Visible;
            else if (sender == tbMedical)
                panelMedical.Visibility = Visibility.Visible;
            else if (sender == tbVisits)
                panelVisits.Visibility = Visibility.Visible;
            else if (sender == tbPresc)
                panelPresc.Visibility = Visibility.Visible;
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("Edit functionality will be implemented soon.",
                "Edit Patient", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Please select a patient first.", "No Patient Selected",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Are you sure you want to archive {0}?", SelectedPatient.FullName),
                "Confirm Archive", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    SelectedPatient.Status = "Archived";
                    _db.SaveChanges();
                    MessageBox.Show("Patient archived successfully.", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadPatients();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Error archiving patient:\n{0}", ex.Message),
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
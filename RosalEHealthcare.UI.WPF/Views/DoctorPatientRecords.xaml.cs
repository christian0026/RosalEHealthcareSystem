using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
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
            var db = new RosalEHealthcareDbContext();
            _svc = new PatientService(db);
            LoadPatients();
            DataContext = this;
        }

        private void LoadPatients()
        {
            var list = _svc.GetAll();
            lvPatients.ItemsSource = list;
            lvPatients.SelectedIndex = 0;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = txtSearch.Text;
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
                MessageBox.Show("Select a patient first.");
                return;
            }

            MessageBox.Show($"Editing {SelectedPatient.FullName} (implement your edit dialog here).");
        }

        private void BtnArchive_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Select a patient first.");
                return;
            }

            if (MessageBox.Show("Archive this patient?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                _svc.ArchivePatient(SelectedPatient.Id);
                LoadPatients();
                MessageBox.Show("Patient archived successfully.");
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

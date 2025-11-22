using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ViewMedicineDialog : Window
    {
        private readonly MedicineService _medicineService;
        private readonly RosalEHealthcareDbContext _db;
        private readonly int _medicineId;

        public ViewMedicineDialog(int medicineId)
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _medicineService = new MedicineService(_db);
            _medicineId = medicineId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadMedicineDetails();
        }

        private void LoadMedicineDetails()
        {
            try
            {
                var medicine = _medicineService.GetById(_medicineId);
                if (medicine == null)
                {
                    MessageBox.Show("Medicine not found.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Set initials
                var name = medicine.Name ?? "?";
                var words = name.Split(' ');
                if (words.Length >= 2)
                    txtInitials.Text = $"{words[0][0]}{words[1][0]}".ToUpper();
                else
                    txtInitials.Text = name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();

                // Set details
                txtMedicineName.Text = medicine.Name;
                txtMedicineId.Text = medicine.MedicineId ?? "N/A";
                txtGenericName.Text = medicine.GenericName ?? "-";
                txtBrand.Text = medicine.Brand ?? "-";
                txtCategory.Text = medicine.Category ?? "-";
                txtType.Text = medicine.Type ?? "-";
                txtStrength.Text = medicine.Strength ?? "-";
                txtStock.Text = medicine.Stock.ToString();
                txtPrice.Text = $"₱{medicine.Price:N2}";
                txtExpiryDate.Text = medicine.ExpiryDate.ToString("MMMM dd, yyyy");
                txtStatus.Text = medicine.Status ?? "Unknown";

                // Set status color
                switch (medicine.Status)
                {
                    case "Available":
                        statusBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                        break;
                    case "Low Stock":
                        statusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                        break;
                    case "Out of Stock":
                        statusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                        break;
                    case "Expiring Soon":
                        statusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                        txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                        break;
                }

                // Highlight expiry date if expiring
                var threeMonthsFromNow = DateTime.Now.AddMonths(3);
                if (medicine.ExpiryDate <= threeMonthsFromNow && medicine.ExpiryDate >= DateTime.Now)
                {
                    txtExpiryDate.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                    txtExpiryDate.FontWeight = FontWeights.SemiBold;
                }
                else if (medicine.ExpiryDate < DateTime.Now)
                {
                    txtExpiryDate.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    txtExpiryDate.FontWeight = FontWeights.Bold;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading medicine details: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
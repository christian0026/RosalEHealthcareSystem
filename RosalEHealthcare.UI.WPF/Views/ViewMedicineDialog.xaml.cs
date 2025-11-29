using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ViewMedicineDialog : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly MedicineService _medicineService;
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

                // Header
                txtMedicineName.Text = medicine.Name;
                txtMedicineId.Text = medicine.MedicineId;

                // Status with color
                txtStatus.Text = medicine.Status;
                SetStatusColor(medicine.Status);

                // Basic Information
                txtGenericName.Text = medicine.GenericName ?? "N/A";
                txtBrand.Text = medicine.Brand ?? "N/A";
                txtCategory.Text = medicine.Category ?? "N/A";
                txtType.Text = medicine.Type ?? "N/A";
                txtStrength.Text = medicine.Strength ?? "N/A";
                txtUnit.Text = medicine.Unit ?? "N/A";

                // Stock & Pricing
                txtStock.Text = $"{medicine.Stock} units";
                txtMinStock.Text = $"{medicine.MinimumStockLevel} units";
                txtPrice.Text = $"₱{medicine.Price:N2}";

                // Set stock color
                if (medicine.Stock == 0)
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                }
                else if (medicine.Stock <= medicine.MinimumStockLevel)
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                }
                else
                {
                    txtStock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }

                // Expiry Information
                txtExpiryDate.Text = medicine.ExpiryDate.ToString("MMMM yyyy");

                // Check if expiring or expired
                var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Now).Days;
                if (daysUntilExpiry < 0)
                {
                    txtExpiryDate.Foreground = new SolidColorBrush(Color.FromRgb(211, 47, 47));
                    txtExpiryDate.Text += " (EXPIRED)";
                }
                else if (daysUntilExpiry <= 90)
                {
                    txtExpiryDate.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    txtExpiryDate.Text += $" ({daysUntilExpiry} days remaining)";
                }

                // Additional Information
                if (string.IsNullOrWhiteSpace(medicine.Notes))
                {
                    txtNotes.Text = "No notes available";
                    txtNotes.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                }
                else
                {
                    txtNotes.Text = medicine.Notes;
                    txtNotes.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                }

                // Last Modified
                txtLastModifiedBy.Text = medicine.LastModifiedBy ?? "System";
                txtLastModifiedAt.Text = medicine.LastModifiedAt?.ToString("MMM dd, yyyy hh:mm tt") ?? "N/A";

                // Hide edit button if archived
                if (!medicine.IsActive)
                {
                    btnEdit.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading medicine details: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void SetStatusColor(string status)
        {
            switch (status)
            {
                case "Available":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    break;
                case "Low Stock":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                    break;
                case "Out of Stock":
                case "Expired":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    break;
                case "Expiring Soon":
                    StatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    txtStatus.Foreground = new SolidColorBrush(Color.FromRgb(245, 124, 0));
                    break;
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var editDialog = new AddEditMedicineDialog(_medicineId);
            if (editDialog.ShowDialog() == true)
            {
                // Reload data
                LoadMedicineDetails();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
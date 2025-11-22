using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditMedicineDialog : Window
    {
        private readonly MedicineService _medicineService;
        private readonly RosalEHealthcareDbContext _db;
        private readonly int? _medicineId;
        private bool _isEditMode;

        public AddEditMedicineDialog(int? medicineId = null)
        {
            InitializeComponent();

            _db = new RosalEHealthcareDbContext();
            _medicineService = new MedicineService(_db);
            _medicineId = medicineId;
            _isEditMode = medicineId.HasValue;

            if (_isEditMode)
            {
                Title = "Edit Medicine";
                txtTitle.Text = "Edit Medicine";
                btnSave.Content = "Update Medicine";
            }

            // Set default expiry date (1 year from now)
            dpExpiryDate.SelectedDate = DateTime.Now.AddYears(1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isEditMode)
            {
                LoadMedicineData();
            }
        }

        private void LoadMedicineData()
        {
            try
            {
                var medicine = _medicineService.GetById(_medicineId.Value);
                if (medicine != null)
                {
                    txtName.Text = medicine.Name;
                    txtGenericName.Text = medicine.GenericName;
                    txtBrand.Text = medicine.Brand;
                    txtStrength.Text = medicine.Strength;
                    txtStock.Text = medicine.Stock.ToString();
                    txtPrice.Text = medicine.Price.ToString("F2");
                    dpExpiryDate.SelectedDate = medicine.ExpiryDate;

                    // Set ComboBoxes
                    SetComboBoxValue(cbCategory, medicine.Category);
                    SetComboBoxValue(cbType, medicine.Type);
                    SetComboBoxValue(cbUnit, medicine.Unit);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading medicine: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void SetComboBoxValue(System.Windows.Controls.ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            foreach (System.Windows.Controls.ComboBoxItem item in comboBox.Items)
            {
                if (item.Content.ToString() == value)
                {
                    comboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter medicine name.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            if (cbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a category.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCategory.Focus();
                return;
            }

            if (cbType.SelectedItem == null)
            {
                MessageBox.Show("Please select a type.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cbType.Focus();
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Please enter a valid stock quantity.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid price.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return;
            }

            if (!dpExpiryDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select an expiry date.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpExpiryDate.Focus();
                return;
            }

            try
            {
                Medicine medicine;

                if (_isEditMode)
                {
                    medicine = _medicineService.GetById(_medicineId.Value);
                    if (medicine == null)
                    {
                        MessageBox.Show("Medicine not found.", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    medicine = new Medicine();
                }

                // Update medicine properties
                medicine.Name = txtName.Text.Trim();
                medicine.GenericName = txtGenericName.Text.Trim();
                medicine.Brand = txtBrand.Text.Trim();
                medicine.Category = (cbCategory.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                medicine.Type = (cbType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                medicine.Strength = txtStrength.Text.Trim();
                medicine.Unit = (cbUnit.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString();
                medicine.Stock = stock;
                medicine.Price = price;
                medicine.ExpiryDate = dpExpiryDate.SelectedDate.Value;

                if (_isEditMode)
                {
                    _medicineService.UpdateMedicine(medicine);
                    MessageBox.Show("Medicine updated successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _medicineService.AddMedicine(medicine);
                    MessageBox.Show("Medicine added successfully!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving medicine: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
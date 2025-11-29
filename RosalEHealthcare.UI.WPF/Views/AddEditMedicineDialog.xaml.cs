using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class AddEditMedicineDialog : Window
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly MedicineService _medicineService;
        private readonly int? _medicineId;
        private bool _isEditMode;

        public AddEditMedicineDialog(int? medicineId = null)
        {
            InitializeComponent();
            _db = new RosalEHealthcareDbContext();
            _medicineService = new MedicineService(_db);
            _medicineId = medicineId;
            _isEditMode = medicineId.HasValue;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isEditMode)
            {
                txtTitle.Text = "Edit Medicine";
                btnSave.Content = "Update Medicine";
                LoadMedicineData();
            }
            else
            {
                // Set defaults for new medicine
                txtStock.Text = "0";
                txtMinStock.Text = "10";
                txtPrice.Text = "0.00";
                dpExpiryDate.SelectedDate = DateTime.Now.AddYears(2);
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

                    // Select category
                    foreach (var item in cbCategory.Items)
                    {
                        if (item is System.Windows.Controls.ComboBoxItem cbi &&
                            cbi.Content.ToString() == medicine.Category)
                        {
                            cbCategory.SelectedItem = cbi;
                            break;
                        }
                    }

                    // Select type
                    foreach (var item in cbType.Items)
                    {
                        if (item is System.Windows.Controls.ComboBoxItem cbi &&
                            cbi.Content.ToString() == medicine.Type)
                        {
                            cbType.SelectedItem = cbi;
                            break;
                        }
                    }

                    txtStrength.Text = medicine.Strength;

                    // Select unit
                    foreach (var item in cbUnit.Items)
                    {
                        if (item is System.Windows.Controls.ComboBoxItem cbi &&
                            cbi.Content.ToString() == medicine.Unit)
                        {
                            cbUnit.SelectedItem = cbi;
                            break;
                        }
                    }

                    txtStock.Text = medicine.Stock.ToString();
                    txtMinStock.Text = medicine.MinimumStockLevel.ToString();
                    txtPrice.Text = medicine.Price.ToString("F2");
                    dpExpiryDate.SelectedDate = medicine.ExpiryDate;
                    txtNotes.Text = medicine.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading medicine data: {ex.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
                return;

            try
            {
                var medicine = _isEditMode
                    ? _medicineService.GetById(_medicineId.Value)
                    : new Medicine();

                // Update properties
                medicine.Name = txtName.Text.Trim();
                medicine.GenericName = txtGenericName.Text.Trim();
                medicine.Brand = txtBrand.Text.Trim();
                medicine.Category = (cbCategory.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                medicine.Type = (cbType.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                medicine.Strength = txtStrength.Text.Trim();
                medicine.Unit = (cbUnit.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();
                medicine.Stock = int.Parse(txtStock.Text);
                medicine.MinimumStockLevel = int.Parse(txtMinStock.Text);
                medicine.Price = decimal.Parse(txtPrice.Text);
                medicine.ExpiryDate = dpExpiryDate.SelectedDate ?? DateTime.Now.AddYears(2);
                medicine.Notes = txtNotes.Text.Trim();

                string userName = SessionManager.CurrentUser?.FullName ?? "System";

                if (_isEditMode)
                {
                    _medicineService.UpdateMedicine(medicine, userName);
                    MessageBox.Show("Medicine updated successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _medicineService.AddMedicine(medicine, userName);
                    MessageBox.Show("Medicine added successfully!",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving medicine: {ex.Message}",
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValidateInputs()
        {
            // Medicine Name
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Please enter medicine name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return false;
            }

            // Category
            if (cbCategory.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a category.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cbCategory.Focus();
                return false;
            }

            // Type
            if (cbType.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a type.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                cbType.Focus();
                return false;
            }

            // Stock
            if (!int.TryParse(txtStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Please enter a valid stock quantity (0 or greater).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtStock.Focus();
                return false;
            }

            // Minimum Stock
            if (!int.TryParse(txtMinStock.Text, out int minStock) || minStock < 0)
            {
                MessageBox.Show("Please enter a valid minimum stock level (0 or greater).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtMinStock.Focus();
                return false;
            }

            // Price
            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid price (0 or greater).", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrice.Focus();
                return false;
            }

            // Expiry Date
            if (!dpExpiryDate.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select an expiry date.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                dpExpiryDate.Focus();
                return false;
            }

            if (dpExpiryDate.SelectedDate.Value < DateTime.Now)
            {
                var result = MessageBox.Show(
                    "The expiry date is in the past. This medicine will be marked as expired.\n\nDo you want to continue?",
                    "Past Expiry Date",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    dpExpiryDate.Focus();
                    return false;
                }
            }

            return true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #region Input Validation

        private void NumericOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9.]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        #endregion
    }
}
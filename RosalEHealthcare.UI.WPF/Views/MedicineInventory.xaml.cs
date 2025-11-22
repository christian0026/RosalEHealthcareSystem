using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class MedicineInventory : UserControl
    {
        private readonly MedicineService _medicineService;
        private readonly RosalEHealthcareDbContext _db;

        private List<MedicineViewModel> _allMedicines;
        private List<MedicineViewModel> _filteredMedicines;

        // Pagination
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;

        public MedicineInventory()
        {
            InitializeComponent();

            try
            {
                _db = new RosalEHealthcareDbContext();
                _medicineService = new MedicineService(_db);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing Medicine Inventory: {ex.Message}",
                    "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            ShowLoading(true);

            try
            {
                // Load statistics
                await LoadStatisticsAsync();

                // Load categories for filter
                await LoadCategoriesAsync();

                // Load medicines
                await LoadMedicinesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}\n\n{ex.InnerException?.Message}",
                    "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ShowLoading(false);
            }
        }

        private async Task LoadStatisticsAsync()
        {
            await Task.Run(() =>
            {
                var totalMedicines = _medicineService.GetTotalMedicines();
                var lowStock = _medicineService.GetLowStockCount();
                var expiringSoon = _medicineService.GetExpiringSoonCount();
                var outOfStock = _medicineService.GetOutOfStockCount();

                Dispatcher.Invoke(() =>
                {
                    cardTotalMedicines.Value = totalMedicines.ToString();
                    cardLowStock.Value = lowStock.ToString();
                    cardExpiring.Value = expiringSoon.ToString();
                    cardOutOfStock.Value = outOfStock.ToString();
                });
            });
        }

        private async Task LoadCategoriesAsync()
        {
            await Task.Run(() =>
            {
                var categories = _medicineService.GetAllCategories().ToList();

                Dispatcher.Invoke(() =>
                {
                    cbCategory.Items.Clear();
                    cbCategory.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });

                    foreach (var category in categories)
                    {
                        cbCategory.Items.Add(new ComboBoxItem { Content = category });
                    }
                });
            });
        }

        private async Task LoadMedicinesAsync()
        {
            await Task.Run(() =>
            {
                var medicines = _medicineService.GetAllMedicines().ToList();

                _allMedicines = medicines.Select(m => new MedicineViewModel
                {
                    Id = m.Id,
                    MedicineId = m.MedicineId ?? "N/A",
                    Name = m.Name ?? "Unknown",
                    GenericName = m.GenericName ?? "",
                    Brand = m.Brand ?? "",
                    Category = m.Category ?? "Uncategorized",
                    Stock = m.Stock,
                    Price = m.Price,
                    ExpiryDate = m.ExpiryDate,
                    Status = m.Status ?? "Unknown",
                    Type = m.Type ?? "",
                    Strength = m.Strength ?? "",
                    Unit = m.Unit ?? ""
                }).ToList();

                _filteredMedicines = new List<MedicineViewModel>(_allMedicines);

                Dispatcher.Invoke(() =>
                {
                    ApplyPagination();
                });
            });
        }

        private void ApplyFilters()
        {
            var searchQuery = txtSearch.Text?.Trim().ToLower() ?? "";
            var selectedCategory = (cbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";
            var selectedStatus = (cbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Status";

            _filteredMedicines = _allMedicines.Where(m =>
            {
                // Search filter
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                    m.Name.ToLower().Contains(searchQuery) ||
                    m.MedicineId.ToLower().Contains(searchQuery) ||
                    m.GenericName.ToLower().Contains(searchQuery);

                // Category filter
                bool matchesCategory = selectedCategory == "All Categories" ||
                    m.Category == selectedCategory;

                // Status filter
                bool matchesStatus = selectedStatus == "All Status" ||
                    m.Status == selectedStatus;

                return matchesSearch && matchesCategory && matchesStatus;
            }).ToList();

            _currentPage = 1;
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            _totalPages = (int)Math.Ceiling((double)_filteredMedicines.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var pagedMedicines = _filteredMedicines
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            dgMedicines.ItemsSource = pagedMedicines;

            // Update UI
            txtResultCount.Text = $"Showing {pagedMedicines.Count} of {_filteredMedicines.Count} medicines";
            txtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";

            // Update pagination buttons
            btnFirst.IsEnabled = _currentPage > 1;
            btnPrevious.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < _totalPages;
            btnLast.IsEnabled = _currentPage < _totalPages;
        }

        private void ShowLoading(bool show)
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Event Handlers

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allMedicines != null)
            {
                ApplyFilters();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddEditMedicineDialog();
            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var medicine = _allMedicines.FirstOrDefault(m => m.Id == id);
                if (medicine != null)
                {
                    var dialog = new ViewMedicineDialog(id);
                    dialog.ShowDialog();
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var dialog = new AddEditMedicineDialog(id);
                if (dialog.ShowDialog() == true)
                {
                    _ = LoadDataAsync();
                }
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this medicine?\n\nThis action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ShowLoading(true);

                        await Task.Run(() =>
                        {
                            _medicineService.DeleteMedicine(id);
                        });

                        MessageBox.Show("Medicine deleted successfully!",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadDataAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting medicine: {ex.Message}",
                            "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    finally
                    {
                        ShowLoading(false);
                    }
                }
            }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyPagination();
        }

        private void BtnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                ApplyPagination();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                ApplyPagination();
            }
        }

        private void BtnLast_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            ApplyPagination();
        }

        #endregion
    }

    #region ViewModel

    public class MedicineViewModel : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string MedicineId { get; set; }
        public string Name { get; set; }
        public string GenericName { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Strength { get; set; }
        public string Unit { get; set; }

        // Computed Properties
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return "?";
                var words = Name.Split(' ');
                if (words.Length >= 2)
                    return $"{words[0][0]}{words[1][0]}".ToUpper();
                return Name.Length >= 2 ? Name.Substring(0, 2).ToUpper() : Name.ToUpper();
            }
        }

        public string PriceFormatted => $"₱{Price:N2}";

        public string ExpiryDateFormatted => ExpiryDate.ToString("MMM yyyy");

        public bool IsExpiring
        {
            get
            {
                var threeMonthsFromNow = DateTime.Now.AddMonths(3);
                return ExpiryDate <= threeMonthsFromNow && ExpiryDate >= DateTime.Now;
            }
        }

        public bool IsExpired => ExpiryDate < DateTime.Now;

        public string StockStatus
        {
            get
            {
                if (Stock == 0) return "Critical";
                if (Stock <= 20) return "Low";
                return "Good";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    #endregion
}
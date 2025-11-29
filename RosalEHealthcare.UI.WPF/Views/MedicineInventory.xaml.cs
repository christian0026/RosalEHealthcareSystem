using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RosalEHealthcare.UI.WPF.Views
{
    // 1. MAIN CONTROL
    public partial class MedicineInventory : UserControl
    {
        private MedicineService _medicineService;
        private MedicineExportService _exportService;
        private ActivityLogService _activityLogService;
        private RosalEHealthcareDbContext _db;

        private bool _isInitialized = false;
        private List<MedicineViewModel> _allMedicines;
        private List<MedicineViewModel> _filteredMedicines;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private bool _showArchived = false;

        // Static constructor to init PDF license once
        static MedicineInventory()
        {
            try
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            }
            catch { }
        }

        public MedicineInventory()
        {
            InitializeComponent();
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                _db = new RosalEHealthcareDbContext();
                var conn = _db.Database.Connection; // Test connection

                _medicineService = new MedicineService(_db);
                _activityLogService = new ActivityLogService(_db);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                MessageBox.Show($"Database Connection Failed:\n{ex.Message}", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                _exportService = new MedicineExportService();
            }
            catch
            {
                _exportService = null;
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            if (_allMedicines == null)
            {
                await LoadDataAsync();
            }

            if (_exportService == null && btnExport != null)
            {
                btnExport.IsEnabled = false;
                btnExport.ToolTip = "Export service unavailable (Install SkiaSharp.NativeAssets.Win32)";
            }
        }

        private async Task LoadDataAsync()
        {
            ShowLoading(true);
            try
            {
                await LoadStatisticsAsync();
                await LoadCategoriesAsync();
                await LoadMedicinesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                try
                {
                    if (_medicineService == null) return;
                    var totalMedicines = _medicineService.GetTotalMedicines(_showArchived);
                    var lowStock = _medicineService.GetLowStockCount();
                    var expiringSoon = _medicineService.GetExpiringSoonCount();
                    var outOfStock = _medicineService.GetOutOfStockCount();

                    Dispatcher.Invoke(() =>
                    {
                        if (cardTotalMedicines != null) cardTotalMedicines.Value = totalMedicines.ToString();
                        if (cardLowStock != null) cardLowStock.Value = lowStock.ToString();
                        if (cardExpiring != null) cardExpiring.Value = expiringSoon.ToString();
                        if (cardOutOfStock != null) cardOutOfStock.Value = outOfStock.ToString();
                    });
                }
                catch { }
            });
        }

        private async Task LoadCategoriesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_medicineService == null) return;
                    var categories = _medicineService.GetAllCategories().ToList();

                    Dispatcher.Invoke(() =>
                    {
                        if (cbCategory == null) return;
                        cbCategory.SelectionChanged -= FilterChanged;
                        cbCategory.Items.Clear();
                        cbCategory.Items.Add(new ComboBoxItem { Content = "All Categories", IsSelected = true });

                        foreach (var category in categories)
                        {
                            cbCategory.Items.Add(new ComboBoxItem { Content = category });
                        }
                        cbCategory.SelectionChanged += FilterChanged;
                    });
                }
                catch { }
            });
        }

        private async Task LoadMedicinesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_medicineService == null) return;
                    var medicines = _medicineService.GetAllMedicines(_showArchived).ToList();

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
                        Unit = m.Unit ?? "",
                        MinimumStockLevel = m.MinimumStockLevel,
                        IsActive = m.IsActive
                    }).ToList();

                    _filteredMedicines = new List<MedicineViewModel>(_allMedicines);

                    Dispatcher.Invoke(() =>
                    {
                        ApplyPagination();
                    });
                }
                catch { }
            });
        }

        private void ApplyFilters()
        {
            if (_allMedicines == null) return;

            var searchQuery = txtSearch.Text?.Trim().ToLower() ?? "";
            var selectedCategory = (cbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Categories";
            var selectedStatus = (cbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All Status";

            _filteredMedicines = _allMedicines.Where(m =>
            {
                bool matchesSearch = string.IsNullOrEmpty(searchQuery) ||
                    m.Name.ToLower().Contains(searchQuery) ||
                    m.MedicineId.ToLower().Contains(searchQuery) ||
                    m.GenericName.ToLower().Contains(searchQuery);

                bool matchesCategory = selectedCategory == "All Categories" || m.Category == selectedCategory;
                bool matchesStatus = selectedStatus == "All Status" || m.Status == selectedStatus;

                return matchesSearch && matchesCategory && matchesStatus;
            }).ToList();

            _currentPage = 1;
            ApplyPagination();
        }

        private void ApplyPagination()
        {
            if (_filteredMedicines == null || dgMedicines == null) return;

            _totalPages = (int)Math.Ceiling((double)_filteredMedicines.Count / _pageSize);
            if (_totalPages == 0) _totalPages = 1;

            var pagedMedicines = _filteredMedicines
                .Skip((_currentPage - 1) * _pageSize)
                .Take(_pageSize)
                .ToList();

            dgMedicines.ItemsSource = pagedMedicines;

            if (txtResultCount != null) txtResultCount.Text = $"Showing {pagedMedicines.Count} of {_filteredMedicines.Count} medicines";
            if (txtPageInfo != null) txtPageInfo.Text = $"Page {_currentPage} of {_totalPages}";

            if (btnFirst != null) btnFirst.IsEnabled = _currentPage > 1;
            if (btnPrevious != null) btnPrevious.IsEnabled = _currentPage > 1;
            if (btnNext != null) btnNext.IsEnabled = _currentPage < _totalPages;
            if (btnLast != null) btnLast.IsEnabled = _currentPage < _totalPages;
        }

        private void ShowLoading(bool show)
        {
            if (LoadingOverlay != null)
                LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

        private void FilterChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allMedicines != null) ApplyFilters();
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            if (txtSearch != null) txtSearch.Text = "";
            if (cbCategory != null) cbCategory.SelectedIndex = 0;
            if (cbStatus != null) cbStatus.SelectedIndex = 0;
        }

        private void ChkShowArchived_Changed(object sender, RoutedEventArgs e)
        {
            _showArchived = chkShowArchived.IsChecked == true;
            _ = LoadDataAsync();
        }

        private void BtnAddMedicine_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            var dialog = new AddEditMedicineDialog();
            if (dialog.ShowDialog() == true)
            {
                _ = LoadDataAsync();
                LogActivitySafe("Create", "Added new medicine");
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
                    LogActivitySafe("Update", $"Updated medicine (ID: {id})");
                }
            }
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int id)
            {
                var dialog = new ViewMedicineDialog(id);
                dialog.ShowDialog();
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            if (sender is Button btn && btn.Tag is int id)
            {
                var medicine = _allMedicines.FirstOrDefault(m => m.Id == id);
                if (medicine == null) return;

                string action = medicine.IsActive ? "archive" : "restore";
                var result = MessageBox.Show(medicine.IsActive ? $"Archive '{medicine.Name}'?" : $"Restore '{medicine.Name}'?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        ShowLoading(true);
                        string user = SessionManager.CurrentUser?.FullName ?? "System";
                        await Task.Run(() =>
                        {
                            if (medicine.IsActive) _medicineService.ArchiveMedicine(id, user);
                            else _medicineService.RestoreMedicine(id, user);
                        });
                        LogActivitySafe(action == "archive" ? "Archive" : "Restore", $"{action}: {medicine.Name}");
                        await LoadDataAsync();
                    }
                    catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
                    finally { ShowLoading(false); }
                }
            }
        }

        private void LogActivitySafe(string type, string desc)
        {
            try
            {
                if (SessionManager.CurrentUser != null && _activityLogService != null)
                {
                    _activityLogService.LogActivity(type, desc, "Medicine Inventory",
                        SessionManager.CurrentUser.FullName, SessionManager.CurrentUser.Role);
                }
            }
            catch { }
        }

        private void BtnFirst_Click(object sender, RoutedEventArgs e) { _currentPage = 1; ApplyPagination(); }
        private void BtnPrevious_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; ApplyPagination(); } }
        private void BtnNext_Click(object sender, RoutedEventArgs e) { if (_currentPage < _totalPages) { _currentPage++; ApplyPagination(); } }
        private void BtnLast_Click(object sender, RoutedEventArgs e) { _currentPage = _totalPages; ApplyPagination(); }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_exportService == null) { MessageBox.Show("Export unavailable. Install SkiaSharp.NativeAssets.Win32."); return; }
            try
            {
                var medicines = MapToModel(_filteredMedicines);
                string path = _exportService.ExportToExcel(medicines);
                if (MessageBox.Show("Export success. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Process.Start(path);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_exportService == null) { MessageBox.Show("Export unavailable. Install SkiaSharp.NativeAssets.Win32."); return; }
            try
            {
                var medicines = MapToModel(_filteredMedicines);
                string path = _exportService.ExportToPdf(medicines, SessionManager.CurrentUser?.FullName ?? "System");
                if (MessageBox.Show("Export success. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Process.Start(path);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnDownloadTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (_exportService == null) return;
            try
            {
                string path = _exportService.CreateImportTemplate();
                if (MessageBox.Show("Template created. Open file?", "Success", MessageBoxButton.YesNo) == MessageBoxResult.Yes) Process.Start(path);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Coming soon!");

        private List<Medicine> MapToModel(List<MedicineViewModel> vms)
        {
            if (vms == null) return new List<Medicine>();
            return vms.Select(vm => new Medicine
            {
                Id = vm.Id,
                MedicineId = vm.MedicineId,
                Name = vm.Name,
                GenericName = vm.GenericName,
                Brand = vm.Brand,
                Category = vm.Category,
                Type = vm.Type,
                Strength = vm.Strength,
                Stock = vm.Stock,
                MinimumStockLevel = vm.MinimumStockLevel,
                Price = vm.Price,
                ExpiryDate = vm.ExpiryDate,
                Status = vm.Status
            }).ToList();
        }
    }

    // 2. VIEW MODEL (Inside the same namespace)
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
        public int MinimumStockLevel { get; set; }
        public bool IsActive { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(Name)) return "?";
                var words = Name.Split(' ');
                if (words.Length >= 2) return $"{words[0][0]}{words[1][0]}".ToUpper();
                return Name.Length >= 2 ? Name.Substring(0, 2).ToUpper() : Name.ToUpper();
            }
        }

        public string PriceFormatted => $"₱{Price:N2}";
        public string ExpiryDateFormatted => ExpiryDate.ToString("MMM yyyy");
        public bool IsExpiring => ExpiryDate <= DateTime.Now.AddMonths(3) && ExpiryDate >= DateTime.Now;
        public bool IsExpired => ExpiryDate < DateTime.Now;
        public string StockStatus => Stock == 0 ? "Critical" : (Stock <= MinimumStockLevel ? "Low" : "Good");

        public event PropertyChangedEventHandler PropertyChanged;
    }

    // 3. CONVERTER (Inside the same namespace)
    public class BoolToArchiveStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive) return isActive ? "Archive" : "Restore";
            return "Archive";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
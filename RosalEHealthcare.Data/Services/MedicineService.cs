using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class MedicineService
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly NotificationService _notificationService;

        // Stock threshold for low stock alerts
        private const int LOW_STOCK_THRESHOLD = 20;
        // Days before expiry to trigger alert
        private const int EXPIRY_WARNING_DAYS = 30;

        public MedicineService(RosalEHealthcareDbContext db)
        {
            _db = db;
            _notificationService = new NotificationService(db);
        }

        #region Basic CRUD

        public IEnumerable<Medicine> GetAllMedicines()
        {
            return _db.Medicines
                .OrderBy(m => m.Name)
                .ToList();
        }

        public Medicine GetById(int id)
        {
            return _db.Medicines.Find(id);
        }

        public Medicine GetByMedicineId(string medicineId)
        {
            return _db.Medicines.FirstOrDefault(m => m.MedicineId == medicineId);
        }

        /// <summary>
        /// Add a new medicine and check for alerts
        /// </summary>
        public Medicine AddMedicine(Medicine medicine, string addedBy = null)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            if (string.IsNullOrEmpty(medicine.MedicineId))
            {
                medicine.MedicineId = GenerateMedicineId();
            }

            medicine.Status = DetermineStatus(medicine);

            _db.Medicines.Add(medicine);
            _db.SaveChanges();

            // Check and send alerts for new medicine
            try
            {
                CheckAndSendAlerts(medicine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }

            return medicine;
        }

        /// <summary>
        /// Update medicine and check for stock/expiry alerts
        /// </summary>
        public void UpdateMedicine(Medicine medicine, string updatedBy = null)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            // Get old values for comparison
            var oldMedicine = _db.Medicines.AsNoTracking().FirstOrDefault(m => m.Id == medicine.Id);
            int? oldStock = oldMedicine?.Stock;

            medicine.Status = DetermineStatus(medicine);

            var entry = _db.Entry(medicine);
            if (entry.State == EntityState.Detached)
                _db.Medicines.Attach(medicine);
            entry.State = EntityState.Modified;
            _db.SaveChanges();

            // Check and send alerts
            try
            {
                CheckAndSendAlerts(medicine, oldStock);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        public void DeleteMedicine(int id)
        {
            var medicine = GetById(id);
            if (medicine != null)
            {
                _db.Medicines.Remove(medicine);
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Update stock quantity with alert checking
        /// </summary>
        /// <param name="id">Medicine ID</param>
        /// <param name="newStock">New stock quantity</param>
        /// <param name="updatedBy">Name of user who updated</param>
        public void UpdateStock(int id, int newStock, string updatedBy = null)
        {
            var medicine = GetById(id);
            if (medicine == null) return;

            int oldStock = medicine.Stock;
            medicine.Stock = newStock;
            medicine.Status = DetermineStatus(medicine);
            _db.SaveChanges();

            // Check for stock alerts
            try
            {
                CheckStockAlerts(medicine, oldStock);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        #endregion

        #region Alert Checking

        /// <summary>
        /// Check and send appropriate alerts for a medicine
        /// </summary>
        private void CheckAndSendAlerts(Medicine medicine, int? oldStock = null)
        {
            CheckStockAlerts(medicine, oldStock);
            CheckExpiryAlerts(medicine);
        }

        /// <summary>
        /// Check for stock-related alerts
        /// </summary>
        private void CheckStockAlerts(Medicine medicine, int? oldStock = null)
        {
            // Out of stock alert
            if (medicine.Stock == 0)
            {
                // Only alert if stock just became 0
                if (!oldStock.HasValue || oldStock.Value > 0)
                {
                    _notificationService.NotifyOutOfStock(medicine.Name, medicine.MedicineId);
                }
            }
            // Low stock alert
            else if (medicine.Stock <= LOW_STOCK_THRESHOLD)
            {
                // Only alert if stock just dropped below threshold
                if (!oldStock.HasValue || oldStock.Value > LOW_STOCK_THRESHOLD)
                {
                    _notificationService.NotifyLowStock(medicine.Name, medicine.MedicineId, medicine.Stock, LOW_STOCK_THRESHOLD);
                }
            }
        }

        /// <summary>
        /// Check for expiry-related alerts
        /// </summary>
        private void CheckExpiryAlerts(Medicine medicine)
        {
            // Check if ExpiryDate is set (not default)
            if (medicine.ExpiryDate != default(DateTime) && medicine.ExpiryDate > DateTime.MinValue)
            {
                var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Today).Days;

                // Alert if expiring within warning period
                if (daysUntilExpiry >= 0 && daysUntilExpiry <= EXPIRY_WARNING_DAYS)
                {
                    _notificationService.NotifyExpiringMedicine(
                        medicine.Name,
                        medicine.MedicineId,
                        medicine.ExpiryDate
                    );
                }
            }
        }

        /// <summary>
        /// Run daily check for expiring medicines (call from scheduled task or on dashboard load)
        /// </summary>
        /// <summary>
        /// Run daily check for expiring medicines (call from scheduled task or on dashboard load)
        /// </summary>
        public void CheckAllExpiringMedicines()
        {
            try
            {
                var warningDate = DateTime.Today.AddDays(EXPIRY_WARNING_DAYS);
                var expiringMedicines = _db.Medicines
                    .Where(m => m.ExpiryDate >= DateTime.Today &&
                               m.ExpiryDate <= warningDate &&
                               m.Stock > 0)
                    .ToList();

                foreach (var medicine in expiringMedicines)
                {
                    // Check if we already sent a notification today for this medicine
                    var today = DateTime.Today;
                    var existingNotification = _db.Notifications
                        .Any(n => n.Type == "ExpiringMedicine" &&
                                 n.RelatedEntityId == medicine.MedicineId &&
                                 DbFunctions.TruncateTime(n.CreatedAt) == today);

                    if (!existingNotification)
                    {
                        _notificationService.NotifyExpiringMedicine(
                            medicine.Name,
                            medicine.MedicineId,
                            medicine.ExpiryDate
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking expiring medicines: {ex.Message}");
            }
        }

        /// <summary>
        /// Run daily check for low stock medicines (call from scheduled task or on dashboard load)
        /// </summary>
        public void CheckAllLowStockMedicines()
        {
            try
            {
                var lowStockMedicines = _db.Medicines
                    .Where(m => m.Stock > 0 && m.Stock <= LOW_STOCK_THRESHOLD)
                    .ToList();

                foreach (var medicine in lowStockMedicines)
                {
                    // Check if we already sent a notification today for this medicine
                    var today = DateTime.Today;
                    var existingNotification = _db.Notifications
                        .Any(n => n.Type == "LowStock" &&
                                 n.RelatedEntityId == medicine.MedicineId &&
                                 DbFunctions.TruncateTime(n.CreatedAt) == today);

                    if (!existingNotification)
                    {
                        _notificationService.NotifyLowStock(
                            medicine.Name,
                            medicine.MedicineId,
                            medicine.Stock,
                            LOW_STOCK_THRESHOLD
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking low stock medicines: {ex.Message}");
            }
        }

        #endregion

        #region Search & Filter

        public IEnumerable<Medicine> Search(string query, string category = null, string status = null)
        {
            var q = _db.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(m =>
                    (m.Name != null && m.Name.ToLower().Contains(query)) ||
                    (m.MedicineId != null && m.MedicineId.ToLower().Contains(query)) ||
                    (m.GenericName != null && m.GenericName.ToLower().Contains(query)) ||
                    (m.Brand != null && m.Brand.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
                q = q.Where(m => m.Category == category);

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
                q = q.Where(m => m.Status == status);

            return q.ToList().OrderBy(m => m.Name ?? "").ToList();
        }

        public IEnumerable<Medicine> SearchPaged(string query, string category, string status, int pageNumber, int pageSize)
        {
            var q = _db.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(m =>
                    (m.Name != null && m.Name.ToLower().Contains(query)) ||
                    (m.MedicineId != null && m.MedicineId.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
                q = q.Where(m => m.Category == category);

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
                q = q.Where(m => m.Status == status);

            var totalCount = q.Count();

            var results = q.Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();

            return results.OrderBy(m => m.Name ?? "").ToList();
        }

        public int GetFilteredCount(string query, string category, string status)
        {
            var q = _db.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(m =>
                    (m.Name != null && m.Name.ToLower().Contains(query)) ||
                    (m.MedicineId != null && m.MedicineId.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All Categories")
                q = q.Where(m => m.Category == category);

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
                q = q.Where(m => m.Status == status);

            return q.Count();
        }

        #endregion

        #region Statistics

        public int GetTotalMedicines()
        {
            return _db.Medicines.Count();
        }

        public int GetLowStockCount()
        {
            return _db.Medicines.Count(m => m.Stock > 0 && m.Stock <= LOW_STOCK_THRESHOLD);
        }

        public int GetExpiringSoonCount()
        {
            var warningDate = DateTime.Now.AddDays(EXPIRY_WARNING_DAYS);
            return _db.Medicines.Count(m => m.ExpiryDate <= warningDate && m.ExpiryDate >= DateTime.Now);
        }

        public int GetOutOfStockCount()
        {
            return _db.Medicines.Count(m => m.Stock == 0);
        }

        public Dictionary<string, int> GetMedicinesByCategory()
        {
            return _db.Medicines
                .GroupBy(m => m.Category ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public IEnumerable<Medicine> GetLowStockMedicines()
        {
            return _db.Medicines
                .Where(m => m.Stock > 0 && m.Stock <= LOW_STOCK_THRESHOLD)
                .OrderBy(m => m.Stock)
                .ToList();
        }

        public IEnumerable<Medicine> GetExpiringSoonMedicines()
        {
            var warningDate = DateTime.Now.AddDays(EXPIRY_WARNING_DAYS);
            return _db.Medicines
                .Where(m => m.ExpiryDate <= warningDate && m.ExpiryDate >= DateTime.Now)
                .OrderBy(m => m.ExpiryDate)
                .ToList();
        }

        public IEnumerable<Medicine> GetOutOfStockMedicines()
        {
            return _db.Medicines
                .Where(m => m.Stock == 0)
                .OrderBy(m => m.Name)
                .ToList();
        }

        public IEnumerable<string> GetAllCategories()
        {
            return _db.Medicines
                .Where(m => !string.IsNullOrEmpty(m.Category))
                .Select(m => m.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }

        #endregion

        #region Helper Methods

        private string DetermineStatus(Medicine medicine)
        {
            if (medicine.Stock == 0)
                return "Out of Stock";

            if (medicine.Stock <= LOW_STOCK_THRESHOLD)
                return "Low Stock";

            // Check if ExpiryDate is set (not default)
            if (medicine.ExpiryDate != default(DateTime) && medicine.ExpiryDate > DateTime.MinValue)
            {
                var warningDate = DateTime.Now.AddDays(EXPIRY_WARNING_DAYS);
                if (medicine.ExpiryDate <= warningDate && medicine.ExpiryDate >= DateTime.Now)
                    return "Expiring Soon";

                if (medicine.ExpiryDate < DateTime.Now)
                    return "Expired";
            }

            return "Available";
        }

        private string GenerateMedicineId()
        {
            var lastMedicine = _db.Medicines
                .OrderByDescending(m => m.Id)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastMedicine != null && !string.IsNullOrEmpty(lastMedicine.MedicineId))
            {
                var parts = lastMedicine.MedicineId.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
                else
                {
                    nextNumber = (lastMedicine.Id) + 1;
                }
            }

            return string.Format("MED-{0:D3}", nextNumber);
        }

        #endregion
    }
}
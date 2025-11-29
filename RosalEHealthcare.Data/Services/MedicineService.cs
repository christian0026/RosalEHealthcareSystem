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

        private const int LOW_STOCK_THRESHOLD = 20;
        private const int EXPIRY_WARNING_DAYS = 90; // 3 months

        public MedicineService(RosalEHealthcareDbContext db)
        {
            _db = db;
            _notificationService = new NotificationService(db);
        }

        #region Basic CRUD

        /// <summary>
        /// Get all active medicines
        /// </summary>
        public IEnumerable<Medicine> GetAllMedicines(bool includeArchived = false)
        {
            var query = _db.Medicines.AsQueryable();

            if (!includeArchived)
                query = query.Where(m => m.IsActive);

            return query.OrderBy(m => m.Name).ToList();
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
        /// Add new medicine with audit tracking
        /// </summary>
        public Medicine AddMedicine(Medicine medicine, string addedBy = null)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            if (string.IsNullOrEmpty(medicine.MedicineId))
                medicine.MedicineId = GenerateMedicineId();

            medicine.Status = DetermineStatus(medicine);
            medicine.IsActive = true;
            medicine.LastModifiedBy = addedBy;
            medicine.LastModifiedAt = DateTime.Now;

            _db.Medicines.Add(medicine);
            _db.SaveChanges();

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
        /// Update medicine with audit tracking
        /// </summary>
        public void UpdateMedicine(Medicine medicine, string updatedBy = null)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            var oldMedicine = _db.Medicines.AsNoTracking().FirstOrDefault(m => m.Id == medicine.Id);
            int? oldStock = oldMedicine?.Stock;

            medicine.Status = DetermineStatus(medicine);
            medicine.LastModifiedBy = updatedBy;
            medicine.LastModifiedAt = DateTime.Now;

            var entry = _db.Entry(medicine);
            if (entry.State == EntityState.Detached)
                _db.Medicines.Attach(medicine);

            entry.State = EntityState.Modified;
            _db.SaveChanges();

            try
            {
                CheckAndSendAlerts(medicine, oldStock);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Archive medicine (soft delete)
        /// </summary>
        public void ArchiveMedicine(int id, string archivedBy = null)
        {
            var medicine = GetById(id);
            if (medicine != null)
            {
                medicine.IsActive = false;
                medicine.LastModifiedBy = archivedBy;
                medicine.LastModifiedAt = DateTime.Now;
                medicine.Notes = $"Archived by {archivedBy} on {DateTime.Now:yyyy-MM-dd HH:mm}";
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Restore archived medicine
        /// </summary>
        public void RestoreMedicine(int id, string restoredBy = null)
        {
            var medicine = GetById(id);
            if (medicine != null)
            {
                medicine.IsActive = true;
                medicine.LastModifiedBy = restoredBy;
                medicine.LastModifiedAt = DateTime.Now;
                medicine.Notes = $"Restored by {restoredBy} on {DateTime.Now:yyyy-MM-dd HH:mm}";
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Delete medicine permanently (use with caution)
        /// </summary>
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
        /// Update stock with audit tracking
        /// </summary>
        public void UpdateStock(int id, int newStock, string updatedBy = null)
        {
            var medicine = GetById(id);
            if (medicine == null) return;

            int oldStock = medicine.Stock;
            medicine.Stock = newStock;
            medicine.Status = DetermineStatus(medicine);
            medicine.LastModifiedBy = updatedBy;
            medicine.LastModifiedAt = DateTime.Now;

            _db.SaveChanges();

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

        private void CheckAndSendAlerts(Medicine medicine, int? oldStock = null)
        {
            CheckStockAlerts(medicine, oldStock);
            CheckExpiryAlerts(medicine);
        }

        private void CheckStockAlerts(Medicine medicine, int? oldStock = null)
        {
            if (medicine.Stock == 0)
            {
                if (!oldStock.HasValue || oldStock.Value > 0)
                {
                    _notificationService.NotifyOutOfStock(medicine.Name, medicine.MedicineId);
                }
            }
            else if (medicine.Stock <= medicine.MinimumStockLevel)
            {
                if (!oldStock.HasValue || oldStock.Value > medicine.MinimumStockLevel)
                {
                    _notificationService.NotifyLowStock(medicine.Name, medicine.MedicineId, medicine.Stock, medicine.MinimumStockLevel);
                }
            }
        }

        private void CheckExpiryAlerts(Medicine medicine)
        {
            if (medicine.ExpiryDate != default(DateTime) && medicine.ExpiryDate > DateTime.MinValue)
            {
                var daysUntilExpiry = (medicine.ExpiryDate - DateTime.Today).Days;

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

        public void CheckAllExpiringMedicines()
        {
            try
            {
                var warningDate = DateTime.Today.AddDays(EXPIRY_WARNING_DAYS);
                var expiringMedicines = _db.Medicines
                    .Where(m => m.IsActive &&
                               m.ExpiryDate >= DateTime.Today &&
                               m.ExpiryDate <= warningDate &&
                               m.Stock > 0)
                    .ToList();

                foreach (var medicine in expiringMedicines)
                {
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

        public void CheckAllLowStockMedicines()
        {
            try
            {
                var lowStockMedicines = _db.Medicines
                    .Where(m => m.IsActive && m.Stock > 0 && m.Stock <= m.MinimumStockLevel)
                    .ToList();

                foreach (var medicine in lowStockMedicines)
                {
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
                            medicine.MinimumStockLevel
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

        public IEnumerable<Medicine> Search(string query, string category = null, string status = null, bool includeArchived = false)
        {
            var q = _db.Medicines.AsQueryable();

            if (!includeArchived)
                q = q.Where(m => m.IsActive);

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

            return q.OrderBy(m => m.Name ?? "").ToList();
        }

        public int GetFilteredCount(string query, string category, string status, bool includeArchived = false)
        {
            var q = _db.Medicines.AsQueryable();

            if (!includeArchived)
                q = q.Where(m => m.IsActive);

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

        public int GetTotalMedicines(bool includeArchived = false)
        {
            if (includeArchived)
                return _db.Medicines.Count();

            return _db.Medicines.Count(m => m.IsActive);
        }

        public int GetLowStockCount()
        {
            return _db.Medicines.Count(m => m.IsActive && m.Stock > 0 && m.Stock <= m.MinimumStockLevel);
        }

        public int GetExpiringSoonCount()
        {
            var warningDate = DateTime.Now.AddDays(EXPIRY_WARNING_DAYS);
            return _db.Medicines.Count(m => m.IsActive && m.ExpiryDate <= warningDate && m.ExpiryDate >= DateTime.Now);
        }

        public int GetOutOfStockCount()
        {
            return _db.Medicines.Count(m => m.IsActive && m.Stock == 0);
        }

        public Dictionary<string, int> GetMedicinesByCategory()
        {
            return _db.Medicines
                .Where(m => m.IsActive)
                .GroupBy(m => m.Category ?? "Unknown")
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public IEnumerable<Medicine> GetLowStockMedicines()
        {
            return _db.Medicines
                .Where(m => m.IsActive && m.Stock > 0 && m.Stock <= m.MinimumStockLevel)
                .OrderBy(m => m.Stock)
                .ToList();
        }

        public IEnumerable<Medicine> GetExpiringSoonMedicines()
        {
            var warningDate = DateTime.Now.AddDays(EXPIRY_WARNING_DAYS);
            return _db.Medicines
                .Where(m => m.IsActive && m.ExpiryDate <= warningDate && m.ExpiryDate >= DateTime.Now)
                .OrderBy(m => m.ExpiryDate)
                .ToList();
        }

        public IEnumerable<Medicine> GetOutOfStockMedicines()
        {
            return _db.Medicines
                .Where(m => m.IsActive && m.Stock == 0)
                .OrderBy(m => m.Name)
                .ToList();
        }

        public IEnumerable<string> GetAllCategories()
        {
            return _db.Medicines
                .Where(m => m.IsActive && !string.IsNullOrEmpty(m.Category))
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

            if (medicine.Stock <= medicine.MinimumStockLevel)
                return "Low Stock";

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
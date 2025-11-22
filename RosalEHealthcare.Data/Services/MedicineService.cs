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

        public MedicineService(RosalEHealthcareDbContext db)
        {
            _db = db;
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

        public Medicine AddMedicine(Medicine medicine)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            // Auto-generate MedicineId if not provided
            if (string.IsNullOrEmpty(medicine.MedicineId))
            {
                medicine.MedicineId = GenerateMedicineId();
            }

            medicine.Status = DetermineStatus(medicine);

            _db.Medicines.Add(medicine);
            _db.SaveChanges();
            return medicine;
        }

        public void UpdateMedicine(Medicine medicine)
        {
            if (medicine == null) throw new ArgumentNullException(nameof(medicine));

            medicine.Status = DetermineStatus(medicine);

            var entry = _db.Entry(medicine);
            if (entry.State == EntityState.Detached)
                _db.Medicines.Attach(medicine);
            entry.State = EntityState.Modified;
            _db.SaveChanges();
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

            return q.OrderBy(m => m.Name).ToList();
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

            return q.OrderBy(m => m.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
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
            return _db.Medicines.Count(m => m.Stock > 0 && m.Stock <= 20);
        }

        public int GetExpiringSoonCount()
        {
            var threeMonthsFromNow = DateTime.Now.AddMonths(3);
            return _db.Medicines.Count(m => m.ExpiryDate <= threeMonthsFromNow && m.ExpiryDate >= DateTime.Now);
        }

        public int GetOutOfStockCount()
        {
            return _db.Medicines.Count(m => m.Stock == 0);
        }

        public Dictionary<string, int> GetMedicinesByCategory()
        {
            return _db.Medicines
                .GroupBy(m => m.Category)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Count());
        }

        public IEnumerable<Medicine> GetLowStockMedicines()
        {
            return _db.Medicines
                .Where(m => m.Stock > 0 && m.Stock <= 20)
                .OrderBy(m => m.Stock)
                .ToList();
        }

        public IEnumerable<Medicine> GetExpiringSoonMedicines()
        {
            var threeMonthsFromNow = DateTime.Now.AddMonths(3);
            return _db.Medicines
                .Where(m => m.ExpiryDate <= threeMonthsFromNow && m.ExpiryDate >= DateTime.Now)
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

            if (medicine.Stock <= 20)
                return "Low Stock";

            var threeMonthsFromNow = DateTime.Now.AddMonths(3);
            if (medicine.ExpiryDate <= threeMonthsFromNow && medicine.ExpiryDate >= DateTime.Now)
                return "Expiring Soon";

            if (medicine.ExpiryDate < DateTime.Now)
                return "Expired";

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
                    nextNumber = (lastMedicine?.Id ?? 0) + 1;
                }
            }

            return string.Format("MED-{0:D3}", nextNumber);
        }

        #endregion
    }
}
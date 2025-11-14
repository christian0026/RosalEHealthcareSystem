using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System.Collections.Generic;
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

        public IEnumerable<Medicine> GetAllMedicines()
        {
            return _db.Medicines.ToList();
        }

        public Medicine GetById(int id)
        {
            return _db.Medicines.Find(id);
        }

        public void AddMedicine(Medicine medicine)
        {
            _db.Medicines.Add(medicine);
            _db.SaveChanges();
        }
    }
}
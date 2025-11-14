using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class PrescriptionService
    {
        private readonly RosalEHealthcareDbContext _db;

        public PrescriptionService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Prescription> GetAllPrescriptions()
        {
            return _db.Prescriptions.ToList();
        }

        public IEnumerable<Prescription> GetPrescriptionsByPatientId(int patientId)
        {
            return _db.Prescriptions
                      .Where(p => p.PatientId == patientId)
                      .ToList();
        }

        public Prescription GetById(int id)
        {
            return _db.Prescriptions.Find(id);
        }

        public Prescription SavePrescription(Prescription p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            p.CreatedAt = DateTime.UtcNow;

            if (string.IsNullOrEmpty(p.PrescriptionId))
            {
                p.PrescriptionId = GeneratePrescriptionId();
            }

            // Save prescription first
            var meds = p.Medicines != null ? p.Medicines.ToList() : new List<PrescriptionMedicine>();
            p.Medicines = new List<PrescriptionMedicine>(); // Clear to avoid EF tracking issues

            _db.Prescriptions.Add(p);
            _db.SaveChanges();

            // Now save medicines with the prescription ID
            foreach (var m in meds)
            {
                m.PrescriptionId = p.Id;
                _db.PrescriptionMedicines.Add(m);
            }
            _db.SaveChanges();

            return p;
        }

        private string GeneratePrescriptionId()
        {
            // Simple sequential generator: PR-YYYY-<4 digit>
            var year = DateTime.UtcNow.Year;
            int next = 1;

            var last = _db.Prescriptions
                          .OrderByDescending(x => x.Id)
                          .FirstOrDefault();

            if (last != null)
            {
                // Parse last.PrescriptionId if format matches
                var parts = (last.PrescriptionId ?? "").Split('-');

                // Changed from parts[^1] to parts[parts.Length - 1] for C# 7.3 compatibility
                if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastnum))
                {
                    next = lastnum + 1;
                }
                else
                {
                    next = last.Id + 1;
                }
            }

            return string.Format("PR-{0}-{1:D4}", year, next);
        }
    }
}
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
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

        #region CRUD Operations

        public IEnumerable<Prescription> GetAllPrescriptions()
        {
            return _db.Prescriptions
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        public IEnumerable<Prescription> GetPrescriptionsByPatientId(int patientId)
        {
            return _db.Prescriptions
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Add a new prescription and notify receptionist
        /// </summary>
        /// <param name="prescription">Prescription to add</param>
        /// <param name="doctorName">Name of doctor who created the prescription</param>
        public Prescription AddPrescription(Prescription prescription, string doctorName = null)
        {
            if (prescription == null) throw new ArgumentNullException(nameof(prescription));

            prescription.CreatedAt = DateTime.Now;

            // Generate prescription ID if not set
            if (string.IsNullOrEmpty(prescription.PrescriptionId))
            {
                prescription.PrescriptionId = GeneratePrescriptionId();
            }

            _db.Prescriptions.Add(prescription);
            _db.SaveChanges();

            // Notify receptionist that prescription is ready
            try
            {
                if (!string.IsNullOrEmpty(doctorName))
                {
                    _notificationService.NotifyPrescriptionReady(
                        prescription.PatientName,
                        prescription.PrescriptionId,
                        doctorName
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }

            return prescription;
        }

        public void UpdatePrescription(Prescription prescription)
        {
            if (prescription == null) throw new ArgumentNullException(nameof(prescription));

            var existing = _db.Prescriptions.Find(prescription.Id);
            if (existing == null) return;

            // Update properties
            existing.PatientName = prescription.PatientName;
            existing.Diagnosis = prescription.Diagnosis;
            existing.Medications = prescription.Medications;
            existing.Instructions = prescription.Instructions;
            existing.Notes = prescription.Notes;

            _db.SaveChanges();
        }

        public void DeletePrescription(int id)
        {
            var prescription = GetById(id);
            if (prescription != null)
            {
                _db.Prescriptions.Remove(prescription);
                _db.SaveChanges();
            }
        }

        public Prescription GetById(int id)
        {
            return _db.Prescriptions
                .Include(p => p.Medicines)
                .FirstOrDefault(p => p.Id == id);
        }

        public Prescription GetByPrescriptionId(string prescriptionId)
        {
            return _db.Prescriptions
                .Include(p => p.Medicines)
                .FirstOrDefault(p => p.PrescriptionId == prescriptionId);
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

        public void UpdatePrescription(Prescription p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            var entry = _db.Entry(p);
            if (entry.State == EntityState.Detached)
                _db.Prescriptions.Attach(p);
            entry.State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void DeletePrescription(int id)
        {
            var prescription = GetById(id);
            if (prescription != null)
            {
                // Delete medicines first
                var medicines = _db.PrescriptionMedicines.Where(m => m.PrescriptionId == id).ToList();
                _db.PrescriptionMedicines.RemoveRange(medicines);

                // Delete prescription
                _db.Prescriptions.Remove(prescription);
                _db.SaveChanges();
            }
        }

        #endregion

        #region Statistics

        public int GetTotalPrescriptions()
        {
            return _db.Prescriptions.Count();
        }

        public int GetPrescriptionsToday()
        {
            var today = DateTime.Today;
            return _db.Prescriptions.Count(p => DbFunctions.TruncateTime(p.CreatedAt) == today);
        }

        public int GetPrescriptionsThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.Prescriptions.Count(p => p.CreatedAt >= startOfMonth);
        }

        public IEnumerable<Prescription> GetRecentPrescriptions(int count = 10)
        {
            return _db.Prescriptions
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Notifications


        #region Helper Methods

        private string GeneratePrescriptionId()
        {
            var year = DateTime.UtcNow.Year;
            int next = 1;

            var last = _db.Prescriptions
                .OrderByDescending(x => x.Id)
                .FirstOrDefault();

            if (last != null)
            {
                var parts = (last.PrescriptionId ?? "").Split('-');
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

        #endregion
    }
}
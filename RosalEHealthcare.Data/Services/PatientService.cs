using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class PatientService
    {
        private readonly RosalEHealthcareDbContext _db;

        public PatientService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Patient> GetAll()
        {
            return _db.Patients.OrderByDescending(p => p.LastVisit).ToList();
        }

        public Patient GetById(int id)
        {
            return _db.Patients.Find(id);
        }

        public Patient GetByPatientId(string patientId)
        {
            return _db.Patients.FirstOrDefault(p => p.PatientId == patientId);
        }

        public Patient AddPatient(Patient p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            _db.Patients.Add(p);
            _db.SaveChanges();
            return p;
        }

        public void UpdatePatient(Patient p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var entry = _db.Entry(p);
            if (entry.State == System.Data.Entity.EntityState.Detached)
                _db.Patients.Attach(p);
            entry.State = System.Data.Entity.EntityState.Modified;
            _db.SaveChanges();
        }

        public void ArchivePatient(int id)
        {
            var p = GetById(id);
            if (p == null) return;
            p.Status = "Archived";
            UpdatePatient(p);
        }

        public IEnumerable<Patient> Search(string query, string status = null)
        {
            var q = _db.Patients.AsQueryable();
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(x =>
                    (x.FullName != null && x.FullName.ToLower().Contains(query))
                    || (x.PatientId != null && x.PatientId.ToLower().Contains(query))
                    || (x.Contact != null && x.Contact.ToLower().Contains(query))
                    || (x.PrimaryDiagnosis != null && x.PrimaryDiagnosis.ToLower().Contains(query))
                );
            }

            return q.OrderByDescending(p => p.LastVisit).ToList();
        }
    }
}

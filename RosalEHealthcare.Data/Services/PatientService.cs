using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class PatientService
    {
        private readonly RosalEHealthcareDbContext _db;
        private readonly NotificationService _notificationService;

        public PatientService(RosalEHealthcareDbContext db)
        {
            _db = db;
            _notificationService = new NotificationService(db);
        }

        #region Patient CRUD

        public IEnumerable<Patient> GetAll()
        {
            return _db.Patients
                .Where(p => p.Status != "Archived")
                .OrderByDescending(p => p.LastVisit)
                .ToList();
        }

        public Patient GetById(int id)
        {
            return _db.Patients.Find(id);
        }

        public Patient GetByPatientId(string patientId)
        {
            return _db.Patients.FirstOrDefault(p => p.PatientId == patientId);
        }

        /// <summary>
        /// Add a new patient and notify doctors
        /// </summary>
        /// <param name="p">Patient to add</param>
        /// <param name="registeredBy">Name of user who registered the patient</param>
        /// <returns>Added patient</returns>
        public Patient AddPatient(Patient p, string registeredBy = null)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            // Auto-generate PatientId if not provided
            if (string.IsNullOrEmpty(p.PatientId))
            {
                p.PatientId = GeneratePatientId();
            }

            p.DateCreated = DateTime.Now;
            p.Status = "Active";

            _db.Patients.Add(p);
            _db.SaveChanges();

            // Send notification to doctors
            try
            {
                if (!string.IsNullOrEmpty(registeredBy))
                {
                    _notificationService.NotifyNewPatient(p.FullName, p.PatientId, registeredBy);
                }
            }
            catch (Exception ex)
            {
                // Log but don't fail the operation
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }

            return p;
        }

        /// <summary>
        /// Update patient information and optionally notify
        /// </summary>
        /// <param name="p">Patient to update</param>
        /// <param name="updatedBy">Name of user who updated the patient</param>
        /// <param name="sendNotification">Whether to send notification</param>
        public void UpdatePatient(Patient p, string updatedBy = null, bool sendNotification = false)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            var entry = _db.Entry(p);
            if (entry.State == EntityState.Detached)
                _db.Patients.Attach(p);
            entry.State = EntityState.Modified;
            _db.SaveChanges();

            // Send notification if requested
            try
            {
                if (sendNotification && !string.IsNullOrEmpty(updatedBy))
                {
                    _notificationService.NotifyPatientUpdated(p.FullName, p.PatientId, updatedBy);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        // Overload for backward compatibility
        public void UpdatePatient(Patient p)
        {
            UpdatePatient(p, null, false);
        }

        public void ArchivePatient(int id)
        {
            var p = GetById(id);
            if (p == null) return;
            p.Status = "Archived";
            UpdatePatient(p);
        }

        public void DeletePatient(int id)
        {
            var p = GetById(id);
            if (p != null)
            {
                _db.Patients.Remove(p);
                _db.SaveChanges();
            }
        }

        #endregion

        #region Search & Filter

        public IEnumerable<Patient> SearchPaged(string query, string status, string gender, int pageNumber, int pageSize)
        {
            var q = _db.Patients.Where(p => p.Status != "Archived");

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(x => x.FullName.ToLower().Contains(query) || x.PatientId.ToLower().Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
                q = q.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(gender) && gender != "All Gender")
                q = q.Where(x => x.Gender == gender);

            return q.OrderByDescending(p => p.DateCreated)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetFilteredCount(string query, string status, string gender)
        {
            var q = _db.Patients.Where(p => p.Status != "Archived");

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(x => x.FullName.ToLower().Contains(query) || x.PatientId.ToLower().Contains(query));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
                q = q.Where(x => x.Status == status);

            if (!string.IsNullOrWhiteSpace(gender) && gender != "All Gender")
                q = q.Where(x => x.Gender == gender);

            return q.Count();
        }

        public int GetPatientsWaitingToday()
        {
            var today = DateTime.Today;
            return _db.Appointments.Count(a =>
                DbFunctions.TruncateTime(a.Time) == today &&
                (a.Status == "PENDING" || a.Status == "CONFIRMED"));
        }

        public IEnumerable<Patient> Search(string query, string status = null)
        {
            var q = _db.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(x => x.Status == status);
            else
                q = q.Where(x => x.Status != "Archived");

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(x =>
                    (x.FullName != null && x.FullName.ToLower().Contains(query)) ||
                    (x.PatientId != null && x.PatientId.ToLower().Contains(query)) ||
                    (x.Contact != null && x.Contact.ToLower().Contains(query)) ||
                    (x.Email != null && x.Email.ToLower().Contains(query)) ||
                    (x.PrimaryDiagnosis != null && x.PrimaryDiagnosis.ToLower().Contains(query))
                );
            }

            return q.OrderByDescending(x => x.LastVisit).ToList();
        }

        public IEnumerable<Patient> FilterByDateRange(DateTime startDate, DateTime endDate)
        {
            return _db.Patients
                .Where(p => p.DateCreated >= startDate && p.DateCreated <= endDate && p.Status != "Archived")
                .OrderByDescending(p => p.DateCreated)
                .ToList();
        }

        public IEnumerable<Patient> FilterByDiagnosis(string diagnosis)
        {
            return _db.Patients
                .Where(p => p.PrimaryDiagnosis == diagnosis && p.Status != "Archived")
                .OrderByDescending(p => p.LastVisit)
                .ToList();
        }

        public IEnumerable<Patient> FilterByGender(string gender)
        {
            return _db.Patients
                .Where(p => p.Gender == gender && p.Status != "Archived")
                .OrderByDescending(p => p.LastVisit)
                .ToList();
        }

        public IEnumerable<Patient> FilterByBloodType(string bloodType)
        {
            return _db.Patients
                .Where(p => p.BloodType == bloodType && p.Status != "Archived")
                .OrderByDescending(p => p.LastVisit)
                .ToList();
        }

        #endregion

        #region Medical History

        public IEnumerable<MedicalHistory> GetMedicalHistory(int patientId)
        {
            return _db.MedicalHistories
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.VisitDate)
                .ToList();
        }

        public MedicalHistory AddMedicalHistory(MedicalHistory history)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            history.CreatedAt = DateTime.Now;
            _db.MedicalHistories.Add(history);
            _db.SaveChanges();

            // Update patient's last visit
            var patient = GetById(history.PatientId);
            if (patient != null)
            {
                patient.LastVisit = history.VisitDate;
                UpdatePatient(patient);
            }

            return history;
        }

        public void UpdateMedicalHistory(MedicalHistory history)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));
            var entry = _db.Entry(history);
            if (entry.State == EntityState.Detached)
                _db.MedicalHistories.Attach(history);
            entry.State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void DeleteMedicalHistory(int id)
        {
            var history = _db.MedicalHistories.Find(id);
            if (history != null)
            {
                _db.MedicalHistories.Remove(history);
                _db.SaveChanges();
            }
        }

        #endregion

        #region Medical Documents

        public IEnumerable<MedicalDocument> GetMedicalDocuments(int patientId)
        {
            return _db.MedicalDocuments
                .Where(m => m.PatientId == patientId && m.Status == "Active")
                .OrderByDescending(m => m.UploadedDate)
                .ToList();
        }

        public IEnumerable<MedicalDocument> GetMedicalDocumentsByCategory(int patientId, string category)
        {
            return _db.MedicalDocuments
                .Where(m => m.PatientId == patientId && m.Category == category && m.Status == "Active")
                .OrderByDescending(m => m.UploadedDate)
                .ToList();
        }

        public MedicalDocument AddMedicalDocument(MedicalDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            document.UploadedDate = DateTime.Now;
            document.Status = "Active";
            _db.MedicalDocuments.Add(document);
            _db.SaveChanges();
            return document;
        }

        public void DeleteMedicalDocument(int id)
        {
            var doc = _db.MedicalDocuments.Find(id);
            if (doc != null)
            {
                doc.Status = "Archived";
                _db.SaveChanges();
            }
        }

        #endregion

        #region Prescriptions

        public IEnumerable<Prescription> GetPatientPrescriptions(int patientId)
        {
            var patient = GetById(patientId);
            if (patient == null) return new List<Prescription>();

            return _db.Prescriptions
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();
        }

        #endregion

        #region Statistics

        public int GetTotalPatients()
        {
            return _db.Patients.Count(p => p.Status != "Archived");
        }

        public int GetNewPatientsThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.Patients.Count(p => p.DateCreated >= startOfMonth && p.Status != "Archived");
        }

        public Dictionary<string, int> GetPatientsByGender()
        {
            return _db.Patients
                .Where(p => p.Status != "Archived")
                .GroupBy(p => p.Gender)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Count());
        }

        public Dictionary<string, int> GetPatientsByDiagnosis()
        {
            return _db.Patients
                .Where(p => p.Status != "Archived" && !string.IsNullOrEmpty(p.PrimaryDiagnosis))
                .GroupBy(p => p.PrimaryDiagnosis)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        #endregion

        #region Helper Methods

        private string GeneratePatientId()
        {
            var year = DateTime.Now.Year;
            var yearStr = year.ToString();

            var lastPatient = _db.Patients
                .ToList()
                .Where(p => p.PatientId != null && p.PatientId.StartsWith("PT-" + yearStr))
                .OrderByDescending(p => p.PatientId)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastPatient != null && !string.IsNullOrEmpty(lastPatient.PatientId))
            {
                var parts = lastPatient.PatientId.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return string.Format("PT-{0}-{1:D3}", year, nextNumber);
        }

        #endregion
    }
}
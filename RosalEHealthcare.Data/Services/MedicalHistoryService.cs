using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class MedicalHistoryService
    {
        private readonly RosalEHealthcareDbContext _db;

        public MedicalHistoryService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region CRUD Operations

        public IEnumerable<MedicalHistory> GetAll()
        {
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .OrderByDescending(m => m.VisitDate)
                .ToList();
        }

        public MedicalHistory GetById(int id)
        {
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .FirstOrDefault(m => m.Id == id);
        }

        public IEnumerable<MedicalHistory> GetByPatientId(int patientId)
        {
            return _db.MedicalHistories
                .Where(m => m.PatientId == patientId)
                .OrderByDescending(m => m.VisitDate)
                .ToList();
        }

        public MedicalHistory Add(MedicalHistory history)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));

            history.CreatedAt = DateTime.Now;
            _db.MedicalHistories.Add(history);
            _db.SaveChanges();

            // Update patient's last visit
            var patient = _db.Patients.Find(history.PatientId);
            if (patient != null)
            {
                patient.LastVisit = history.VisitDate;
                _db.SaveChanges();
            }

            return history;
        }

        public void Update(MedicalHistory history)
        {
            if (history == null) throw new ArgumentNullException(nameof(history));

            var entry = _db.Entry(history);
            if (entry.State == EntityState.Detached)
                _db.MedicalHistories.Attach(history);
            entry.State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void Delete(int id)
        {
            var history = _db.MedicalHistories.Find(id);
            if (history != null)
            {
                _db.MedicalHistories.Remove(history);
                _db.SaveChanges();
            }
        }

        #endregion

        #region Search & Filter

        public IEnumerable<MedicalHistory> Search(string query, DateTime? startDate = null, DateTime? endDate = null)
        {
            var q = _db.MedicalHistories.Include(m => m.Patient).AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(m =>
                    (m.Diagnosis != null && m.Diagnosis.ToLower().Contains(query)) ||
                    (m.Treatment != null && m.Treatment.ToLower().Contains(query)) ||
                    (m.DoctorName != null && m.DoctorName.ToLower().Contains(query)) ||
                    (m.Patient != null && m.Patient.FullName != null && m.Patient.FullName.ToLower().Contains(query))
                );
            }

            if (startDate.HasValue)
            {
                q = q.Where(m => m.VisitDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                q = q.Where(m => m.VisitDate <= endDate.Value);
            }

            return q.OrderByDescending(m => m.VisitDate).ToList();
        }

        public IEnumerable<MedicalHistory> GetRecentConsultations(int count = 10)
        {
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .OrderByDescending(m => m.VisitDate)
                .Take(count)
                .ToList();
        }

        public IEnumerable<MedicalHistory> GetRecentConsultationsPaged(int pageNumber, int pageSize)
        {
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .OrderByDescending(m => m.VisitDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetTotalConsultationsCount()
        {
            return _db.MedicalHistories.Count();
        }

        #endregion

        #region Statistics for Dashboard

        public int GetTotalConsultations()
        {
            return _db.MedicalHistories.Count();
        }

        public int GetConsultationsToday()
        {
            var today = DateTime.Today;
            return _db.MedicalHistories.Count(m => DbFunctions.TruncateTime(m.VisitDate) == today);
        }

        public int GetConsultationsThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.MedicalHistories.Count(m => m.VisitDate >= startOfMonth);
        }

        public int GetConsultationsLastMonth()
        {
            var startOfLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            var endOfLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            return _db.MedicalHistories.Count(m => m.VisitDate >= startOfLastMonth && m.VisitDate <= endOfLastMonth);
        }

        public int GetFollowUpsRequiredCount()
        {
            var nextWeek = DateTime.Now.AddDays(7);
            return _db.MedicalHistories.Count(m =>
                m.FollowUpRequired &&
                m.NextFollowUpDate.HasValue &&
                m.NextFollowUpDate.Value <= nextWeek &&
                m.NextFollowUpDate.Value >= DateTime.Today);
        }

        public int GetFollowUpsRequiredInDays(int days)
        {
            var futureDate = DateTime.Now.AddDays(days);
            return _db.MedicalHistories.Count(m =>
                m.FollowUpRequired &&
                m.NextFollowUpDate.HasValue &&
                m.NextFollowUpDate.Value <= futureDate &&
                m.NextFollowUpDate.Value >= DateTime.Today);
        }

        /// <summary>
        /// Get monthly diagnosis trends (Common Illness chart data)
        /// </summary>
        public Dictionary<string, int> GetMonthlyDiagnosisTrends(int monthsBack = 6)
        {
            var startDate = DateTime.Now.AddMonths(-monthsBack);

            return _db.MedicalHistories
                .Where(m => m.VisitDate >= startDate && !string.IsNullOrEmpty(m.Diagnosis))
                .GroupBy(m => m.Diagnosis)
                .Select(g => new { Diagnosis = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionary(x => x.Diagnosis, x => x.Count);
        }

        /// <summary>
        /// Get weekly patient visit counts for trend analysis
        /// </summary>
        public Dictionary<string, int> GetWeeklyVisitTrends(int weeksBack = 8)
        {
            var startDate = DateTime.Now.AddDays(-weeksBack * 7);
            var results = new Dictionary<string, int>();

            var visits = _db.MedicalHistories
                .Where(m => m.VisitDate >= startDate)
                .ToList();

            // Group by week
            for (int i = weeksBack - 1; i >= 0; i--)
            {
                var weekStart = DateTime.Now.AddDays(-i * 7 - (int)DateTime.Now.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);
                var weekLabel = weekStart.ToString("MMM dd");
                var count = visits.Count(v => v.VisitDate >= weekStart && v.VisitDate < weekEnd);
                results[weekLabel] = count;
            }

            return results;
        }

        /// <summary>
        /// Get monthly visit counts for charts
        /// </summary>
        public Dictionary<string, int> GetMonthlyVisitCounts(int monthsBack = 6)
        {
            var results = new Dictionary<string, int>();

            for (int i = monthsBack - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthLabel = monthStart.ToString("MMM yyyy");

                var count = _db.MedicalHistories
                    .Count(m => m.VisitDate >= monthStart && m.VisitDate <= monthEnd);

                results[monthLabel] = count;
            }

            return results;
        }

        /// <summary>
        /// Get top diagnoses for pie chart
        /// </summary>
        public Dictionary<string, int> GetTopDiagnoses(int topCount = 5)
        {
            return _db.MedicalHistories
                .Where(m => !string.IsNullOrEmpty(m.Diagnosis))
                .GroupBy(m => m.Diagnosis)
                .Select(g => new { Diagnosis = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToDictionary(x => x.Diagnosis, x => x.Count);
        }

        /// <summary>
        /// Get diagnoses by severity
        /// </summary>
        public Dictionary<string, int> GetDiagnosesBySeverity()
        {
            return _db.MedicalHistories
                .Where(m => !string.IsNullOrEmpty(m.Severity))
                .GroupBy(m => m.Severity)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        #endregion

        #region Follow-up Management

        public IEnumerable<MedicalHistory> GetUpcomingFollowUps(int daysAhead = 7)
        {
            var futureDate = DateTime.Now.AddDays(daysAhead);
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .Where(m => m.FollowUpRequired &&
                           m.NextFollowUpDate.HasValue &&
                           m.NextFollowUpDate.Value >= DateTime.Today &&
                           m.NextFollowUpDate.Value <= futureDate)
                .OrderBy(m => m.NextFollowUpDate)
                .ToList();
        }

        public IEnumerable<MedicalHistory> GetOverdueFollowUps()
        {
            return _db.MedicalHistories
                .Include(m => m.Patient)
                .Where(m => m.FollowUpRequired &&
                           m.NextFollowUpDate.HasValue &&
                           m.NextFollowUpDate.Value < DateTime.Today)
                .OrderBy(m => m.NextFollowUpDate)
                .ToList();
        }

        #endregion
    }
}
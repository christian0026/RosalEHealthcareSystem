using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class DashboardService
    {
        private readonly RosalEHealthcareDbContext _db;

        public DashboardService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Admin Dashboard Statistics

        // Total Patients
        public int GetTotalPatients()
        {
            return _db.Patients.Count(p => p.Status != "Archived");
        }

        // Total Patients Last Month
        public int GetTotalPatientsLastMonth()
        {
            var lastMonth = DateTime.Now.AddMonths(-1);
            var startOfLastMonth = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            var endOfLastMonth = startOfLastMonth.AddMonths(1).AddDays(-1);

            return _db.Patients.Count(p => p.DateCreated >= startOfLastMonth && p.DateCreated <= endOfLastMonth);
        }

        // Today's Appointments
        public int GetTodayAppointments()
        {
            var today = DateTime.Today;
            return _db.Appointments.Count(a => DbFunctions.TruncateTime(a.Time) == today);
        }

        // Yesterday's Appointments
        public int GetYesterdayAppointments()
        {
            var yesterday = DateTime.Today.AddDays(-1);
            return _db.Appointments.Count(a => DbFunctions.TruncateTime(a.Time) == yesterday);
        }

        // Low Stock Medicines
        public int GetLowStockMedicines(int threshold = 50)
        {
            return _db.Medicines.Count(m => m.Stock <= threshold && m.Status == "Active");
        }

        // Expiring Medicines (within 30 days)
        public int GetExpiringMedicines(int daysThreshold = 30)
        {
            var thresholdDate = DateTime.Now.AddDays(daysThreshold);
            return _db.Medicines.Count(m => m.ExpiryDate <= thresholdDate && m.ExpiryDate >= DateTime.Now);
        }

        // Active Users
        public int GetActiveUsers()
        {
            return _db.Users.Count(u => u.Status == "Active");
        }

        // Pending Appointments
        public int GetPendingAppointments()
        {
            return _db.Appointments.Count(a => a.Status == "PENDING");
        }

        #endregion

        #region Patient Statistics - Common Illnesses

        public class IllnessStatistic
        {
            public string Illness { get; set; }
            public int Count { get; set; }
            public double Percentage { get; set; }
        }

        public IEnumerable<IllnessStatistic> GetTopCommonIllnesses(int topCount = 10)
        {
            var totalPatients = _db.Patients.Count(p => !string.IsNullOrEmpty(p.PrimaryDiagnosis));

            if (totalPatients == 0) return new List<IllnessStatistic>();

            var illnesses = _db.Patients
                .Where(p => !string.IsNullOrEmpty(p.PrimaryDiagnosis))
                .GroupBy(p => p.PrimaryDiagnosis)
                .Select(g => new IllnessStatistic
                {
                    Illness = g.Key,
                    Count = g.Count(),
                    Percentage = 0 // Will calculate below
                })
                .OrderByDescending(x => x.Count)
                .Take(topCount)
                .ToList();

            // Calculate percentages
            foreach (var illness in illnesses)
            {
                illness.Percentage = Math.Round((double)illness.Count / totalPatients * 100, 1);
            }

            return illnesses;
        }

        #endregion

        #region Patient Visit Trends

        public class VisitTrend
        {
            public string Date { get; set; }
            public int Count { get; set; }
        }

        // Last 7 days visit trends
        public IEnumerable<VisitTrend> GetLast7DaysVisitTrends()
        {
            var trends = new List<VisitTrend>();
            var today = DateTime.Today;

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = _db.Appointments.Count(a => DbFunctions.TruncateTime(a.Time) == date);

                trends.Add(new VisitTrend
                {
                    Date = date.ToString("MMM dd"),
                    Count = count
                });
            }

            return trends;
        }

        // Last 30 days visit trends
        public IEnumerable<VisitTrend> GetLast30DaysVisitTrends()
        {
            var trends = new List<VisitTrend>();
            var today = DateTime.Today;

            for (int i = 29; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var count = _db.Appointments.Count(a => DbFunctions.TruncateTime(a.Time) == date);

                trends.Add(new VisitTrend
                {
                    Date = date.ToString("MMM dd"),
                    Count = count
                });
            }

            return trends;
        }

        // Monthly visit trends (last 12 months)
        public IEnumerable<VisitTrend> GetMonthlyVisitTrends()
        {
            var trends = new List<VisitTrend>();
            var today = DateTime.Today;

            for (int i = 11; i >= 0; i--)
            {
                var targetMonth = today.AddMonths(-i);
                var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var count = _db.Appointments.Count(a => a.Time >= startOfMonth && a.Time <= endOfMonth);

                trends.Add(new VisitTrend
                {
                    Date = targetMonth.ToString("MMM yyyy"),
                    Count = count
                });
            }

            return trends;
        }

        #endregion

        #region Percentage Calculations

        public double CalculatePercentageChange(int current, int previous)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round(((double)(current - previous) / previous) * 100, 1);
        }

        #endregion

        #region Doctor Dashboard Methods

        public int GetPatientsThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.Patients.Count(p => p.DateCreated >= startOfMonth && p.Status != "Archived");
        }

        public double GetPatientGrowthPercentage()
        {
            var lastMonth = GetTotalPatientsLastMonth();
            var thisMonth = GetPatientsThisMonth();
            return CalculatePercentageChange(thisMonth, lastMonth);
        }

        public double GetAppointmentGrowthPercentage()
        {
            var yesterday = GetYesterdayAppointments();
            var today = GetTodayAppointments();
            return CalculatePercentageChange(today, yesterday);
        }

        public Dictionary<string, int> GetAppointmentStatusDistribution()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var statuses = _db.Appointments
                .Where(a => a.Time >= startOfMonth)
                .GroupBy(a => a.Status ?? "Unknown")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var result = new Dictionary<string, int>
    {
        { "CONFIRMED", 0 },
        { "PENDING", 0 },
        { "CANCELLED", 0 },
        { "COMPLETED", 0 }
    };

            foreach (var status in statuses)
            {
                var key = status.Status.ToUpper();
                if (result.ContainsKey(key))
                    result[key] = status.Count;
            }

            return result;
        }

        public Dictionary<string, double> GetAppointmentStatusPercentages()
        {
            var distribution = GetAppointmentStatusDistribution();
            var total = distribution.Values.Sum();

            if (total == 0)
                return distribution.ToDictionary(kvp => kvp.Key, kvp => 0.0);

            return distribution.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Round((double)kvp.Value / total * 100, 1)
            );
        }

        public Dictionary<string, int> GetMonthlyIllnessTrends(int monthsBack = 6)
        {
            var results = new Dictionary<string, int>();

            for (int i = monthsBack - 1; i >= 0; i--)
            {
                var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthLabel = monthStart.ToString("MMM");

                var count = _db.MedicalHistories
                    .Count(m => m.VisitDate >= monthStart && m.VisitDate <= monthEnd);

                results[monthLabel] = count;
            }

            return results;
        }

        public List<ConsultationDisplayModel> GetRecentConsultationsPaged(int pageNumber, int pageSize)
        {
            var consultations = _db.MedicalHistories
                .Include(m => m.Patient)
                .OrderByDescending(m => m.VisitDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return consultations.Select(m => new ConsultationDisplayModel
            {
                Id = m.Id,
                Date = m.VisitDate,
                PatientName = m.Patient?.FullName ?? "Unknown",
                PatientId = m.PatientId,
                Age = m.Patient?.Age ?? 0,
                Diagnosis = m.Diagnosis ?? "N/A",
                Treatment = m.Treatment ?? "N/A",
                FollowUpRequired = m.FollowUpRequired,
                NextFollowUpDate = m.NextFollowUpDate,
                FollowUpStatus = GetFollowUpStatus(m.FollowUpRequired, m.NextFollowUpDate)
            }).ToList();
        }

        public int GetTotalConsultationsCount()
        {
            return _db.MedicalHistories.Count();
        }

        private string GetFollowUpStatus(bool followUpRequired, DateTime? nextFollowUpDate)
        {
            if (!followUpRequired || !nextFollowUpDate.HasValue)
                return "COMPLETED";

            var daysUntil = (nextFollowUpDate.Value.Date - DateTime.Today).Days;

            if (daysUntil < 0) return "OVERDUE";
            if (daysUntil <= 3) return $"{daysUntil} DAYS";
            if (daysUntil <= 7) return "1 WEEK";
            if (daysUntil <= 14) return "2 WEEKS";
            if (daysUntil <= 30) return "1 MONTH";

            return $"{daysUntil} DAYS";
        }

        #endregion
    }
    public class ConsultationDisplayModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string DateFormatted => Date.ToString("MMM dd, yyyy");
        public string PatientName { get; set; }
        public int PatientId { get; set; }
        public int Age { get; set; }
        public string Diagnosis { get; set; }
        public string Treatment { get; set; }
        public bool FollowUpRequired { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string FollowUpStatus { get; set; }

        public string FollowUpBadgeColor
        {
            get
            {
                if (FollowUpStatus == "COMPLETED") return "#4CAF50";
                if (FollowUpStatus == "OVERDUE") return "#F44336";
                if (FollowUpStatus.Contains("DAYS"))
                {
                    var parts = FollowUpStatus.Split(' ');
                    if (parts.Length >= 1 && int.TryParse(parts[0], out int days))
                        return days <= 3 ? "#4CAF50" : "#FF9800";
                }
                if (FollowUpStatus == "1 WEEK") return "#FF9800";
                if (FollowUpStatus == "2 WEEKS") return "#2196F3";
                if (FollowUpStatus == "1 MONTH") return "#9C27B0";
                return "#757575";
            }
        }
    }
}
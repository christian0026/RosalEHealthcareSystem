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
    }
}
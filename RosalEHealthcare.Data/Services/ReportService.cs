using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;

namespace RosalEHealthcare.Data.Services
{
    public class ReportService
    {
        private readonly RosalEHealthcareDbContext _db;

        public ReportService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Report Management

        public IEnumerable<Report> GetAllReports()
        {
            return _db.Reports.OrderByDescending(r => r.DateGenerated).ToList();
        }

        public Report GetReportById(int id)
        {
            return _db.Reports.Find(id);
        }

        public Report CreateReport(Report report)
        {
            report.DateGenerated = DateTime.Now;
            report.ReportId = GenerateReportId();
            report.Status = "Generated";
            _db.Reports.Add(report);
            _db.SaveChanges();
            return report;
        }

        public IEnumerable<ReportTemplate> GetAllTemplates()
        {
            return _db.ReportTemplates.Where(t => t.IsActive).ToList();
        }

        private string GenerateReportId()
        {
            var year = DateTime.Now.Year;
            var lastReport = _db.Reports
                .Where(r => r.ReportId.StartsWith($"RPT-{year}"))
                .OrderByDescending(r => r.ReportId)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastReport != null && !string.IsNullOrEmpty(lastReport.ReportId))
            {
                var parts = lastReport.ReportId.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"RPT-{year}-{nextNumber:D4}";
        }

        #endregion

        #region Dashboard Statistics

        public int GetTotalPatients()
        {
            return _db.Patients.Count(p => p.Status != "Archived");
        }

        public int GetTotalPatientsLastWeek()
        {
            var lastWeek = DateTime.Now.AddDays(-7);
            return _db.Patients.Count(p => p.DateCreated >= lastWeek && p.Status != "Archived");
        }

        public int GetCompletedAppointments()
        {
            return _db.Appointments.Count(a => a.Status == "COMPLETED");
        }

        public int GetCompletedAppointmentsLastWeek()
        {
            var lastWeek = DateTime.Now.AddDays(-7);
            return _db.Appointments.Count(a => a.Status == "COMPLETED" && a.Time >= lastWeek);
        }

        public int GetMedicinesPrescribed()
        {
            return _db.PrescriptionMedicines.Count();
        }

        public int GetMedicinesPrescribedLastWeek()
        {
            var lastWeek = DateTime.Now.AddDays(-7);
            return _db.PrescriptionMedicines
                .Where(pm => _db.Prescriptions.Any(p => p.Id == pm.PrescriptionId && p.CreatedAt >= lastWeek))
                .Count();
        }

        public int GetLowStockCount(int threshold = 50)
        {
            return _db.Medicines.Count(m => m.Stock <= threshold && m.Status == "Active");
        }

        // NEW METHODS FOR DOCTOR MEDICAL REPORTS
        public int GetTotalVisits()
        {
            return _db.Appointments.Count(a => a.Status == "COMPLETED");
        }

        public int GetTotalAppointments()
        {
            return _db.Appointments.Count();
        }

        public double GetShowRate()
        {
            var total = _db.Appointments.Count();
            if (total == 0) return 0;
            var completed = _db.Appointments.Count(a => a.Status == "COMPLETED");
            return Math.Round((double)completed / total * 100, 1);
        }

        public int GetMedicineItemsCount()
        {
            return _db.Medicines.Count(m => m.Status == "Active");
        }

        public int GetSavedTemplates()
        {
            return _db.ReportTemplates.Count(t => t.IsActive);
        }

        public int GetGeneratedReportsCount()
        {
            return _db.Reports.Count();
        }

        #endregion

        #region Weekly Appointment Analysis

        public Dictionary<string, int> GetWeeklyAppointmentAnalysis()
        {
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            var appointments = _db.Appointments
                .Where(a => a.Time >= startOfWeek && a.Time < endOfWeek)
                .ToList();

            return new Dictionary<string, int>
            {
                { "Scheduled", appointments.Count(a => a.Status == "SCHEDULED") },
                { "Completed", appointments.Count(a => a.Status == "COMPLETED") },
                { "No-Shows", appointments.Count(a => a.Status == "NO-SHOW") },
                { "Cancelled", appointments.Count(a => a.Status == "CANCELLED") }
            };
        }

        #endregion

        #region Common Illness Report

        public List<ChartDataPoint> GetCommonIllnessReport()
        {
            var illnesses = _db.Patients
                .Where(p => !string.IsNullOrEmpty(p.PrimaryDiagnosis) && p.Status != "Archived")
                .GroupBy(p => p.PrimaryDiagnosis)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(10)
                .ToList();

            var colors = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF",
                               "#FF9F40", "#FF6384", "#C9CBCF", "#4BC0C0", "#FF9F40" };

            for (int i = 0; i < illnesses.Count; i++)
            {
                illnesses[i].Color = colors[i % colors.Length];
            }

            return illnesses;
        }

        #endregion

        #region Daily Patient Summary

        public Dictionary<string, int> GetDailyPatientSummary()
        {
            var today = DateTime.Today;

            var newPatients = _db.Patients.Count(p =>
                DbFunctions.TruncateTime(p.DateCreated) == today);

            var followUps = _db.Appointments.Count(a =>
                DbFunctions.TruncateTime(a.Time) == today &&
                a.Type == "Follow-up");

            var completed = _db.Appointments.Count(a =>
                DbFunctions.TruncateTime(a.Time) == today &&
                a.Status == "COMPLETED");

            var cancelled = _db.Appointments.Count(a =>
                DbFunctions.TruncateTime(a.Time) == today &&
                a.Status == "CANCELLED");

            return new Dictionary<string, int>
            {
                { "New Patients", newPatients },
                { "Follow-ups", followUps },
                { "Completed", completed },
                { "Cancelled", cancelled }
            };
        }

        #endregion

        #region Medicine Inventory Status

        public Dictionary<string, int> GetMedicineInventoryStatus()
        {
            var totalMedicines = _db.Medicines.Count(m => m.Status == "Active");
            var lowStock = _db.Medicines.Count(m => m.Stock <= 50 && m.Stock > 0 && m.Status == "Active");
            var expiringSoon = _db.Medicines.Count(m =>
                m.ExpiryDate <= DateTime.Now.AddDays(30) &&
                m.ExpiryDate >= DateTime.Now &&
                m.Status == "Active");
            var outOfStock = _db.Medicines.Count(m => m.Stock == 0 && m.Status == "Active");

            return new Dictionary<string, int>
            {
                { "Total Medicines", totalMedicines },
                { "Low Stock", lowStock },
                { "Expiring Soon", expiringSoon },
                { "Out of Stock", outOfStock }
            };
        }

        #endregion

        #region Patient Visit Trends

        public List<ChartDataPoint> GetPatientVisitTrends(string period = "Last 7 Days")
        {
            var trends = new List<ChartDataPoint>();

            if (period == "Last 7 Days")
            {
                for (int i = 6; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var count = _db.Appointments.Count(a =>
                        DbFunctions.TruncateTime(a.Time) == date);

                    trends.Add(new ChartDataPoint
                    {
                        Label = date.ToString("ddd"),
                        Value = count
                    });
                }
            }
            else if (period == "Last 30 Days")
            {
                for (int i = 29; i >= 0; i--)
                {
                    var date = DateTime.Today.AddDays(-i);
                    var count = _db.Appointments.Count(a =>
                        DbFunctions.TruncateTime(a.Time) == date);

                    trends.Add(new ChartDataPoint
                    {
                        Label = date.ToString("MMM dd"),
                        Value = count
                    });
                }
            }
            else if (period == "Last 12 Months")
            {
                for (int i = 11; i >= 0; i--)
                {
                    var targetMonth = DateTime.Today.AddMonths(-i);
                    var startOfMonth = new DateTime(targetMonth.Year, targetMonth.Month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    var count = _db.Appointments.Count(a =>
                        a.Time >= startOfMonth && a.Time <= endOfMonth);

                    trends.Add(new ChartDataPoint
                    {
                        Label = targetMonth.ToString("MMM yy"),
                        Value = count
                    });
                }
            }
            else if (period == "This Week")
            {
                var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                for (int i = 0; i < 7; i++)
                {
                    var date = startOfWeek.AddDays(i);
                    var count = _db.Appointments.Count(a =>
                        DbFunctions.TruncateTime(a.Time) == date);

                    trends.Add(new ChartDataPoint
                    {
                        Label = date.ToString("ddd"),
                        Value = count
                    });
                }
            }
            else if (period == "This Month")
            {
                var startOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                var daysInMonth = DateTime.DaysInMonth(DateTime.Today.Year, DateTime.Today.Month);

                for (int i = 1; i <= daysInMonth; i++)
                {
                    var date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, i);
                    var count = _db.Appointments.Count(a =>
                        DbFunctions.TruncateTime(a.Time) == date);

                    trends.Add(new ChartDataPoint
                    {
                        Label = i.ToString(),
                        Value = count
                    });
                }
            }
            else if (period == "This Year")
            {
                for (int i = 1; i <= 12; i++)
                {
                    var startOfMonth = new DateTime(DateTime.Today.Year, i, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    var count = _db.Appointments.Count(a =>
                        a.Time >= startOfMonth && a.Time <= endOfMonth);

                    trends.Add(new ChartDataPoint
                    {
                        Label = startOfMonth.ToString("MMM"),
                        Value = count
                    });
                }
            }

            return trends;
        }

        #endregion

        #region Top Diagnoses

        public List<ChartDataPoint> GetTopDiagnoses(string period = "Last 7 Days")
        {
            DateTime startDate;

            switch (period)
            {
                case "Last 7 Days":
                    startDate = DateTime.Today.AddDays(-7);
                    break;
                case "Last 30 Days":
                    startDate = DateTime.Today.AddDays(-30);
                    break;
                case "Last 12 Months":
                    startDate = DateTime.Today.AddMonths(-12);
                    break;
                case "This Week":
                    startDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                    break;
                case "This Month":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                case "This Year":
                    startDate = new DateTime(DateTime.Today.Year, 1, 1);
                    break;
                default:
                    startDate = DateTime.Today.AddDays(-7);
                    break;
            }

            var diagnoses = _db.Patients
                .Where(p => !string.IsNullOrEmpty(p.PrimaryDiagnosis) &&
                           p.DateCreated >= startDate &&
                           p.Status != "Archived")
                .GroupBy(p => p.PrimaryDiagnosis)
                .Select(g => new ChartDataPoint
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(x => x.Value)
                .Take(5)
                .ToList();

            return diagnoses;
        }

        #endregion

        #region Generate Custom Report Data

        public ReportData GenerateCustomReport(ReportFilter filter)
        {
            var reportData = new ReportData
            {
                Title = GetReportTitle(filter.ReportType),
                ReportType = filter.ReportType,
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status // Assuming this contains username
            };

            switch (filter.ReportType)
            {
                case "Appointment Summary":
                    reportData = GenerateAppointmentSummaryReport(filter);
                    break;
                case "Patient Demographics":
                    reportData = GeneratePatientDemographicsReport(filter);
                    break;
                case "Medicine Inventory":
                    reportData = GenerateMedicineInventoryReport(filter);
                    break;
                case "Daily Patient Summary":
                    reportData = GenerateDailyPatientSummaryReport(filter);
                    break;
                case "Weekly Analysis":
                    reportData = GenerateWeeklyAnalysisReport(filter);
                    break;
                case "Revenue Report":
                    reportData = GenerateRevenueReport(filter);
                    break;
                default:
                    reportData = GenerateAppointmentSummaryReport(filter);
                    break;
            }

            // Save to database
            SaveReportToDatabase(reportData, filter);

            return reportData;
        }

        private void SaveReportToDatabase(ReportData reportData, ReportFilter filter)
        {
            try
            {
                var report = new Report
                {
                    ReportId = GenerateReportId(),
                    ReportType = reportData.ReportType,
                    Title = reportData.Title,
                    Description = $"Generated on {reportData.GeneratedDate:yyyy-MM-dd HH:mm:ss}",
                    DateGenerated = reportData.GeneratedDate,
                    GeneratedBy = reportData.GeneratedBy,
                    Status = "Completed",
                    Parameters = JsonConvert.SerializeObject(new
                    {
                        filter.ReportType,
                        filter.DateRange,
                        filter.Format,
                        StartDate = filter.StartDate,
                        EndDate = filter.EndDate
                    })
                };

                _db.Reports.Add(report);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving report: {ex.Message}");
            }
        }

        private ReportData GenerateAppointmentSummaryReport(ReportFilter filter)
        {
            var startDate = GetStartDate(filter.DateRange);
            var endDate = DateTime.Today;

            var appointments = _db.Appointments
                .Where(a => a.Time >= startDate && a.Time <= endDate)
                .ToList();

            var reportData = new ReportData
            {
                Title = "Appointment Summary Report",
                ReportType = "Appointment Summary",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Appointments",
                Value = appointments.Count,
                Category = "Overview"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Completed",
                Value = appointments.Count(a => a.Status == "COMPLETED"),
                Category = "Status"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Scheduled",
                Value = appointments.Count(a => a.Status == "SCHEDULED"),
                Category = "Status"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Cancelled",
                Value = appointments.Count(a => a.Status == "CANCELLED"),
                Category = "Status"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "No-Shows",
                Value = appointments.Count(a => a.Status == "NO-SHOW"),
                Category = "Status"
            });

            return reportData;
        }

        private ReportData GeneratePatientDemographicsReport(ReportFilter filter)
        {
            var startDate = GetStartDate(filter.DateRange);
            var endDate = DateTime.Today;

            var patients = _db.Patients
                .Where(p => p.DateCreated >= startDate && p.DateCreated <= endDate && p.Status != "Archived")
                .ToList();

            var reportData = new ReportData
            {
                Title = "Patient Demographics Report",
                ReportType = "Patient Demographics",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            var maleCount = patients.Count(p => p.Gender == "Male");
            var femaleCount = patients.Count(p => p.Gender == "Female");

            // FIXED: Calculate average age properly
            double avgAge = 0;
            if (patients.Any())
            {
                var currentYear = DateTime.Now.Year;
                avgAge = patients.Average(p =>
                {
                    var birthYear = p.BirthDate.HasValue ? p.BirthDate.Value.Year : currentYear;
                    return currentYear - birthYear;
                });
            }

            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Patients",
                Value = patients.Count,
                Category = "Overview"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Male",
                Value = maleCount,
                Category = "Gender"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Female",
                Value = femaleCount,
                Category = "Gender"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Average Age",
                Value = Math.Round(avgAge, 1),
                Category = "Demographics"
            });

            return reportData;
        }

        private ReportData GenerateMedicineInventoryReport(ReportFilter filter)
        {
            var medicines = _db.Medicines.Where(m => m.Status == "Active").ToList();

            var reportData = new ReportData
            {
                Title = "Medicine Inventory Report",
                ReportType = "Medicine Inventory",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Medicines",
                Value = medicines.Count,
                Category = "Overview"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Stock",
                Value = medicines.Sum(m => m.Stock),
                Category = "Stock"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Low Stock Items",
                Value = medicines.Count(m => m.Stock <= 50),
                Category = "Alerts"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Expiring Soon",
                Value = medicines.Count(m => m.ExpiryDate <= DateTime.Now.AddDays(30) && m.ExpiryDate >= DateTime.Now),
                Category = "Alerts"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Out of Stock",
                Value = medicines.Count(m => m.Stock == 0),
                Category = "Alerts"
            });

            return reportData;
        }

        private ReportData GenerateDailyPatientSummaryReport(ReportFilter filter)
        {
            var summary = GetDailyPatientSummary();

            var reportData = new ReportData
            {
                Title = "Daily Patient Summary Report",
                ReportType = "Daily Patient Summary",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            foreach (var item in summary)
            {
                reportData.Statistics.Add(new ReportStatistic
                {
                    Label = item.Key,
                    Value = item.Value,
                    Category = "Daily Summary"
                });
            }

            return reportData;
        }

        private ReportData GenerateWeeklyAnalysisReport(ReportFilter filter)
        {
            var analysis = GetWeeklyAppointmentAnalysis();

            var reportData = new ReportData
            {
                Title = "Weekly Appointment Analysis Report",
                ReportType = "Weekly Analysis",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            foreach (var item in analysis)
            {
                reportData.Statistics.Add(new ReportStatistic
                {
                    Label = item.Key,
                    Value = item.Value,
                    Category = "Weekly Analysis"
                });
            }

            return reportData;
        }

        private ReportData GenerateRevenueReport(ReportFilter filter)
        {
            var reportData = new ReportData
            {
                Title = "Revenue Report",
                ReportType = "Revenue Report",
                GeneratedDate = DateTime.Now,
                GeneratedBy = filter.Status
            };

            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Revenue",
                Value = "$0.00",
                Category = "Financial"
            });
            reportData.Statistics.Add(new ReportStatistic
            {
                Label = "Total Appointments",
                Value = _db.Appointments.Count(),
                Category = "Financial"
            });

            return reportData;
        }

        private string GetReportTitle(string reportType)
        {
            switch (reportType)
            {
                case "Appointment Summary":
                    return "Appointment Summary Report";
                case "Patient Demographics":
                    return "Patient Demographics Report";
                case "Medicine Inventory":
                    return "Medicine Inventory Report";
                case "Daily Patient Summary":
                    return "Daily Patient Summary Report";
                case "Weekly Analysis":
                    return "Weekly Appointment Analysis Report";
                case "Revenue Report":
                    return "Revenue Report";
                default:
                    return "Custom Report";
            }
        }

        private DateTime GetStartDate(string dateRange)
        {
            switch (dateRange)
            {
                case "Last 7 Days":
                    return DateTime.Today.AddDays(-7);
                case "Last 30 Days":
                    return DateTime.Today.AddDays(-30);
                case "Last 12 Months":
                    return DateTime.Today.AddMonths(-12);
                case "This Week":
                    return DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                case "This Month":
                    return new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                case "This Year":
                    return new DateTime(DateTime.Today.Year, 1, 1);
                default:
                    return DateTime.Today.AddDays(-7);
            }
        }

        #endregion
    }
}
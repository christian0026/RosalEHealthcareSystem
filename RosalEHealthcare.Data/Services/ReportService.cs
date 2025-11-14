using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class ReportService
    {
        private readonly RosalEHealthcareDbContext _db;

        public ReportService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

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
            var count = _db.Reports.Count() + 1;
            return string.Format("RPT-{0}-{1:D4}", year, count);
        }

        // Statistics methods
        public int GetTotalPatients()
        {
            return _db.Patients.Count();
        }

        public int GetCompletedAppointments()
        {
            return _db.Appointments.Count(a => a.Status == "Completed");
        }

        public int GetMedicinesPrescribed()
        {
            return _db.PrescriptionMedicines.Count();
        }

        public int GetLowStockCount()
        {
            return _db.Medicines.Count(m => m.Stock < 50);
        }

        public int GetTotalVisits()
        {
            return _db.Appointments.Count();
        }

        public int GetTotalAppointments()
        {
            return _db.Appointments.Count();
        }

        public double GetShowRate()
        {
            var total = _db.Appointments.Count();
            if (total == 0) return 0;
            var completed = _db.Appointments.Count(a => a.Status == "Completed");
            return Math.Round((double)completed / total * 100, 0);
        }

        public int GetMedicineItemsCount()
        {
            return _db.Medicines.Count();
        }

        public int GetSavedTemplates()
        {
            return _db.ReportTemplates.Count(t => t.IsActive);
        }

        public int GetGeneratedReportsCount()
        {
            return _db.Reports.Count();
        }
    }
}
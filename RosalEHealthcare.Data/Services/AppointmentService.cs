using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class AppointmentService
    {
        private readonly RosalEHealthcareDbContext _db;

        public AppointmentService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        public IEnumerable<Appointment> GetAllAppointments()
        {
            return _db.Appointments.OrderByDescending(a => a.Time).ToList();
        }

        public Appointment GetById(int id)
        {
            return _db.Appointments.Find(id);
        }

        public IEnumerable<Appointment> Search(string keyword, DateTime? date = null, string status = null, string timeSlot = null)
        {
            var query = _db.Appointments.AsQueryable();

            // Search by name or Patient ID
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(a =>
                    a.PatientName.ToLower().Contains(keyword) ||
                    a.AppointmentId.ToLower().Contains(keyword) ||
                    (a.PatientId.ToString()).Contains(keyword)
                );
            }

            // Filter by date
            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(a => DbFunctions.TruncateTime(a.Time) == dateOnly);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                query = query.Where(a => a.Status == status);
            }

            // Filter by time slot
            if (!string.IsNullOrWhiteSpace(timeSlot) && timeSlot != "All Time")
            {
                switch (timeSlot)
                {
                    case "Morning":
                        query = query.Where(a => a.Time.Hour >= 6 && a.Time.Hour < 12);
                        break;
                    case "Afternoon":
                        query = query.Where(a => a.Time.Hour >= 12 && a.Time.Hour < 17);
                        break;
                    case "Evening":
                        query = query.Where(a => a.Time.Hour >= 17 && a.Time.Hour < 22);
                        break;
                }
            }

            return query.OrderByDescending(a => a.Time).ToList();
        }

        public void UpdateStatus(int id, string status)
        {
            var appointment = _db.Appointments.FirstOrDefault(a => a.Id == id);
            if (appointment == null) return;
            appointment.Status = status;
            _db.SaveChanges();
        }

        public void AddAppointment(Appointment appt)
        {
            if (string.IsNullOrEmpty(appt.AppointmentId))
            {
                appt.AppointmentId = GenerateAppointmentId();
            }
            appt.CreatedAt = DateTime.Now;
            _db.Appointments.Add(appt);
            _db.SaveChanges();
        }

        public void UpdateAppointment(Appointment appt)
        {
            var existing = _db.Appointments.Find(appt.Id);
            if (existing == null) return;

            existing.PatientId = appt.PatientId;
            existing.PatientName = appt.PatientName;
            existing.Type = appt.Type;
            existing.Condition = appt.Condition;
            existing.Status = appt.Status;
            existing.Time = appt.Time;
            existing.Contact = appt.Contact;

            _db.SaveChanges();
        }

        public void DeleteAppointment(int id)
        {
            var appt = _db.Appointments.Find(id);
            if (appt != null)
            {
                _db.Appointments.Remove(appt);
                _db.SaveChanges();
            }
        }

        public void CancelAppointment(int id, string reason = null)
        {
            var appt = _db.Appointments.Find(id);
            if (appt != null)
            {
                appt.Status = "CANCELLED";
                if (!string.IsNullOrEmpty(reason))
                {
                    appt.Condition = (appt.Condition ?? "") + $"\n[CANCELLED: {reason}]";
                }
                _db.SaveChanges();
            }
        }

        private string GenerateAppointmentId()
        {
            var year = DateTime.Now.Year;
            var lastAppt = _db.Appointments
                .Where(a => a.AppointmentId.StartsWith($"APT-{year}"))
                .OrderByDescending(a => a.AppointmentId)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastAppt != null && !string.IsNullOrEmpty(lastAppt.AppointmentId))
            {
                var parts = lastAppt.AppointmentId.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"APT-{year}-{nextNumber:D4}";
        }

        public int GetTotalCount()
        {
            return _db.Appointments.Count();
        }

        public IEnumerable<Appointment> GetPaged(int pageNumber, int pageSize, string keyword = null, DateTime? date = null, string status = null, string timeSlot = null)
        {
            var query = _db.Appointments.AsQueryable();

            // Apply filters (same as Search method)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(a =>
                    a.PatientName.ToLower().Contains(keyword) ||
                    a.AppointmentId.ToLower().Contains(keyword) ||
                    (a.PatientId.ToString()).Contains(keyword)
                );
            }

            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(a => DbFunctions.TruncateTime(a.Time) == dateOnly);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(timeSlot) && timeSlot != "All Time")
            {
                switch (timeSlot)
                {
                    case "Morning":
                        query = query.Where(a => a.Time.Hour >= 6 && a.Time.Hour < 12);
                        break;
                    case "Afternoon":
                        query = query.Where(a => a.Time.Hour >= 12 && a.Time.Hour < 17);
                        break;
                    case "Evening":
                        query = query.Where(a => a.Time.Hour >= 17 && a.Time.Hour < 22);
                        break;
                }
            }

            return query
                .OrderByDescending(a => a.Time)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public int GetFilteredCount(string keyword = null, DateTime? date = null, string status = null, string timeSlot = null)
        {
            var query = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(a =>
                    a.PatientName.ToLower().Contains(keyword) ||
                    a.AppointmentId.ToLower().Contains(keyword) ||
                    (a.PatientId.ToString()).Contains(keyword)
                );
            }

            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(a => DbFunctions.TruncateTime(a.Time) == dateOnly);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                query = query.Where(a => a.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(timeSlot) && timeSlot != "All Time")
            {
                switch (timeSlot)
                {
                    case "Morning":
                        query = query.Where(a => a.Time.Hour >= 6 && a.Time.Hour < 12);
                        break;
                    case "Afternoon":
                        query = query.Where(a => a.Time.Hour >= 12 && a.Time.Hour < 17);
                        break;
                    case "Evening":
                        query = query.Where(a => a.Time.Hour >= 17 && a.Time.Hour < 22);
                        break;
                }
            }

            return query.Count();
        }
    }
}
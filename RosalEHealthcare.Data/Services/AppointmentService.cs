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

        public IEnumerable<Appointment> Search(string keyword)
        {
            return _db.Appointments
                .Where(a => a.PatientName.Contains(keyword) ||
                            a.AppointmentId.Contains(keyword) ||
                            a.Condition.Contains(keyword))
                .OrderByDescending(a => a.Time)
                .ToList();
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
            _db.Appointments.Add(appt);
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
    }
}
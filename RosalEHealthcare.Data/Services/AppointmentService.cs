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
        private readonly NotificationService _notificationService;

        public AppointmentService(RosalEHealthcareDbContext db)
        {
            _db = db;
            _notificationService = new NotificationService(db);
        }

        #region Basic CRUD

        public IEnumerable<Appointment> GetAllAppointments()
        {
            return _db.Appointments.OrderByDescending(a => a.Time).ToList();
        }

        public Appointment GetById(int id)
        {
            return _db.Appointments.Find(id);
        }

        public void AddAppointment(Appointment appt, string scheduledBy = null)
        {
            if (string.IsNullOrEmpty(appt.AppointmentId))
            {
                appt.AppointmentId = GenerateAppointmentId();
            }
            appt.CreatedAt = DateTime.Now;
            _db.Appointments.Add(appt);
            _db.SaveChanges();

            // TRIGGER NOTIFICATION
            try
            {
                if (!string.IsNullOrEmpty(scheduledBy))
                {
                    _notificationService.NotifyNewAppointment(
                        appt.PatientName,
                        appt.AppointmentId,
                        appt.Time,
                        scheduledBy
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification failed: {ex.Message}");
            }
        }

        public void AddAppointment(Appointment appt)
        {
            AddAppointment(appt, null);
        }

        public bool LinkToPatient(int appointmentId, int patientId)
        {
            var appointment = _db.Appointments.Find(appointmentId);
            if (appointment == null) return false;

            appointment.PatientId = patientId;
            _db.SaveChanges();
            return true;
        }

        public void UpdateAppointment(Appointment appt, string updatedBy = null, bool sendNotification = true)
        {
            var existing = _db.Appointments.Find(appt.Id);
            if (existing == null) return;

            bool timeChanged = existing.Time != appt.Time;

            existing.PatientId = appt.PatientId;
            existing.PatientName = appt.PatientName;
            existing.Type = appt.Type;
            existing.Condition = appt.Condition;
            existing.Status = appt.Status;
            existing.Time = appt.Time;
            existing.Contact = appt.Contact;
            existing.BirthDate = appt.BirthDate;
            existing.Gender = appt.Gender;
            existing.Email = appt.Email;
            existing.Address = appt.Address;

            _db.SaveChanges();

            // TRIGGER NOTIFICATION
            try
            {
                if (sendNotification && timeChanged && !string.IsNullOrEmpty(updatedBy))
                {
                    _notificationService.NotifyAppointmentUpdated(
                        appt.PatientName,
                        existing.AppointmentId,
                        appt.Time,
                        updatedBy
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification failed: {ex.Message}");
            }
        }

        public void UpdateAppointment(Appointment appt)
        {
            UpdateAppointment(appt, null, false);
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

        #endregion

        #region Status Updates with Notifications

        public void UpdateStatus(int id, string status, string changedBy = null, string userRole = null, string reason = null)
        {
            var appointment = _db.Appointments.FirstOrDefault(a => a.Id == id);
            if (appointment == null) return;

            string oldStatus = appointment.Status;
            appointment.Status = status;
            _db.SaveChanges();

            // TRIGGER NOTIFICATIONS
            try
            {
                if (!string.IsNullOrEmpty(changedBy))
                {
                    SendStatusChangeNotification(appointment, status, changedBy, userRole, reason);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Notification failed: {ex.Message}");
            }
        }

        public void UpdateStatus(int id, string status)
        {
            UpdateStatus(id, status, null, null, null);
        }

        private void SendStatusChangeNotification(Appointment appointment, string newStatus, string changedBy, string userRole, string reason)
        {
            switch (newStatus.ToUpper())
            {
                case "CONFIRMED":
                    if (userRole == "Doctor")
                    {
                        _notificationService.NotifyAppointmentConfirmed(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            appointment.Time,
                            changedBy
                        );
                    }
                    break;

                case "COMPLETED":
                case "DONE":
                    if (userRole == "Doctor")
                    {
                        _notificationService.NotifyAppointmentCompleted(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            changedBy
                        );
                    }
                    break;

                case "CANCELLED":
                    if (userRole == "Doctor")
                    {
                        _notificationService.NotifyAppointmentCancelledByDoctor(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            changedBy,
                            reason
                        );
                    }
                    else if (userRole == "Receptionist")
                    {
                        _notificationService.NotifyAppointmentCancelledByReceptionist(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            changedBy,
                            reason
                        );
                    }
                    break;
            }
        }

        public void CancelAppointment(int id, string reason = null, string cancelledBy = null, string userRole = null)
        {
            UpdateStatus(id, "CANCELLED", cancelledBy, userRole, reason);
        }

        public void CancelAppointment(int id, string reason = null)
        {
            CancelAppointment(id, reason, null, null);
        }

        public bool CompleteAppointment(int appointmentId)
        {
            var appointment = _db.Appointments.Find(appointmentId);
            if (appointment == null) return false;

            appointment.Status = "COMPLETED";
            appointment.ConsultationCompletedAt = DateTime.Now;

            if (!appointment.ConsultationStartedAt.HasValue)
            {
                appointment.ConsultationStartedAt = DateTime.Now;
            }

            _db.SaveChanges();
            return true;
        }

        public bool ConfirmAppointment(int appointmentId)
        {
            var appointment = _db.Appointments.Find(appointmentId);
            if (appointment == null) return false;

            appointment.Status = "CONFIRMED";
            _db.SaveChanges();
            return true;
        }

        public bool StartConsultation(int appointmentId)
        {
            var appointment = _db.Appointments.Find(appointmentId);
            if (appointment == null) return false;

            appointment.Status = "IN_PROGRESS";
            appointment.ConsultationStartedAt = DateTime.Now;
            _db.SaveChanges();
            return true;
        }

        #endregion

        #region Search & Filter (Existing Methods)

        public IEnumerable<Appointment> Search(string keyword, DateTime? date = null, string status = null, string timeSlot = null)
        {
            var query = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(a =>
                    a.PatientName.ToLower().Contains(keyword) ||
                    a.AppointmentId.ToLower().Contains(keyword)
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

            return query.OrderByDescending(a => a.Time).ToList();
        }

        public Appointment GetWithPatient(int appointmentId)
        {
            return _db.Appointments
                .Include("Patient")
                .FirstOrDefault(a => a.Id == appointmentId);
        }

        public int GetTotalCount()
        {
            return _db.Appointments.Count();
        }

        public IEnumerable<Appointment> GetPaged(int pageNumber, int pageSize, string keyword = null, DateTime? date = null, string status = null, string timeSlot = null)
        {
            var query = _db.Appointments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                query = query.Where(a =>
                    a.PatientName.ToLower().Contains(keyword) ||
                    a.AppointmentId.ToLower().Contains(keyword)
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
                    a.AppointmentId.ToLower().Contains(keyword)
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

        public List<Appointment> GetTodayAppointments(string status = null)
        {
            var today = DateTime.Today;
            var query = _db.Appointments.Where(a =>
                a.Time.Year == today.Year &&
                a.Time.Month == today.Month &&
                a.Time.Day == today.Day);

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            return query.OrderBy(a => a.Time).ToList();
        }

        public IEnumerable<Appointment> GetUpcomingAppointments(int patientId)
        {
            var now = DateTime.Now;
            return _db.Appointments
                .Where(a => a.PatientId == patientId &&
                            a.Time >= now &&
                            a.Status != "CANCELLED")
                .OrderBy(a => a.Time)
                .ToList();
        }

        public bool IsTimeSlotAvailable(DateTime appointmentTime, int? excludeAppointmentId = null)
        {
            var query = _db.Appointments
                .Where(a => DbFunctions.TruncateTime(a.Time) == appointmentTime.Date &&
                            a.Time.Hour == appointmentTime.Hour &&
                            a.Time.Minute == appointmentTime.Minute &&
                            a.Status != "CANCELLED");

            if (excludeAppointmentId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAppointmentId.Value);
            }

            return !query.Any();
        }

        private string GenerateAppointmentId()
        {
            var year = DateTime.Now.Year;
            var prefix = $"APT-{year}";

            var lastAppt = _db.Appointments
                .Where(a => a.AppointmentId.StartsWith(prefix))
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

        #endregion
    }
}
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

        /// <summary>
        /// Add a new appointment and notify doctors
        /// </summary>
        /// <param name="appt">Appointment to add</param>
        /// <param name="scheduledBy">Name of user who scheduled the appointment</param>
        public void AddAppointment(Appointment appt, string scheduledBy = null)
        {
            if (string.IsNullOrEmpty(appt.AppointmentId))
            {
                appt.AppointmentId = GenerateAppointmentId();
            }
            appt.CreatedAt = DateTime.Now;
            _db.Appointments.Add(appt);
            _db.SaveChanges();

            // Send notification to doctors
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
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        // Overload for backward compatibility
        public void AddAppointment(Appointment appt)
        {
            AddAppointment(appt, null);
        }

        /// <summary>
        /// Update an appointment and optionally notify
        /// </summary>
        /// <param name="appt">Appointment to update</param>
        /// <param name="updatedBy">Name of user who updated</param>
        /// <param name="sendNotification">Whether to send notification</param>
        public void UpdateAppointment(Appointment appt, string updatedBy = null, bool sendNotification = true)
        {
            var existing = _db.Appointments.Find(appt.Id);
            if (existing == null) return;

            // Check if time changed for notification
            bool timeChanged = existing.Time != appt.Time;
            DateTime oldTime = existing.Time;

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

            // Send notification if time changed
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
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        // Overload for backward compatibility
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

        /// <summary>
        /// Update appointment status with notifications
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="status">New status</param>
        /// <param name="changedBy">Name of user who changed status</param>
        /// <param name="userRole">Role of user (Doctor/Receptionist)</param>
        /// <param name="reason">Reason for cancellation (if applicable)</param>
        public void UpdateStatus(int id, string status, string changedBy = null, string userRole = null, string reason = null)
        {
            var appointment = _db.Appointments.FirstOrDefault(a => a.Id == id);
            if (appointment == null) return;

            string oldStatus = appointment.Status;
            appointment.Status = status;
            _db.SaveChanges();

            // Send notifications based on status change
            try
            {
                if (!string.IsNullOrEmpty(changedBy))
                {
                    SendStatusChangeNotification(appointment, oldStatus, status, changedBy, userRole, reason);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        // Overload for backward compatibility
        public void UpdateStatus(int id, string status)
        {
            UpdateStatus(id, status, null, null, null);
        }

        /// <summary>
        /// Send appropriate notification based on status change
        /// </summary>
        private void SendStatusChangeNotification(Appointment appointment, string oldStatus, string newStatus,
            string changedBy, string userRole, string reason)
        {
            switch (newStatus.ToUpper())
            {
                case "CONFIRMED":
                    // Doctor confirmed - notify receptionist
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
                    // Doctor completed consultation - notify receptionist
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
                    // Determine who cancelled and notify the other party
                    if (userRole == "Doctor")
                    {
                        // Doctor cancelled - notify receptionist
                        _notificationService.NotifyAppointmentCancelledByDoctor(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            changedBy,
                            reason
                        );
                    }
                    else if (userRole == "Receptionist")
                    {
                        // Receptionist cancelled - notify doctor
                        _notificationService.NotifyAppointmentCancelledByReceptionist(
                            appointment.PatientName,
                            appointment.AppointmentId,
                            changedBy,
                            reason
                        );
                    }
                    break;

                case "IN PROGRESS":
                case "ONGOING":
                    // Patient is now with doctor - could notify receptionist
                    // Optional: Add notification if needed
                    break;
            }
        }

        /// <summary>
        /// Cancel an appointment with reason and notification
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="reason">Cancellation reason</param>
        /// <param name="cancelledBy">Name of user who cancelled</param>
        /// <param name="userRole">Role of user</param>
        public void CancelAppointment(int id, string reason = null, string cancelledBy = null, string userRole = null)
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

                // Send notification
                try
                {
                    if (!string.IsNullOrEmpty(cancelledBy))
                    {
                        if (userRole == "Doctor")
                        {
                            _notificationService.NotifyAppointmentCancelledByDoctor(
                                appt.PatientName,
                                appt.AppointmentId,
                                cancelledBy,
                                reason
                            );
                        }
                        else
                        {
                            _notificationService.NotifyAppointmentCancelledByReceptionist(
                                appt.PatientName,
                                appt.AppointmentId,
                                cancelledBy,
                                reason
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
                }
            }
        }

        // Overload for backward compatibility
        public void CancelAppointment(int id, string reason = null)
        {
            CancelAppointment(id, reason, null, null);
        }

        /// <summary>
        /// Mark appointment as completed (Doctor action)
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="doctorName">Name of doctor</param>
        public void CompleteAppointment(int id, string doctorName)
        {
            UpdateStatus(id, "COMPLETED", doctorName, "Doctor");
        }

        /// <summary>
        /// Confirm appointment (Doctor action)
        /// </summary>
        /// <param name="id">Appointment ID</param>
        /// <param name="doctorName">Name of doctor</param>
        public void ConfirmAppointment(int id, string doctorName)
        {
            UpdateStatus(id, "CONFIRMED", doctorName, "Doctor");
        }

        #endregion

        #region Search & Filter

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

        #endregion

        #region Helper Methods

        public IEnumerable<Appointment> GetTodayAppointments()
        {
            var today = DateTime.Today;
            return _db.Appointments
                .Where(a => DbFunctions.TruncateTime(a.Time) == today)
                .OrderBy(a => a.Time)
                .ToList();
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
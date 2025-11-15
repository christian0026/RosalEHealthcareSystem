using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Media;

namespace RosalEHealthcare.Data.Services
{
    public class NotificationService
    {
        private readonly RosalEHealthcareDbContext _db;

        public NotificationService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Create Notifications

        public Notification CreateNotification(string type, string title, string message,
            string targetUser = "All", string targetRole = null, string priority = "Normal",
            string relatedEntityId = null)
        {
            var notification = new Notification
            {
                Type = type,
                Title = title,
                Message = message,
                TargetUser = targetUser,
                TargetRole = targetRole,
                Priority = priority,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            // Set icon and color based on type
            SetNotificationStyle(notification);

            _db.Notifications.Add(notification);
            _db.SaveChanges();

            return notification;
        }

        public void NotifyNewPatient(Patient patient, string receptionist)
        {
            CreateNotification(
                type: "NewPatient",
                title: "New Patient Registered",
                message: $"{patient.FullName} has been registered by {receptionist}",
                targetRole: "Doctor",
                priority: "Normal",
                relatedEntityId: patient.Id.ToString()
            );
        }

        public void NotifyAppointmentReminder(Appointment appointment)
        {
            CreateNotification(
                type: "AppointmentReminder",
                title: "Upcoming Appointment",
                message: $"Appointment with {appointment.PatientName} in 1 hour",
                targetRole: "Doctor",
                priority: "High",
                relatedEntityId: appointment.Id.ToString()
            );
        }

        public void NotifyLowStock(Medicine medicine)
        {
            CreateNotification(
                type: "LowStock",
                title: "Low Stock Alert",
                message: $"{medicine.Name} is running low (Stock: {medicine.Stock})",
                targetRole: "All",
                priority: "High",
                relatedEntityId: medicine.Id.ToString()
            );
        }

        public void NotifyPrescriptionReady(Prescription prescription)
        {
            CreateNotification(
                type: "PrescriptionReady",
                title: "Prescription Ready",
                message: $"Prescription for {prescription.PatientName} is ready",
                targetRole: "Receptionist",
                priority: "Normal",
                relatedEntityId: prescription.Id.ToString()
            );
        }

        #endregion

        #region Read Notifications

        public IEnumerable<Notification> GetUnreadNotifications(string user, string role)
        {
            return _db.Notifications
                .Where(n => !n.IsRead &&
                       (n.TargetUser == user || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public IEnumerable<Notification> GetAllNotifications(string user, string role)
        {
            return _db.Notifications
                .Where(n => (n.TargetUser == user || n.TargetUser == "All") &&
                           (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public int GetUnreadCount(string user, string role)
        {
            return _db.Notifications
                .Count(n => !n.IsRead &&
                       (n.TargetUser == user || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null));
        }

        #endregion

        #region Mark as Read

        public void MarkAsRead(int notificationId)
        {
            var notification = _db.Notifications.Find(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                _db.SaveChanges();
            }
        }

        public void MarkAllAsRead(string user, string role)
        {
            var notifications = _db.Notifications
                .Where(n => !n.IsRead &&
                       (n.TargetUser == user || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .ToList();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            _db.SaveChanges();
        }

        #endregion

        #region Delete Notifications

        public void DeleteNotification(int notificationId)
        {
            var notification = _db.Notifications.Find(notificationId);
            if (notification != null)
            {
                _db.Notifications.Remove(notification);
                _db.SaveChanges();
            }
        }

        public void DeleteAllRead(string user, string role)
        {
            var notifications = _db.Notifications
                .Where(n => n.IsRead &&
                       (n.TargetUser == user || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .ToList();

            _db.Notifications.RemoveRange(notifications);
            _db.SaveChanges();
        }

        #endregion

        #region Helper Methods

        private void SetNotificationStyle(Notification notification)
        {
            switch (notification.Type)
            {
                case "NewPatient":
                    notification.Icon = "AccountPlus";
                    notification.Color = "#4CAF50";
                    break;
                case "AppointmentReminder":
                    notification.Icon = "CalendarClock";
                    notification.Color = "#2196F3";
                    break;
                case "LowStock":
                    notification.Icon = "AlertCircle";
                    notification.Color = "#FF9800";
                    break;
                case "PrescriptionReady":
                    notification.Icon = "Pill";
                    notification.Color = "#9C27B0";
                    break;
                case "SystemAlert":
                    notification.Icon = "Alert";
                    notification.Color = "#F44336";
                    break;
                default:
                    notification.Icon = "Information";
                    notification.Color = "#607D8B";
                    break;
            }
        }

        public void PlayNotificationSound()
        {
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Ignore sound errors
            }
        }

        #endregion
    }
}
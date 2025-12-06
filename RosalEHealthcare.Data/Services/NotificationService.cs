using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

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
            string relatedEntityId = null, string actionUrl = null)
        {
            try
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
                    ActionUrl = actionUrl,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                SetNotificationStyle(notification);

                _db.Notifications.Add(notification);
                _db.SaveChanges();

                return notification;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating notification: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Patient Notifications

        public void NotifyNewPatient(string patientName, string patientId, string registeredBy)
        {
            // Notify Doctor
            CreateNotification(
                type: "NewPatient",
                title: "New Patient Registered",
                message: $"{patientName} (ID: {patientId}) has been registered by {registeredBy}",
                targetRole: "Doctor",
                priority: "Normal",
                relatedEntityId: patientId,
                actionUrl: "PatientManagement"
            );

            // Notify Admin (Standardized to 'Administrator')
            CreateNotification(
                type: "NewPatient",
                title: "New Patient Registered",
                message: $"{patientName} (ID: {patientId}) has been registered by {registeredBy}",
                targetRole: "Administrator",
                priority: "Low",
                relatedEntityId: patientId,
                actionUrl: "PatientManagement"
            );
        }

        public void NotifyPatientUpdated(string patientName, string patientId, string updatedBy)
        {
            CreateNotification(
                type: "PatientUpdated",
                title: "Patient Information Updated",
                message: $"{patientName}'s information has been updated by {updatedBy}",
                targetRole: "Doctor",
                priority: "Low",
                relatedEntityId: patientId,
                actionUrl: "PatientManagement"
            );
        }

        #endregion

        #region Appointment Notifications

        public void NotifyNewAppointment(string patientName, string appointmentId, DateTime appointmentTime, string scheduledBy)
        {
            string timeStr = appointmentTime.ToString("MMM dd, yyyy 'at' h:mm tt");

            // Notify Doctor
            CreateNotification(
                type: "NewAppointment",
                title: "New Appointment Scheduled",
                message: $"Appointment for {patientName} on {timeStr} - Scheduled by {scheduledBy}",
                targetRole: "Doctor",
                priority: "Normal",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        public void NotifyAppointmentUpdated(string patientName, string appointmentId, DateTime appointmentTime, string updatedBy)
        {
            string timeStr = appointmentTime.ToString("MMM dd, yyyy 'at' h:mm tt");

            CreateNotification(
                type: "AppointmentUpdated",
                title: "Appointment Updated",
                message: $"Appointment for {patientName} has been rescheduled to {timeStr} by {updatedBy}",
                targetRole: "Doctor",
                priority: "Normal",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        public void NotifyAppointmentCancelledByReceptionist(string patientName, string appointmentId, string cancelledBy, string reason = null)
        {
            string message = $"Appointment for {patientName} has been cancelled by {cancelledBy}";
            if (!string.IsNullOrEmpty(reason)) message += $". Reason: {reason}";

            CreateNotification(
                type: "AppointmentCancelled",
                title: "Appointment Cancelled",
                message: message,
                targetRole: "Doctor",
                priority: "High",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        // For Receptionist: When Doctor confirms
        public void NotifyAppointmentConfirmed(string patientName, string appointmentId, DateTime appointmentTime, string confirmedBy)
        {
            string timeStr = appointmentTime.ToString("MMM dd, yyyy 'at' h:mm tt");

            CreateNotification(
                type: "AppointmentConfirmed",
                title: "Appointment Confirmed",
                message: $"Appointment for {patientName} on {timeStr} has been confirmed by {confirmedBy}",
                targetRole: "Receptionist",
                priority: "Normal",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        // For Receptionist: When Doctor completes consultation
        public void NotifyAppointmentCompleted(string patientName, string appointmentId, string completedBy)
        {
            CreateNotification(
                type: "AppointmentCompleted",
                title: "Consultation Completed",
                message: $"Consultation with {patientName} has been completed by Dr. {completedBy}. Patient is ready for billing/discharge.",
                targetRole: "Receptionist",
                priority: "High",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        // For Receptionist: When Doctor cancels
        public void NotifyAppointmentCancelledByDoctor(string patientName, string appointmentId, string cancelledBy, string reason = null)
        {
            string message = $"Appointment for {patientName} has been cancelled by Dr. {cancelledBy}";
            if (!string.IsNullOrEmpty(reason)) message += $". Reason: {reason}";

            CreateNotification(
                type: "AppointmentCancelled",
                title: "Appointment Cancelled by Doctor",
                message: message,
                targetRole: "Receptionist",
                priority: "High",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        public void NotifyAppointmentReminder(string patientName, string appointmentId, DateTime appointmentTime, int minutesBefore = 30)
        {
            CreateNotification(
                type: "AppointmentReminder",
                title: "Upcoming Appointment",
                message: $"Appointment with {patientName} in {minutesBefore} minutes",
                targetRole: "Doctor",
                priority: "High",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        #endregion

        #region Medicine/Inventory Notifications

        public void NotifyLowStock(string medicineName, string medicineId, int currentStock, int threshold = 20)
        {
            string msg = $"{medicineName} is running low. Current stock: {currentStock} units (Threshold: {threshold})";

            CreateNotification("LowStock", "Low Stock Alert", msg, "All", "Doctor", "High", medicineId, "MedicineInventory");
            CreateNotification("LowStock", "Low Stock Alert", msg, "All", "Administrator", "High", medicineId, "MedicineInventory");
        }

        public void NotifyExpiringMedicine(string medicineName, string medicineId, DateTime expiryDate)
        {
            int days = (expiryDate - DateTime.Today).Days;
            string urgency = days <= 7 ? "URGENT: " : "";
            string msg = $"{medicineName} will expire on {expiryDate:MMM dd, yyyy} ({days} days remaining)";

            CreateNotification("ExpiringMedicine", $"{urgency}Medicine Expiring Soon", msg, "All", "Doctor", days <= 7 ? "Urgent" : "High", medicineId, "MedicineInventory");
            CreateNotification("ExpiringMedicine", $"{urgency}Medicine Expiring Soon", msg, "All", "Administrator", days <= 7 ? "Urgent" : "High", medicineId, "MedicineInventory");
        }

        public void NotifyOutOfStock(string medicineName, string medicineId)
        {
            string msg = $"{medicineName} is now out of stock!";

            CreateNotification("OutOfStock", "Out of Stock Alert", msg, "All", "Doctor", "Urgent", medicineId, "MedicineInventory");
            CreateNotification("OutOfStock", "Out of Stock Alert", msg, "All", "Administrator", "Urgent", medicineId, "MedicineInventory");
            CreateNotification("OutOfStock", "Out of Stock Alert", msg, "All", "Receptionist", "High", medicineId, "MedicineInventory");
        }

        #endregion

        #region Prescription Notifications

        // For Receptionist: When prescription is created
        public void NotifyPrescriptionReady(string patientName, string prescriptionId, string doctorName)
        {
            CreateNotification(
                type: "PrescriptionReady",
                title: "Prescription Ready",
                message: $"Prescription for {patientName} is ready. Prepared by Dr. {doctorName}",
                targetRole: "Receptionist",
                priority: "Normal",
                relatedEntityId: prescriptionId,
                actionUrl: "Prescriptions"
            );
        }

        #endregion

        #region Admin Notifications

        public void NotifyNewUserCreated(string username, string role, string createdBy)
        {
            CreateNotification(
                type: "NewUser",
                title: "New User Account Created",
                message: $"New {role} account '{username}' has been created by {createdBy}",
                targetRole: "Administrator",
                priority: "Normal",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        public void NotifyUserModified(string username, string modifiedBy, string changes)
        {
            CreateNotification(
                type: "UserModified",
                title: "User Account Modified",
                message: $"Account '{username}' has been modified by {modifiedBy}. Changes: {changes}",
                targetRole: "Administrator",
                priority: "Low",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        public void NotifyLoginFailedAttempts(string username, int attemptCount, string ipAddress = null)
        {
            CreateNotification(
                type: "SecurityAlert",
                title: "Security Alert: Failed Login Attempts",
                message: $"Multiple failed login attempts ({attemptCount}) for account '{username}'",
                targetRole: "Administrator",
                priority: "Urgent",
                relatedEntityId: username,
                actionUrl: "SystemSettings"
            );
        }

        public void NotifyAccountLocked(string username, string reason = null)
        {
            CreateNotification(
                type: "AccountLocked",
                title: "Account Locked",
                message: $"Account '{username}' has been locked. Reason: {reason ?? "N/A"}",
                targetRole: "Administrator",
                priority: "High",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        public void NotifyBackupCompleted(bool success, string backupPath = null, string errorMessage = null)
        {
            if (success)
            {
                CreateNotification("BackupSuccess", "Backup Completed", $"System backup successful. Location: {backupPath}", "All", "Administrator", "Low", null, "SystemSettings");
            }
            else
            {
                CreateNotification("BackupFailed", "Backup Failed", $"Backup failed: {errorMessage}", "All", "Administrator", "Urgent", null, "SystemSettings");
            }
        }

        public void NotifyRestoreCompleted(bool success, string restoreFile = null, string errorMessage = null)
        {
            if (success)
                CreateNotification("RestoreSuccess", "Restore Completed", $"Database restored from: {restoreFile}", "All", "Administrator", "High", null, "SystemSettings");
            else
                CreateNotification("RestoreFailed", "Restore Failed", $"Restore failed: {errorMessage}", "All", "Administrator", "Urgent", null, "SystemSettings");
        }

        public void NotifySettingsChanged(string settingCategory, string changedBy)
        {
            CreateNotification("SettingsChanged", "Settings Updated", $"{settingCategory} settings modified by {changedBy}", "All", "Administrator", "Low", null, "SystemSettings");
        }

        #endregion

        #region Read/Count Methods (Bridge for Polling Service)

        public IEnumerable<Notification> GetUnreadNotifications(string username, string role)
        {
            // Handles "Administrator" vs "Admin" ambiguity by checking startsWith
            return _db.Notifications
                .Where(n => !n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null ||
                        (role == "Administrator" && n.TargetRole == "Admin") ||
                        (role == "Admin" && n.TargetRole == "Administrator")))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public int GetUnreadCount(string username, string role)
        {
            return _db.Notifications
                .Count(n => !n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null ||
                        (role == "Administrator" && n.TargetRole == "Admin") ||
                        (role == "Admin" && n.TargetRole == "Administrator")));
        }

        public IEnumerable<Notification> GetAllNotifications(string username, string role, int? limit = null)
        {
            var query = _db.Notifications
                .Where(n => (n.TargetUser == username || n.TargetUser == "All") &&
                           (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null ||
                            (role == "Administrator" && n.TargetRole == "Admin") ||
                            (role == "Admin" && n.TargetRole == "Administrator")))
                .OrderByDescending(n => n.CreatedAt);

            if (limit.HasValue) return query.Take(limit.Value).ToList();
            return query.ToList();
        }

        public void MarkAsRead(int notificationId)
        {
            var n = _db.Notifications.Find(notificationId);
            if (n != null) { n.IsRead = true; n.ReadAt = DateTime.Now; _db.SaveChanges(); }
        }

        public void MarkAllAsRead(string username, string role)
        {
            var notifications = GetUnreadNotifications(username, role).ToList();
            foreach (var n in notifications) { n.IsRead = true; n.ReadAt = DateTime.Now; }
            _db.SaveChanges();
        }

        public void DeleteNotification(int notificationId)
        {
            var n = _db.Notifications.Find(notificationId);
            if (n != null) { _db.Notifications.Remove(n); _db.SaveChanges(); }
        }

        public IEnumerable<Notification> GetNewNotificationsSince(string username, string role, DateTime since)
        {
            return _db.Notifications
                .Where(n => !n.IsRead && n.CreatedAt > since &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null ||
                        (role == "Administrator" && n.TargetRole == "Admin") ||
                        (role == "Admin" && n.TargetRole == "Administrator")))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        #endregion

        #region Styling Helper

        private void SetNotificationStyle(Notification notification)
        {
            switch (notification.Type)
            {
                case "NewPatient":
                    notification.Icon = "AccountPlus"; notification.Color = "#4CAF50"; break;
                case "PatientUpdated":
                    notification.Icon = "AccountEdit"; notification.Color = "#2196F3"; break;
                case "NewAppointment":
                    notification.Icon = "CalendarPlus"; notification.Color = "#4CAF50"; break;
                case "AppointmentUpdated":
                    notification.Icon = "CalendarEdit"; notification.Color = "#FF9800"; break;
                case "AppointmentCancelled":
                    notification.Icon = "CalendarRemove"; notification.Color = "#F44336"; break;
                case "AppointmentConfirmed":
                    notification.Icon = "CalendarCheck"; notification.Color = "#4CAF50"; break;
                case "AppointmentCompleted":
                    notification.Icon = "CheckCircle"; notification.Color = "#4CAF50"; break;
                case "LowStock":
                    notification.Icon = "PackageVariantClosed"; notification.Color = "#FF9800"; break;
                case "OutOfStock":
                    notification.Icon = "PackageVariantRemove"; notification.Color = "#F44336"; break;
                case "ExpiringMedicine":
                    notification.Icon = "TimerSand"; notification.Color = "#FF9800"; break;
                case "PrescriptionReady":
                    notification.Icon = "Pill"; notification.Color = "#9C27B0"; break;
                case "NewUser":
                    notification.Icon = "AccountPlus"; notification.Color = "#2196F3"; break;
                case "SecurityAlert":
                    notification.Icon = "ShieldAlert"; notification.Color = "#F44336"; break;
                case "AccountLocked":
                    notification.Icon = "AccountLock"; notification.Color = "#F44336"; break;
                case "BackupSuccess":
                case "RestoreSuccess":
                    notification.Icon = "DatabaseCheck"; notification.Color = "#4CAF50"; break;
                case "BackupFailed":
                case "RestoreFailed":
                    notification.Icon = "DatabaseAlert"; notification.Color = "#F44336"; break;
                default:
                    notification.Icon = "Information"; notification.Color = "#607D8B"; break;
            }
        }

        #endregion
    }
}
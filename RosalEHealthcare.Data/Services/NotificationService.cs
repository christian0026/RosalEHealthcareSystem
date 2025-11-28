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

        /// <summary>
        /// Creates a new notification and saves it to the database
        /// </summary>
        public Notification CreateNotification(string type, string title, string message,
            string targetUser = "All", string targetRole = null, string priority = "Normal",
            string relatedEntityId = null, string actionUrl = null)
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

            // Set icon and color based on type
            SetNotificationStyle(notification);

            _db.Notifications.Add(notification);
            _db.SaveChanges();

            return notification;
        }

        #endregion

        #region Patient Notifications

        /// <summary>
        /// Notify doctors when a new patient is registered
        /// </summary>
        public void NotifyNewPatient(string patientName, string patientId, string registeredBy)
        {
            CreateNotification(
                type: "NewPatient",
                title: "New Patient Registered",
                message: $"{patientName} (ID: {patientId}) has been registered by {registeredBy}",
                targetRole: "Doctor",
                priority: "Normal",
                relatedEntityId: patientId,
                actionUrl: "PatientManagement"
            );

            // Also notify Admin
            CreateNotification(
                type: "NewPatient",
                title: "New Patient Registered",
                message: $"{patientName} (ID: {patientId}) has been registered by {registeredBy}",
                targetRole: "Admin",
                priority: "Low",
                relatedEntityId: patientId,
                actionUrl: "PatientManagement"
            );
        }

        /// <summary>
        /// Notify when patient information is updated
        /// </summary>
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

        /// <summary>
        /// Notify doctors when a new appointment is scheduled
        /// </summary>
        public void NotifyNewAppointment(string patientName, string appointmentId, DateTime appointmentTime, string scheduledBy)
        {
            string timeStr = appointmentTime.ToString("MMM dd, yyyy 'at' h:mm tt");
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

        /// <summary>
        /// Notify doctors when an appointment is updated
        /// </summary>
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

        /// <summary>
        /// Notify doctors when an appointment is cancelled by receptionist
        /// </summary>
        public void NotifyAppointmentCancelledByReceptionist(string patientName, string appointmentId, string cancelledBy, string reason = null)
        {
            string message = $"Appointment for {patientName} has been cancelled by {cancelledBy}";
            if (!string.IsNullOrEmpty(reason))
                message += $". Reason: {reason}";

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

        /// <summary>
        /// Notify receptionist when doctor confirms an appointment
        /// </summary>
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

        /// <summary>
        /// Notify receptionist when doctor completes an appointment/consultation
        /// </summary>
        public void NotifyAppointmentCompleted(string patientName, string appointmentId, string completedBy)
        {
            CreateNotification(
                type: "AppointmentCompleted",
                title: "Consultation Completed",
                message: $"Consultation with {patientName} has been completed by Dr. {completedBy}. Patient may proceed.",
                targetRole: "Receptionist",
                priority: "High",
                relatedEntityId: appointmentId,
                actionUrl: "Appointments"
            );
        }

        /// <summary>
        /// Notify receptionist when doctor cancels an appointment
        /// </summary>
        public void NotifyAppointmentCancelledByDoctor(string patientName, string appointmentId, string cancelledBy, string reason = null)
        {
            string message = $"Appointment for {patientName} has been cancelled by Dr. {cancelledBy}";
            if (!string.IsNullOrEmpty(reason))
                message += $". Reason: {reason}";

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

        /// <summary>
        /// Notify about upcoming appointment (reminder)
        /// </summary>
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

        /// <summary>
        /// Notify about low stock medicine
        /// </summary>
        public void NotifyLowStock(string medicineName, string medicineId, int currentStock, int threshold = 20)
        {
            // Notify Doctor
            CreateNotification(
                type: "LowStock",
                title: "Low Stock Alert",
                message: $"{medicineName} is running low. Current stock: {currentStock} units (Threshold: {threshold})",
                targetRole: "Doctor",
                priority: "High",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );

            // Notify Admin
            CreateNotification(
                type: "LowStock",
                title: "Low Stock Alert",
                message: $"{medicineName} is running low. Current stock: {currentStock} units (Threshold: {threshold})",
                targetRole: "Admin",
                priority: "High",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );
        }

        /// <summary>
        /// Notify about expiring medicine
        /// </summary>
        public void NotifyExpiringMedicine(string medicineName, string medicineId, DateTime expiryDate)
        {
            int daysUntilExpiry = (expiryDate - DateTime.Today).Days;
            string urgency = daysUntilExpiry <= 7 ? "URGENT: " : "";

            // Notify Doctor
            CreateNotification(
                type: "ExpiringMedicine",
                title: $"{urgency}Medicine Expiring Soon",
                message: $"{medicineName} will expire on {expiryDate:MMM dd, yyyy} ({daysUntilExpiry} days remaining)",
                targetRole: "Doctor",
                priority: daysUntilExpiry <= 7 ? "Urgent" : "High",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );

            // Notify Admin
            CreateNotification(
                type: "ExpiringMedicine",
                title: $"{urgency}Medicine Expiring Soon",
                message: $"{medicineName} will expire on {expiryDate:MMM dd, yyyy} ({daysUntilExpiry} days remaining)",
                targetRole: "Admin",
                priority: daysUntilExpiry <= 7 ? "Urgent" : "High",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );
        }

        /// <summary>
        /// Notify about out of stock medicine
        /// </summary>
        public void NotifyOutOfStock(string medicineName, string medicineId)
        {
            // Notify all roles
            CreateNotification(
                type: "OutOfStock",
                title: "Out of Stock Alert",
                message: $"{medicineName} is now out of stock!",
                targetRole: "Doctor",
                priority: "Urgent",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );

            CreateNotification(
                type: "OutOfStock",
                title: "Out of Stock Alert",
                message: $"{medicineName} is now out of stock!",
                targetRole: "Admin",
                priority: "Urgent",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );

            CreateNotification(
                type: "OutOfStock",
                title: "Out of Stock Alert",
                message: $"{medicineName} is now out of stock!",
                targetRole: "Receptionist",
                priority: "High",
                relatedEntityId: medicineId,
                actionUrl: "MedicineInventory"
            );
        }

        #endregion

        #region Prescription Notifications

        /// <summary>
        /// Notify receptionist when prescription is ready
        /// </summary>
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

        /// <summary>
        /// Notify admin when a new user is created
        /// </summary>
        public void NotifyNewUserCreated(string username, string role, string createdBy)
        {
            CreateNotification(
                type: "NewUser",
                title: "New User Account Created",
                message: $"New {role} account '{username}' has been created by {createdBy}",
                targetRole: "Admin",
                priority: "Normal",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        /// <summary>
        /// Notify admin when user account is modified
        /// </summary>
        public void NotifyUserModified(string username, string modifiedBy, string changes)
        {
            CreateNotification(
                type: "UserModified",
                title: "User Account Modified",
                message: $"Account '{username}' has been modified by {modifiedBy}. Changes: {changes}",
                targetRole: "Admin",
                priority: "Low",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        /// <summary>
        /// Notify admin about multiple failed login attempts
        /// </summary>
        public void NotifyLoginFailedAttempts(string username, int attemptCount, string ipAddress = null)
        {
            string message = $"Multiple failed login attempts ({attemptCount}) for account '{username}'";
            if (!string.IsNullOrEmpty(ipAddress))
                message += $" from IP: {ipAddress}";

            CreateNotification(
                type: "SecurityAlert",
                title: "Security Alert: Failed Login Attempts",
                message: message,
                targetRole: "Admin",
                priority: "Urgent",
                relatedEntityId: username,
                actionUrl: "SystemSettings"
            );
        }

        /// <summary>
        /// Notify admin about account lockout
        /// </summary>
        public void NotifyAccountLocked(string username, string reason = null)
        {
            string message = $"Account '{username}' has been locked";
            if (!string.IsNullOrEmpty(reason))
                message += $". Reason: {reason}";

            CreateNotification(
                type: "AccountLocked",
                title: "Account Locked",
                message: message,
                targetRole: "Admin",
                priority: "High",
                relatedEntityId: username,
                actionUrl: "UserManagement"
            );
        }

        /// <summary>
        /// Notify admin about system backup
        /// </summary>
        public void NotifyBackupCompleted(bool success, string backupPath = null, string errorMessage = null)
        {
            if (success)
            {
                CreateNotification(
                    type: "BackupSuccess",
                    title: "Backup Completed Successfully",
                    message: $"System backup completed successfully. Location: {backupPath}",
                    targetRole: "Admin",
                    priority: "Low",
                    actionUrl: "SystemSettings"
                );
            }
            else
            {
                CreateNotification(
                    type: "BackupFailed",
                    title: "Backup Failed",
                    message: $"System backup failed. Error: {errorMessage}",
                    targetRole: "Admin",
                    priority: "Urgent",
                    actionUrl: "SystemSettings"
                );
            }
        }

        /// <summary>
        /// Notify admin about database restore
        /// </summary>
        public void NotifyRestoreCompleted(bool success, string restoreFile = null, string errorMessage = null)
        {
            if (success)
            {
                CreateNotification(
                    type: "RestoreSuccess",
                    title: "Database Restore Completed",
                    message: $"Database restored successfully from: {restoreFile}",
                    targetRole: "Admin",
                    priority: "High",
                    actionUrl: "SystemSettings"
                );
            }
            else
            {
                CreateNotification(
                    type: "RestoreFailed",
                    title: "Database Restore Failed",
                    message: $"Database restore failed. Error: {errorMessage}",
                    targetRole: "Admin",
                    priority: "Urgent",
                    actionUrl: "SystemSettings"
                );
            }
        }

        /// <summary>
        /// Notify admin about system settings changes
        /// </summary>
        public void NotifySettingsChanged(string settingCategory, string changedBy)
        {
            CreateNotification(
                type: "SettingsChanged",
                title: "System Settings Updated",
                message: $"{settingCategory} settings have been modified by {changedBy}",
                targetRole: "Admin",
                priority: "Low",
                actionUrl: "SystemSettings"
            );
        }

        /// <summary>
        /// General system alert for admin
        /// </summary>
        public void NotifySystemAlert(string title, string message, string priority = "High")
        {
            CreateNotification(
                type: "SystemAlert",
                title: title,
                message: message,
                targetRole: "Admin",
                priority: priority,
                actionUrl: "SystemSettings"
            );
        }

        #endregion

        #region Read Notifications

        /// <summary>
        /// Get unread notifications for a specific user and role
        /// </summary>
        public IEnumerable<Notification> GetUnreadNotifications(string username, string role)
        {
            return _db.Notifications
                .Where(n => !n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get all notifications for a specific user and role (with optional limit)
        /// </summary>
        public IEnumerable<Notification> GetAllNotifications(string username, string role, int? limit = null)
        {
            var query = _db.Notifications
                .Where(n => (n.TargetUser == username || n.TargetUser == "All") &&
                           (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .OrderByDescending(n => n.CreatedAt);

            if (limit.HasValue)
                return query.Take(limit.Value).ToList();

            return query.ToList();
        }

        /// <summary>
        /// Get notifications created after a specific date (for polling)
        /// </summary>
        public IEnumerable<Notification> GetNewNotificationsSince(string username, string role, DateTime since)
        {
            return _db.Notifications
                .Where(n => !n.IsRead &&
                       n.CreatedAt > since &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        public int GetUnreadCount(string username, string role)
        {
            return _db.Notifications
                .Count(n => !n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null));
        }

        /// <summary>
        /// Get a single notification by ID
        /// </summary>
        public Notification GetById(int notificationId)
        {
            return _db.Notifications.Find(notificationId);
        }

        #endregion

        #region Mark as Read

        /// <summary>
        /// Mark a single notification as read
        /// </summary>
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

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        public void MarkAllAsRead(string username, string role)
        {
            var notifications = _db.Notifications
                .Where(n => !n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
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

        /// <summary>
        /// Delete a single notification
        /// </summary>
        public void DeleteNotification(int notificationId)
        {
            var notification = _db.Notifications.Find(notificationId);
            if (notification != null)
            {
                _db.Notifications.Remove(notification);
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Delete all read notifications for a user
        /// </summary>
        public void DeleteAllRead(string username, string role)
        {
            var notifications = _db.Notifications
                .Where(n => n.IsRead &&
                       (n.TargetUser == username || n.TargetUser == "All") &&
                       (n.TargetRole == role || n.TargetRole == "All" || n.TargetRole == null))
                .ToList();

            _db.Notifications.RemoveRange(notifications);
            _db.SaveChanges();
        }

        /// <summary>
        /// Delete old notifications (cleanup)
        /// </summary>
        public void DeleteOldNotifications(int daysOld = 30)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysOld);
            var oldNotifications = _db.Notifications
                .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                .ToList();

            _db.Notifications.RemoveRange(oldNotifications);
            _db.SaveChanges();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Set icon and color based on notification type
        /// </summary>
        private void SetNotificationStyle(Notification notification)
        {
            switch (notification.Type)
            {
                // Patient notifications
                case "NewPatient":
                    notification.Icon = "AccountPlus";
                    notification.Color = "#4CAF50";
                    break;
                case "PatientUpdated":
                    notification.Icon = "AccountEdit";
                    notification.Color = "#2196F3";
                    break;

                // Appointment notifications
                case "NewAppointment":
                    notification.Icon = "CalendarPlus";
                    notification.Color = "#4CAF50";
                    break;
                case "AppointmentUpdated":
                    notification.Icon = "CalendarEdit";
                    notification.Color = "#FF9800";
                    break;
                case "AppointmentCancelled":
                    notification.Icon = "CalendarRemove";
                    notification.Color = "#F44336";
                    break;
                case "AppointmentConfirmed":
                    notification.Icon = "CalendarCheck";
                    notification.Color = "#4CAF50";
                    break;
                case "AppointmentCompleted":
                    notification.Icon = "CheckCircle";
                    notification.Color = "#4CAF50";
                    break;
                case "AppointmentReminder":
                    notification.Icon = "CalendarClock";
                    notification.Color = "#2196F3";
                    break;

                // Medicine/Inventory notifications
                case "LowStock":
                    notification.Icon = "PackageVariantClosed";
                    notification.Color = "#FF9800";
                    break;
                case "OutOfStock":
                    notification.Icon = "PackageVariantRemove";
                    notification.Color = "#F44336";
                    break;
                case "ExpiringMedicine":
                    notification.Icon = "TimerSand";
                    notification.Color = "#FF9800";
                    break;

                // Prescription notifications
                case "PrescriptionReady":
                    notification.Icon = "Pill";
                    notification.Color = "#9C27B0";
                    break;

                // User/Security notifications
                case "NewUser":
                    notification.Icon = "AccountPlus";
                    notification.Color = "#2196F3";
                    break;
                case "UserModified":
                    notification.Icon = "AccountEdit";
                    notification.Color = "#607D8B";
                    break;
                case "SecurityAlert":
                    notification.Icon = "ShieldAlert";
                    notification.Color = "#F44336";
                    break;
                case "AccountLocked":
                    notification.Icon = "AccountLock";
                    notification.Color = "#F44336";
                    break;

                // Backup/System notifications
                case "BackupSuccess":
                    notification.Icon = "DatabaseCheck";
                    notification.Color = "#4CAF50";
                    break;
                case "BackupFailed":
                    notification.Icon = "DatabaseRemove";
                    notification.Color = "#F44336";
                    break;
                case "RestoreSuccess":
                    notification.Icon = "DatabaseSync";
                    notification.Color = "#4CAF50";
                    break;
                case "RestoreFailed":
                    notification.Icon = "DatabaseAlert";
                    notification.Color = "#F44336";
                    break;
                case "SettingsChanged":
                    notification.Icon = "Cog";
                    notification.Color = "#607D8B";
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

        /// <summary>
        /// Get the sound type for a notification (for sound player)
        /// </summary>
        public string GetSoundType(Notification notification)
        {
            switch (notification.Priority)
            {
                case "Urgent":
                    return "urgent";
                case "High":
                    return "high";
                case "Normal":
                    return "normal";
                case "Low":
                    return "low";
                default:
                    return "normal";
            }
        }

        #endregion
    }
}
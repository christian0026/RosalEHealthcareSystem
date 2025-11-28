using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    /// <summary>
    /// Static class to manage showing notification toasts globally
    /// </summary>
    public static class NotificationManager
    {
        private static Panel _notificationContainer;
        private static int _maxVisibleToasts = 4;
        private static int _toastDuration = 5000; // 5 seconds

        /// <summary>
        /// Initialize the notification manager with a container panel
        /// </summary>
        public static void Initialize(Panel container)
        {
            _notificationContainer = container;

            // Initialize sound player
            NotificationSoundPlayer.Initialize();
        }

        /// <summary>
        /// Set maximum visible toasts at once
        /// </summary>
        public static void SetMaxVisibleToasts(int max)
        {
            _maxVisibleToasts = max;
        }

        /// <summary>
        /// Set toast duration
        /// </summary>
        public static void SetToastDuration(int milliseconds)
        {
            _toastDuration = milliseconds;
        }

        /// <summary>
        /// Show a notification from a Notification model object
        /// </summary>
        public static void ShowNotification(Notification notification, bool playSound = true)
        {
            ShowNotification(notification.Title, notification.Message, notification.Type, playSound);
        }

        /// <summary>
        /// Show a notification with custom parameters
        /// </summary>
        public static void ShowNotification(string title, string message, string type, bool playSound = true)
        {
            if (_notificationContainer == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Remove oldest toasts if we're at max
                while (_notificationContainer.Children.Count >= _maxVisibleToasts)
                {
                    if (_notificationContainer.Children.Count > 0)
                        _notificationContainer.Children.RemoveAt(0);
                }

                var toast = new NotificationToast();

                // Position at bottom-right, stacking upward
                toast.VerticalAlignment = VerticalAlignment.Bottom;
                toast.HorizontalAlignment = HorizontalAlignment.Right;
                toast.Margin = new Thickness(0, 0, 20, 20 + (_notificationContainer.Children.Count * 110));

                _notificationContainer.Children.Add(toast);
                toast.Show(title, message, type, _toastDuration, playSound);
            });
        }

        #region Convenience Methods

        public static void ShowSuccess(string title, string message)
        {
            ShowNotification(title, message, "success");
        }

        public static void ShowError(string title, string message)
        {
            ShowNotification(title, message, "error");
        }

        public static void ShowWarning(string title, string message)
        {
            ShowNotification(title, message, "warning");
        }

        public static void ShowInfo(string title, string message)
        {
            ShowNotification(title, message, "info");
        }

        #endregion

        #region Specific Notification Types

        public static void ShowNewPatient(string patientName, string registeredBy)
        {
            ShowNotification(
                "New Patient Registered",
                $"{patientName} has been registered by {registeredBy}",
                "NewPatient"
            );
        }

        public static void ShowNewAppointment(string patientName, DateTime appointmentTime, string scheduledBy)
        {
            ShowNotification(
                "New Appointment Scheduled",
                $"Appointment for {patientName} on {appointmentTime:MMM dd 'at' h:mm tt} - by {scheduledBy}",
                "NewAppointment"
            );
        }

        public static void ShowAppointmentCompleted(string patientName)
        {
            ShowNotification(
                "Consultation Completed",
                $"Consultation with {patientName} has been completed. Patient may proceed.",
                "AppointmentCompleted"
            );
        }

        public static void ShowAppointmentCancelled(string patientName, string cancelledBy)
        {
            ShowNotification(
                "Appointment Cancelled",
                $"Appointment for {patientName} has been cancelled by {cancelledBy}",
                "AppointmentCancelled"
            );
        }

        public static void ShowLowStock(string medicineName, int currentStock)
        {
            ShowNotification(
                "Low Stock Alert",
                $"{medicineName} is running low. Current stock: {currentStock} units",
                "LowStock"
            );
        }

        public static void ShowExpiringMedicine(string medicineName, DateTime expiryDate)
        {
            int days = (expiryDate - DateTime.Today).Days;
            ShowNotification(
                "Medicine Expiring Soon",
                $"{medicineName} will expire on {expiryDate:MMM dd, yyyy} ({days} days remaining)",
                "ExpiringMedicine"
            );
        }

        #endregion

        /// <summary>
        /// Clear all visible toasts
        /// </summary>
        public static void ClearAll()
        {
            if (_notificationContainer == null) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                _notificationContainer.Children.Clear();
            });
        }
    }
}
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class NotificationManager
    {
        private static Panel _notificationContainer;
        private static int _maxVisibleToasts = 4;
        private static int _toastDuration = 5000;

        public static void Initialize(Panel container)
        {
            _notificationContainer = container;
            NotificationSoundPlayer.Initialize();
            System.Diagnostics.Debug.WriteLine($"NotificationManager.Initialize - Container type: {container?.GetType().Name ?? "NULL"}");
        }

        public static void SetMaxVisibleToasts(int max) => _maxVisibleToasts = max;
        public static void SetToastDuration(int milliseconds) => _toastDuration = milliseconds;

        public static void ShowNotification(Notification notification, bool playSound = true)
        {
            ShowNotification(notification.Title, notification.Message, notification.Type, playSound);
        }

        public static void ShowNotification(string title, string message, string type, bool playSound = true)
        {
            System.Diagnostics.Debug.WriteLine($"=== NotificationManager.ShowNotification ===");
            System.Diagnostics.Debug.WriteLine($"Title: {title}, Type: {type}");
            System.Diagnostics.Debug.WriteLine($"Container is null: {_notificationContainer == null}");

            if (_notificationContainer == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Container is NULL - cannot show toast!");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Diagnostics.Debug.WriteLine($"Container type: {_notificationContainer.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Children BEFORE: {_notificationContainer.Children.Count}");

                // Remove oldest toasts if at max
                while (_notificationContainer.Children.Count >= _maxVisibleToasts)
                {
                    if (_notificationContainer.Children.Count > 0)
                        _notificationContainer.Children.RemoveAt(0);
                }

                var toast = new NotificationToast();

                // CRITICAL: Handle Canvas vs other Panel types differently
                if (_notificationContainer is Canvas canvas)
                {
                    // For Canvas, use Canvas attached properties
                    int stackIndex = canvas.Children.Count;
                    Canvas.SetRight(toast, 0);
                    Canvas.SetBottom(toast, stackIndex * 100); // Stack upward

                    System.Diagnostics.Debug.WriteLine($"Canvas positioning: Right=0, Bottom={stackIndex * 100}");
                }
                else
                {
                    // For Grid/StackPanel, use alignment and margin
                    toast.VerticalAlignment = VerticalAlignment.Bottom;
                    toast.HorizontalAlignment = HorizontalAlignment.Right;
                    toast.Margin = new Thickness(0, 0, 20, 20 + (_notificationContainer.Children.Count * 110));
                }

                _notificationContainer.Children.Add(toast);

                System.Diagnostics.Debug.WriteLine($"Children AFTER: {_notificationContainer.Children.Count}");
                System.Diagnostics.Debug.WriteLine($"Calling toast.Show()...");

                toast.Show(title, message, type, _toastDuration, playSound);

                System.Diagnostics.Debug.WriteLine("Toast.Show() completed");
            });
        }

        #region Convenience Methods

        public static void ShowSuccess(string title, string message) => ShowNotification(title, message, "success");
        public static void ShowError(string title, string message) => ShowNotification(title, message, "error");
        public static void ShowWarning(string title, string message) => ShowNotification(title, message, "warning");
        public static void ShowInfo(string title, string message) => ShowNotification(title, message, "info");

        #endregion

        #region Specific Notification Types

        public static void ShowNewPatient(string patientName, string registeredBy)
        {
            ShowNotification("New Patient Registered",
                $"{patientName} has been registered by {registeredBy}", "NewPatient");
        }

        public static void ShowNewAppointment(string patientName, DateTime appointmentTime, string scheduledBy)
        {
            ShowNotification("New Appointment Scheduled",
                $"Appointment for {patientName} on {appointmentTime:MMM dd 'at' h:mm tt} - by {scheduledBy}", "NewAppointment");
        }

        public static void ShowAppointmentCompleted(string patientName)
        {
            ShowNotification("Consultation Completed",
                $"Consultation with {patientName} has been completed. Patient may proceed.", "AppointmentCompleted");
        }

        public static void ShowAppointmentCancelled(string patientName, string cancelledBy)
        {
            ShowNotification("Appointment Cancelled",
                $"Appointment for {patientName} has been cancelled by {cancelledBy}", "AppointmentCancelled");
        }

        public static void ShowLowStock(string medicineName, int currentStock)
        {
            ShowNotification("Low Stock Alert",
                $"{medicineName} is running low. Current stock: {currentStock} units", "LowStock");
        }

        public static void ShowExpiringMedicine(string medicineName, DateTime expiryDate)
        {
            int days = (expiryDate - DateTime.Today).Days;
            ShowNotification("Medicine Expiring Soon",
                $"{medicineName} will expire on {expiryDate:MMM dd, yyyy} ({days} days remaining)", "ExpiringMedicine");
        }

        #endregion

        public static void ClearAll()
        {
            if (_notificationContainer == null) return;
            Application.Current.Dispatcher.Invoke(() => _notificationContainer.Children.Clear());
        }
    }
}
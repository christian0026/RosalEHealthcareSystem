using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    /// <summary>
    /// Helper class to test notifications during development
    /// </summary>
    public static class NotificationTestHelper
    {
        /// <summary>
        /// Create test notifications for all roles
        /// </summary>
        public static void CreateTestNotifications()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var service = new NotificationService(db);

                    // Test notifications for Doctor
                    service.NotifyNewPatient("Juan Dela Cruz", "PT-2024-001", "Maria Receptionist");
                    service.NotifyNewAppointment("Pedro Santos", "APT-2024-001", DateTime.Now.AddHours(2), "Maria Receptionist");
                    service.NotifyLowStock("Paracetamol 500mg", "MED-001", 15);
                    service.NotifyExpiringMedicine("Amoxicillin 250mg", "MED-002", DateTime.Now.AddDays(7));

                    // Test notifications for Receptionist
                    service.NotifyAppointmentCompleted("Juan Dela Cruz", "APT-2024-001", "Dr. Smith");
                    service.NotifyAppointmentConfirmed("Pedro Santos", "APT-2024-002", DateTime.Now.AddDays(1), "Dr. Smith");
                    service.NotifyPrescriptionReady("Maria Garcia", "RX-2024-001", "Dr. Smith");

                    // Test notifications for Admin
                    service.NotifyNewUserCreated("new_doctor", "Doctor", "Admin");
                    service.NotifyBackupCompleted(true, "C:\\Backups\\backup_2024.bak");
                    service.NotifyLoginFailedAttempts("hacker_user", 5, "192.168.1.100");
                    service.NotifySystemAlert("Database Maintenance", "Scheduled maintenance in 2 hours");

                    System.Windows.MessageBox.Show(
                        "Test notifications created successfully!\n\n" +
                        "Check the notification bell for new notifications.",
                        "Test Complete",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error creating test notifications: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clear all notifications from database
        /// </summary>
        public static void ClearAllNotifications()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    db.Database.ExecuteSqlCommand("DELETE FROM Notifications");

                    System.Windows.MessageBox.Show(
                        "All notifications cleared!",
                        "Success",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Error clearing notifications: {ex.Message}",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Simulate a real-time notification (for testing polling)
        /// </summary>
        public static void SimulateNewPatientNotification()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var service = new NotificationService(db);
                    string patientName = $"Test Patient {DateTime.Now:HHmmss}";
                    service.NotifyNewPatient(patientName, $"PT-TEST-{DateTime.Now:HHmmss}", "Test Receptionist");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error simulating notification: {ex.Message}");
            }
        }
    }
}
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    /// <summary>
    /// Handles polling for new notifications in the background
    /// </summary>
    public class NotificationPollingService
    {
        private DispatcherTimer _pollingTimer;
        private DateTime _lastCheckTime;
        private string _currentUsername;
        private string _currentRole;
        private bool _isFirstCheck = true;

        // Events
        public event Action<IEnumerable<Notification>> OnNewNotifications;
        public event Action<int> OnUnreadCountChanged;

        // Settings
        public int PollingIntervalSeconds { get; set; } = 15; // Check every 15 seconds
        public bool ShowToastOnLogin { get; set; } = false; // Don't show toast on first login

        /// <summary>
        /// Start polling for notifications
        /// </summary>
        public void Start(string username, string role)
        {
            _currentUsername = username;
            _currentRole = role;
            _lastCheckTime = DateTime.Now;
            _isFirstCheck = true;

            Stop(); // Stop any existing timer

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(PollingIntervalSeconds)
            };
            _pollingTimer.Tick += PollingTimer_Tick;
            _pollingTimer.Start();

            // Initial check for unread count (but don't show toasts)
            CheckInitialNotifications();
        }

        /// <summary>
        /// Stop polling
        /// </summary>
        public void Stop()
        {
            if (_pollingTimer != null)
            {
                _pollingTimer.Stop();
                _pollingTimer.Tick -= PollingTimer_Tick;
                _pollingTimer = null;
            }
        }

        /// <summary>
        /// Change polling interval
        /// </summary>
        public void SetPollingInterval(int seconds)
        {
            PollingIntervalSeconds = seconds;
            if (_pollingTimer != null)
            {
                _pollingTimer.Interval = TimeSpan.FromSeconds(seconds);
            }
        }

        /// <summary>
        /// Force an immediate check
        /// </summary>
        public void CheckNow()
        {
            PollingTimer_Tick(null, null);
        }

        /// <summary>
        /// Initial check on login - only updates count, no toasts
        /// </summary>
        private void CheckInitialNotifications()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    int unreadCount = notificationService.GetUnreadCount(_currentUsername, _currentRole);
                    OnUnreadCountChanged?.Invoke(unreadCount);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking initial notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Polling timer tick - check for new notifications
        /// </summary>
        private void PollingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);

                    // Get new notifications since last check
                    var newNotifications = notificationService
                        .GetNewNotificationsSince(_currentUsername, _currentRole, _lastCheckTime)
                        .ToList();

                    // Update last check time
                    _lastCheckTime = DateTime.Now;

                    // Get current unread count
                    int unreadCount = notificationService.GetUnreadCount(_currentUsername, _currentRole);
                    OnUnreadCountChanged?.Invoke(unreadCount);

                    // If there are new notifications and it's not the first check, notify
                    if (newNotifications.Any())
                    {
                        if (_isFirstCheck)
                        {
                            // First check after login - don't show toasts, just update badge
                            _isFirstCheck = false;
                        }
                        else
                        {
                            // Subsequent checks - show toasts for new notifications
                            OnNewNotifications?.Invoke(newNotifications);
                        }
                    }
                    else if (_isFirstCheck)
                    {
                        _isFirstCheck = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error polling notifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all unread notifications (for popup display)
        /// </summary>
        public IEnumerable<Notification> GetUnreadNotifications()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    return notificationService.GetUnreadNotifications(_currentUsername, _currentRole).ToList();
                }
            }
            catch
            {
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Get all notifications with optional limit (for popup display)
        /// </summary>
        public IEnumerable<Notification> GetAllNotifications(int limit = 20)
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    return notificationService.GetAllNotifications(_currentUsername, _currentRole, limit).ToList();
                }
            }
            catch
            {
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Mark notification as read
        /// </summary>
        public void MarkAsRead(int notificationId)
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    notificationService.MarkAsRead(notificationId);

                    // Update unread count
                    int unreadCount = notificationService.GetUnreadCount(_currentUsername, _currentRole);
                    OnUnreadCountChanged?.Invoke(unreadCount);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Mark all notifications as read
        /// </summary>
        public void MarkAllAsRead()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    notificationService.MarkAllAsRead(_currentUsername, _currentRole);
                    OnUnreadCountChanged?.Invoke(0);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking all as read: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a notification
        /// </summary>
        public void DeleteNotification(int notificationId)
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);
                    notificationService.DeleteNotification(notificationId);

                    // Update unread count
                    int unreadCount = notificationService.GetUnreadCount(_currentUsername, _currentRole);
                    OnUnreadCountChanged?.Invoke(unreadCount);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting notification: {ex.Message}");
            }
        }
    }
}
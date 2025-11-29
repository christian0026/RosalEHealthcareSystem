using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationPopup : UserControl
    {
        private string _currentUsername;
        private string _currentRole;
        private bool _showUnreadOnly = false;
        private List<Notification> _allNotifications = new List<Notification>();

        // Events
        public event Action OnCloseRequested;
        public event Action<string> OnNavigateRequested; // ActionUrl
        public event Action OnNotificationsChanged;

        public NotificationPopup()
        {
            InitializeComponent();
        }

        #region Public Methods

        /// <summary>
        /// Initialize the popup with user info
        /// </summary>
        public void Initialize(string username, string role)
        {
            _currentUsername = username;
            _currentRole = role;
            LoadNotifications();
        }

        /// <summary>
        /// Refresh the notification list
        /// </summary>
        public void Refresh()
        {
            LoadNotifications();
        }

        /// <summary>
        /// Update unread count display
        /// </summary>
        public void UpdateUnreadCount(int count)
        {
            if (count > 0)
            {
                HeaderBadge.Visibility = Visibility.Visible;
                HeaderBadgeCount.Text = count > 99 ? "99+" : count.ToString();

                UnreadTabBadge.Visibility = Visibility.Visible;
                UnreadTabCount.Text = count > 99 ? "99+" : count.ToString();
            }
            else
            {
                HeaderBadge.Visibility = Visibility.Collapsed;
                UnreadTabBadge.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Load Data

        private void LoadNotifications()
        {
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var notificationService = new NotificationService(db);

                    // Get all notifications (limit to 50 for performance)
                    _allNotifications = notificationService
                        .GetAllNotifications(_currentUsername, _currentRole, 50)
                        .ToList();

                    // Update unread count
                    int unreadCount = _allNotifications.Count(n => !n.IsRead);
                    UpdateUnreadCount(unreadCount);

                    // Apply filter and display
                    ApplyFilter();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
                ShowEmptyState("Error loading notifications", "Please try again later.");
            }
        }

        private void ApplyFilter()
        {
            IEnumerable<Notification> displayList;

            if (_showUnreadOnly)
            {
                displayList = _allNotifications.Where(n => !n.IsRead);
                if (!displayList.Any())
                {
                    ShowEmptyState("No unread notifications", "You're all caught up!");
                    return;
                }
            }
            else
            {
                displayList = _allNotifications;
                if (!displayList.Any())
                {
                    ShowEmptyState("No notifications", "You don't have any notifications yet.");
                    return;
                }
            }

            EmptyState.Visibility = Visibility.Collapsed;
            NotificationsList.ItemsSource = displayList.ToList();
        }

        private void ShowEmptyState(string title, string message)
        {
            EmptyStateTitle.Text = title;
            EmptyStateMessage.Text = message;
            EmptyState.Visibility = Visibility.Visible;
            NotificationsList.ItemsSource = null;
        }

        #endregion

        #region Tab Switching

        private void BtnTabAll_Click(object sender, RoutedEventArgs e)
        {
            _showUnreadOnly = false;
            SetActiveTab(BtnTabAll, BtnTabUnread);
            ApplyFilter();
        }

        private void BtnTabUnread_Click(object sender, RoutedEventArgs e)
        {
            _showUnreadOnly = true;
            SetActiveTab(BtnTabUnread, BtnTabAll);
            ApplyFilter();
        }

        private void SetActiveTab(Button activeBtn, Button inactiveBtn)
        {
            activeBtn.Style = (Style)FindResource("TabButtonActive");
            inactiveBtn.Style = (Style)FindResource("TabButton");
        }

        #endregion

        #region Notification Actions

        private void NotificationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int notificationId)
            {
                try
                {
                    // Find the notification
                    var notification = _allNotifications.FirstOrDefault(n => n.Id == notificationId);
                    if (notification == null) return;

                    // Mark as read
                    if (!notification.IsRead)
                    {
                        using (var db = new RosalEHealthcareDbContext())
                        {
                            var notificationService = new NotificationService(db);
                            notificationService.MarkAsRead(notificationId);
                        }

                        notification.IsRead = true;
                        int unreadCount = _allNotifications.Count(n => !n.IsRead);
                        UpdateUnreadCount(unreadCount);
                        OnNotificationsChanged?.Invoke();

                        // Refresh display
                        ApplyFilter();
                    }

                    // Navigate if there's an action URL
                    if (!string.IsNullOrEmpty(notification.ActionUrl))
                    {
                        OnNavigateRequested?.Invoke(notification.ActionUrl);
                        OnCloseRequested?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling notification click: {ex.Message}");
                }
            }
        }

        private void BtnDeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent triggering parent button click

            if (sender is Button btn && btn.Tag is int notificationId)
            {
                try
                {
                    using (var db = new RosalEHealthcareDbContext())
                    {
                        var notificationService = new NotificationService(db);
                        notificationService.DeleteNotification(notificationId);
                    }

                    // Remove from local list and refresh
                    _allNotifications.RemoveAll(n => n.Id == notificationId);
                    int unreadCount = _allNotifications.Count(n => !n.IsRead);
                    UpdateUnreadCount(unreadCount);
                    OnNotificationsChanged?.Invoke();

                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error deleting notification: {ex.Message}");
                }
            }
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Mark all notifications as read?",
                "Confirm",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new RosalEHealthcareDbContext())
                    {
                        var notificationService = new NotificationService(db);
                        notificationService.MarkAllAsRead(_currentUsername, _currentRole);
                    }

                    // Update local list
                    foreach (var notification in _allNotifications)
                    {
                        notification.IsRead = true;
                        notification.ReadAt = DateTime.Now;
                    }

                    UpdateUnreadCount(0);
                    OnNotificationsChanged?.Invoke();
                    ApplyFilter();

                    // Show feedback
                    MessageBox.Show("All notifications marked as read.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error marking notifications as read: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            int readCount = _allNotifications.Count(n => n.IsRead);

            if (readCount == 0)
            {
                MessageBox.Show("No read notifications to clear.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Delete {readCount} read notification(s)?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new RosalEHealthcareDbContext())
                    {
                        var notificationService = new NotificationService(db);
                        notificationService.DeleteAllRead(_currentUsername, _currentRole);
                    }

                    // Remove from local list
                    _allNotifications.RemoveAll(n => n.IsRead);
                    OnNotificationsChanged?.Invoke();
                    ApplyFilter();

                    MessageBox.Show("Read notifications cleared.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing notifications: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            OnCloseRequested?.Invoke();
        }

        #endregion
    }
}
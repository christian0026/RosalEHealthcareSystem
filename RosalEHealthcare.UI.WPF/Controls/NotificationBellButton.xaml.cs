using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Helpers;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationBellButton : UserControl
    {
        #region Fields

        private NotificationPollingService _pollingService;
        private NotificationPopup _popupControl;

        private string _username;
        private string _role;
        private Panel _toastContainer;
        private bool _isInitialized = false;

        #endregion

        #region Events

        public event Action<string> OnNavigateRequested;

        #endregion

        #region Constructor

        public NotificationBellButton()
        {
            InitializeComponent();
            UpdateBadge(0);
        }

        #endregion

        #region Public Methods

        public void Initialize(string username, string role, Panel toastContainer)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== NotificationBellButton.Initialize START ===");
                System.Diagnostics.Debug.WriteLine($"Username: {username}, Role: {role}");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role))
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Username or Role is null/empty!");
                    return;
                }

                _username = username;
                _role = role;
                _toastContainer = toastContainer;

                // Initialize NotificationManager for toasts
                if (_toastContainer != null)
                {
                    NotificationManager.Initialize(_toastContainer);
                    System.Diagnostics.Debug.WriteLine("NotificationManager initialized");
                }

                // Create and setup popup control
                _popupControl = new NotificationPopup();
                _popupControl.OnNotificationClicked += HandleNotificationClicked;
                _popupControl.OnMarkAllRead += HandleMarkAllRead;
                _popupControl.OnClearAll += HandleClearAll;
                _popupControl.OnDeleteNotification += HandleDeleteNotification;
                _popupControl.OnCloseRequested += () => NotificationPopup.IsOpen = false;
                PopupContent.Content = _popupControl;

                // Create and start polling service
                _pollingService = new NotificationPollingService();
                _pollingService.OnNewNotifications += HandleNewNotifications;
                _pollingService.OnUnreadCountChanged += UpdateBadge;
                _pollingService.Start(_username, _role);

                _isInitialized = true;
                System.Diagnostics.Debug.WriteLine("=== NotificationBellButton.Initialize COMPLETE ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in Initialize: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
            }
        }

        public void Stop()
        {
            _pollingService?.Stop();
            System.Diagnostics.Debug.WriteLine("NotificationBellButton stopped");
        }

        public void RefreshNotifications()
        {
            _pollingService?.CheckNow();
        }

        #endregion

        #region Private Methods

        private void UpdateBadge(int count)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NotificationCount.Text = count > 99 ? "99+" : count.ToString();
                NotificationBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
                _popupControl?.UpdateBadgeCounts(count);
            });
        }

        private void LoadNotificationsToPopup()
        {
            if (_pollingService == null || _popupControl == null) return;

            var notifications = _pollingService.GetAllNotifications(50);
            _popupControl.LoadNotifications(notifications);
        }

        private void HandleNewNotifications(IEnumerable<Notification> notifications)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var notification in notifications)
                    {
                        System.Diagnostics.Debug.WriteLine($"New notification: {notification.Title}");

                        // Show toast using existing NotificationManager
                        NotificationManager.ShowNotification(notification);

                        // Play pulse animation
                        PlayPulseAnimation();
                    }

                    // Refresh popup if open
                    if (NotificationPopup.IsOpen)
                    {
                        LoadNotificationsToPopup();
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling notifications: {ex.Message}");
            }
        }

        private void PlayPulseAnimation()
        {
            try
            {
                // The pulse animation is triggered by the XAML EventTrigger
                // We can restart it by toggling visibility or using storyboard
                PulseIndicator.Opacity = 1;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;

            // Toggle popup
            NotificationPopup.IsOpen = !NotificationPopup.IsOpen;

            if (NotificationPopup.IsOpen)
            {
                LoadNotificationsToPopup();
            }
        }

        private void HandleNotificationClicked(Notification notification)
        {
            // Mark as read
            _pollingService?.MarkAsRead(notification.Id);

            // Refresh popup
            LoadNotificationsToPopup();

            // Close popup
            NotificationPopup.IsOpen = false;

            // Navigate if action URL exists
            if (!string.IsNullOrEmpty(notification.ActionUrl))
            {
                OnNavigateRequested?.Invoke(notification.ActionUrl);
            }
        }

        private void HandleMarkAllRead()
        {
            _pollingService?.MarkAllAsRead();
            LoadNotificationsToPopup();
        }

        private void HandleClearAll()
        {
            // Delete all read notifications
            var notifications = _pollingService?.GetAllNotifications(100);
            if (notifications != null)
            {
                foreach (var n in notifications.Where(x => x.IsRead))
                {
                    _pollingService?.DeleteNotification(n.Id);
                }
            }
            LoadNotificationsToPopup();
        }

        private void HandleDeleteNotification(int notificationId)
        {
            _pollingService?.DeleteNotification(notificationId);
            LoadNotificationsToPopup();
        }

        #endregion
    }
}
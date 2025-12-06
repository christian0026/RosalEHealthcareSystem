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
        private NotificationPollingService _pollingService;
        private NotificationPopup _popupControl;

        private string _username;
        private string _role;
        private Panel _toastContainer;
        private bool _isInitialized = false;

        public event Action<string> OnNavigateRequested;

        public NotificationBellButton()
        {
            InitializeComponent();
            UpdateBadge(0);
        }

        public void Initialize(string username, string role, Panel toastContainer)
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(role)) return;

                _username = username;
                _role = role;
                _toastContainer = toastContainer;

                if (_toastContainer != null)
                {
                    NotificationManager.Initialize(_toastContainer);
                }

                _popupControl = new NotificationPopup();
                _popupControl.OnNotificationClicked += HandleNotificationClicked;
                _popupControl.OnMarkAllRead += HandleMarkAllRead;
                _popupControl.OnClearAll += HandleClearAll;
                _popupControl.OnDeleteNotification += HandleDeleteNotification;
                _popupControl.OnCloseRequested += () => NotificationPopup.IsOpen = false;
                PopupContent.Content = _popupControl;

                _pollingService = new NotificationPollingService();
                _pollingService.OnNewNotifications += HandleNewNotifications;
                _pollingService.OnUnreadCountChanged += UpdateBadge;
                _pollingService.Start(_username, _role);

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"NotificationBell Initialize Error: {ex.Message}");
            }
        }

        public void Stop()
        {
            _pollingService?.Stop();
        }

        public void RefreshNotifications()
        {
            _pollingService?.CheckNow();
        }

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
                        NotificationManager.ShowNotification(notification);
                        PlayPulseAnimation();
                    }

                    if (NotificationPopup.IsOpen)
                    {
                        LoadNotificationsToPopup();
                    }
                });
            }
            catch { }
        }

        private void PlayPulseAnimation()
        {
            try
            {
                PulseIndicator.Opacity = 1;
            }
            catch { }
        }

        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized) return;
            NotificationPopup.IsOpen = !NotificationPopup.IsOpen;
            if (NotificationPopup.IsOpen)
            {
                LoadNotificationsToPopup();
            }
        }

        private void HandleNotificationClicked(Notification notification)
        {
            _pollingService?.MarkAsRead(notification.Id);
            LoadNotificationsToPopup();
            NotificationPopup.IsOpen = false;

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
    }
}
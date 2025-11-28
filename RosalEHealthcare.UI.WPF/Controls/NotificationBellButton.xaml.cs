using RosalEHealthcare.Core.Models;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationBellButton : UserControl
    {
        private NotificationPollingService _pollingService;
        private NotificationPopup _notificationPopup;
        private string _currentUsername;
        private string _currentRole;
        private bool _isInitialized = false;

        // Event for navigation requests from popup
        public event Action<string> OnNavigateRequested;

        public NotificationBellButton()
        {
            InitializeComponent();
        }

        #region Public Methods

        /// <summary>
        /// Initialize the notification bell with user info and start polling
        /// </summary>
        public void Initialize(string username, string role, Panel toastContainer)
        {
            if (_isInitialized) return;

            _currentUsername = username;
            _currentRole = role;

            // Initialize notification manager for toasts
            NotificationManager.Initialize(toastContainer);

            // Initialize sound player
            NotificationSoundPlayer.Initialize();

            // Create polling service
            _pollingService = new NotificationPollingService();
            _pollingService.PollingIntervalSeconds = 15; // Check every 15 seconds
            _pollingService.OnNewNotifications += HandleNewNotifications;
            _pollingService.OnUnreadCountChanged += UpdateBadgeCount;

            // Create popup
            _notificationPopup = new NotificationPopup();
            _notificationPopup.Initialize(username, role);
            _notificationPopup.OnCloseRequested += () => NotificationPopup.IsOpen = false;
            _notificationPopup.OnNavigateRequested += (url) => OnNavigateRequested?.Invoke(url);
            _notificationPopup.OnNotificationsChanged += () => _pollingService.CheckNow();

            PopupContent.Content = _notificationPopup;

            // Start polling
            _pollingService.Start(username, role);

            _isInitialized = true;
        }

        /// <summary>
        /// Stop the notification service (call on logout/close)
        /// </summary>
        public void Stop()
        {
            _pollingService?.Stop();
            _isInitialized = false;
        }

        /// <summary>
        /// Force refresh notifications
        /// </summary>
        public void Refresh()
        {
            _pollingService?.CheckNow();
            _notificationPopup?.Refresh();
        }

        /// <summary>
        /// Update badge count externally
        /// </summary>
        public void SetBadgeCount(int count)
        {
            UpdateBadgeCount(count);
        }

        #endregion

        #region Event Handlers

        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            if (NotificationPopup.IsOpen)
            {
                NotificationPopup.IsOpen = false;
            }
            else
            {
                _notificationPopup?.Refresh();
                NotificationPopup.IsOpen = true;
            }
        }

        private void HandleNewNotifications(IEnumerable<Notification> newNotifications)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var notification in newNotifications)
                {
                    // Show toast notification
                    NotificationManager.ShowNotification(notification);
                }

                // Play pulse animation
                PlayPulseAnimation();

                // Refresh popup if open
                if (NotificationPopup.IsOpen)
                {
                    _notificationPopup?.Refresh();
                }
            });
        }

        private void UpdateBadgeCount(int count)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (count > 0)
                {
                    NotificationBadge.Visibility = Visibility.Visible;
                    NotificationCount.Text = count > 99 ? "99+" : count.ToString();

                    // Update popup badge too
                    _notificationPopup?.UpdateUnreadCount(count);
                }
                else
                {
                    NotificationBadge.Visibility = Visibility.Collapsed;
                    _notificationPopup?.UpdateUnreadCount(0);
                }
            });
        }

        private void PlayPulseAnimation()
        {
            try
            {
                var storyboard = PulseIndicator.FindResource("PulseAnimation") as Storyboard;
                if (storyboard != null)
                {
                    storyboard.Begin(PulseIndicator);
                }
            }
            catch
            {
                // Animation not critical, ignore errors
            }
        }

        #endregion
    }
}
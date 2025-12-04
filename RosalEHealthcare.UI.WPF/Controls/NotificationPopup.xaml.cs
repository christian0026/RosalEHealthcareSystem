using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationPopup : UserControl
    {
        #region Events

        public event Action<Notification> OnNotificationClicked;
        public event Action OnMarkAllRead;
        public event Action OnClearAll;
        public event Action<int> OnDeleteNotification;
        public event Action OnCloseRequested;

        #endregion

        #region Fields

        private List<Notification> _allNotifications = new List<Notification>();
        private bool _showUnreadOnly = false;

        #endregion

        #region Constructor

        public NotificationPopup()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        public void LoadNotifications(IEnumerable<Notification> notifications)
        {
            _allNotifications = notifications?.ToList() ?? new List<Notification>();
            RefreshList();
        }

        public void UpdateBadgeCounts(int unreadCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Header badge
                HeaderBadgeCount.Text = unreadCount.ToString();
                HeaderBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;

                // Unread tab badge
                UnreadTabCount.Text = unreadCount.ToString();
                UnreadTabBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        #endregion

        #region Private Methods

        private void RefreshList()
        {
            var displayList = _showUnreadOnly
                ? _allNotifications.Where(n => !n.IsRead).ToList()
                : _allNotifications;

            NotificationsList.ItemsSource = null;
            NotificationsList.ItemsSource = displayList;

            // Show/hide empty state
            bool isEmpty = !displayList.Any();
            EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;

            // Update empty state message
            if (isEmpty)
            {
                if (_showUnreadOnly)
                {
                    EmptyStateTitle.Text = "No unread notifications";
                    EmptyStateMessage.Text = "You've read all your notifications!";
                }
                else
                {
                    EmptyStateTitle.Text = "No notifications";
                    EmptyStateMessage.Text = "You're all caught up!";
                }
            }

            // Update badge counts
            int unreadCount = _allNotifications.Count(n => !n.IsRead);
            UpdateBadgeCounts(unreadCount);
        }

        private void SetActiveTab(bool isUnreadTab)
        {
            _showUnreadOnly = isUnreadTab;

            // Update tab styles
            if (isUnreadTab)
            {
                BtnTabAll.Style = (Style)FindResource("TabButton");
                BtnTabUnread.Style = (Style)FindResource("TabButtonActive");
            }
            else
            {
                BtnTabAll.Style = (Style)FindResource("TabButtonActive");
                BtnTabUnread.Style = (Style)FindResource("TabButton");
            }

            RefreshList();
        }

        #endregion

        #region Event Handlers

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            OnCloseRequested?.Invoke();
        }

        private void BtnTabAll_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(false);
        }

        private void BtnTabUnread_Click(object sender, RoutedEventArgs e)
        {
            SetActiveTab(true);
        }

        private void NotificationItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                int id = Convert.ToInt32(button.Tag);
                var notification = _allNotifications.FirstOrDefault(n => n.Id == id);
                if (notification != null)
                {
                    OnNotificationClicked?.Invoke(notification);
                }
            }
        }

        private void BtnDeleteNotification_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent triggering item click

            if (sender is Button button && button.Tag != null)
            {
                int id = Convert.ToInt32(button.Tag);
                OnDeleteNotification?.Invoke(id);

                // Remove from local list and refresh
                _allNotifications.RemoveAll(n => n.Id == id);
                RefreshList();
            }
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            OnMarkAllRead?.Invoke();

            // Update local list
            foreach (var notification in _allNotifications)
            {
                notification.IsRead = true;
            }
            RefreshList();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            OnClearAll?.Invoke();

            // Remove read notifications from local list
            _allNotifications.RemoveAll(n => n.IsRead);
            RefreshList();
        }

        #endregion
    }
}
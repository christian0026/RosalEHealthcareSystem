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
        public event Action<Notification> OnNotificationClicked;
        public event Action OnMarkAllRead;
        public event Action OnClearAll;
        public event Action<int> OnDeleteNotification;
        public event Action OnCloseRequested;

        private List<Notification> _allNotifications = new List<Notification>();
        private bool _showUnreadOnly = false;

        public NotificationPopup()
        {
            InitializeComponent();
        }

        public void LoadNotifications(IEnumerable<Notification> notifications)
        {
            _allNotifications = notifications?.ToList() ?? new List<Notification>();
            RefreshList();
        }

        public void UpdateBadgeCounts(int unreadCount)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                HeaderBadgeCount.Text = unreadCount.ToString();
                HeaderBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
                UnreadTabCount.Text = unreadCount.ToString();
                UnreadTabBadge.Visibility = unreadCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void RefreshList()
        {
            var displayList = _showUnreadOnly
                ? _allNotifications.Where(n => !n.IsRead).ToList()
                : _allNotifications;

            NotificationsList.ItemsSource = null;
            NotificationsList.ItemsSource = displayList;

            bool isEmpty = !displayList.Any();
            EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;

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

            int unreadCount = _allNotifications.Count(n => !n.IsRead);
            UpdateBadgeCounts(unreadCount);
        }

        private void SetActiveTab(bool isUnreadTab)
        {
            _showUnreadOnly = isUnreadTab;

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
            e.Handled = true;
            if (sender is Button button && button.Tag != null)
            {
                int id = Convert.ToInt32(button.Tag);
                OnDeleteNotification?.Invoke(id);
                _allNotifications.RemoveAll(n => n.Id == id);
                RefreshList();
            }
        }

        private void BtnMarkAllRead_Click(object sender, RoutedEventArgs e)
        {
            OnMarkAllRead?.Invoke();
            foreach (var notification in _allNotifications)
            {
                notification.IsRead = true;
            }
            RefreshList();
        }

        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            OnClearAll?.Invoke();
            _allNotifications.RemoveAll(n => n.IsRead);
            RefreshList();
        }
    }
}
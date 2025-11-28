using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;
using RosalEHealthcare.UI.WPF.Helpers;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationToast : UserControl
    {
        private DispatcherTimer _timer;
        private string _notificationType;

        public NotificationToast()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Show the notification toast
        /// </summary>
        /// <param name="title">Notification title</param>
        /// <param name="message">Notification message</param>
        /// <param name="type">Type of notification (determines icon and color)</param>
        /// <param name="duration">How long to show (in milliseconds)</param>
        /// <param name="playSound">Whether to play notification sound</param>
        public void Show(string title, string message, string type = "success", int duration = 5000, bool playSound = true)
        {
            TitleText.Text = title;
            MessageText.Text = message;
            _notificationType = type;

            // Set icon and color based on type
            SetStyleForType(type);

            // Play sound
            if (playSound)
            {
                NotificationSoundPlayer.PlaySoundForType(type);
            }

            // Slide in animation
            var slideAnimation = new ThicknessAnimation
            {
                From = new Thickness(400, 0, -400, 0),
                To = new Thickness(0),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            BeginAnimation(MarginProperty, slideAnimation);
            BeginAnimation(OpacityProperty, fadeAnimation);

            // Auto-hide after duration
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(duration)
            };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Hide();
            };
            _timer.Start();
        }

        /// <summary>
        /// Set icon and colors based on notification type
        /// </summary>
        private void SetStyleForType(string type)
        {
            switch (type.ToLower())
            {
                // Success types
                case "success":
                case "newpatient":
                case "appointmentconfirmed":
                case "appointmentcompleted":
                case "backupsuccess":
                case "restoresuccess":
                    IconElement.Kind = PackIconKind.CheckCircle;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    break;

                // Error/Danger types
                case "error":
                case "appointmentcancelled":
                case "outofstock":
                case "backupfailed":
                case "restorefailed":
                case "securityalert":
                case "accountlocked":
                    IconElement.Kind = PackIconKind.AlertCircle;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    break;

                // Warning types
                case "warning":
                case "lowstock":
                case "expiringmedicine":
                case "appointmentupdated":
                    IconElement.Kind = PackIconKind.Alert;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    break;

                // Info types
                case "info":
                case "newappointment":
                case "appointmentreminder":
                case "patientupdated":
                case "newuser":
                case "usermodified":
                case "settingschanged":
                    IconElement.Kind = PackIconKind.Information;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    break;

                // Prescription
                case "prescriptionready":
                    IconElement.Kind = PackIconKind.Pill;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0"));
                    break;

                // Default
                default:
                    IconElement.Kind = PackIconKind.Bell;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#607D8B"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#607D8B"));
                    break;
            }
        }

        /// <summary>
        /// Hide the toast with animation
        /// </summary>
        private void Hide()
        {
            var slideAnimation = new ThicknessAnimation
            {
                From = new Thickness(0),
                To = new Thickness(400, 0, -400, 0),
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var fadeAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            slideAnimation.Completed += (s, e) =>
            {
                var parent = Parent as Panel;
                parent?.Children.Remove(this);
            };

            BeginAnimation(MarginProperty, slideAnimation);
            BeginAnimation(OpacityProperty, fadeAnimation);
        }

        /// <summary>
        /// Close button click handler
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Hide();
        }
    }
}
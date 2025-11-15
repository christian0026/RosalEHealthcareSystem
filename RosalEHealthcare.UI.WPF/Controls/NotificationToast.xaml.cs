using System;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using MaterialDesignThemes.Wpf;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationToast : UserControl
    {
        private DispatcherTimer _timer;

        public NotificationToast()
        {
            InitializeComponent();
        }

        public void Show(string title, string message, string type = "success", int duration = 5000)
        {
            TitleText.Text = title;
            MessageText.Text = message;

            // Set icon and color based on type
            switch (type.ToLower())
            {
                case "success":
                    IconElement.Kind = PackIconKind.Check;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    break;
                case "error":
                    IconElement.Kind = PackIconKind.AlertCircle;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336"));
                    break;
                case "warning":
                    IconElement.Kind = PackIconKind.Alert;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800"));
                    break;
                case "info":
                    IconElement.Kind = PackIconKind.Information;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3"));
                    break;
                case "newpatient":
                    IconElement.Kind = PackIconKind.AccountPlus;
                    IconElement.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    ToastBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50"));
                    break;
            }

            // Play sound
            try
            {
                SystemSounds.Asterisk.Play();
            }
            catch { }

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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _timer?.Stop();
            Hide();
        }
    }
}

// ===========================================================================
// NotificationManager - Manages toast notifications globally
// ===========================================================================

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class NotificationManager
    {
        private static Panel _notificationContainer;

        public static void Initialize(Panel container)
        {
            _notificationContainer = container;
        }

        public static void ShowSuccess(string title, string message, int duration = 5000)
        {
            ShowNotification(title, message, "success", duration);
        }

        public static void ShowError(string title, string message, int duration = 5000)
        {
            ShowNotification(title, message, "error", duration);
        }

        public static void ShowWarning(string title, string message, int duration = 5000)
        {
            ShowNotification(title, message, "warning", duration);
        }

        public static void ShowInfo(string title, string message, int duration = 5000)
        {
            ShowNotification(title, message, "info", duration);
        }

        public static void ShowNewPatient(string patientName, string receptionist)
        {
            ShowNotification(
                "New Patient Registered",
                $"{patientName} has been registered by {receptionist}",
                "newpatient",
                6000
            );
        }

        private static void ShowNotification(string title, string message, string type, int duration)
        {
            if (_notificationContainer == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new Controls.NotificationToast();

                // Position at bottom-right
                toast.VerticalAlignment = VerticalAlignment.Bottom;
                toast.HorizontalAlignment = HorizontalAlignment.Right;
                toast.Margin = new Thickness(0, 0, 20, 20 + (_notificationContainer.Children.Count * 110));

                _notificationContainer.Children.Add(toast);
                toast.Show(title, message, type, duration);
            });
        }
    }
}
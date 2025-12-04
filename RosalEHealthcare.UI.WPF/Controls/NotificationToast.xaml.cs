using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MaterialDesignThemes.Wpf;
using RosalEHealthcare.UI.WPF.Helpers;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationToast : UserControl
    {
        #region Fields

        private System.Windows.Threading.DispatcherTimer _autoCloseTimer;
        private bool _isClosing = false;

        #endregion

        #region Constructor

        public NotificationToast()
        {
            InitializeComponent();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the toast notification with auto-close
        /// </summary>
        public void Show(string title, string message, string type, int duration = 5000, bool playSound = true)
        {
            TitleText.Text = title ?? "Notification";
            MessageText.Text = message ?? "";

            SetTypeStyle(type);

            // Play sound
            if (playSound)
            {
                NotificationSoundPlayer.PlaySoundForType(type);
            }

            // Slide in
            SlideIn();

            // Auto close
            StartAutoCloseTimer(duration);
        }

        /// <summary>
        /// Close the toast
        /// </summary>
        public void Close()
        {
            if (_isClosing) return;
            _isClosing = true;

            _autoCloseTimer?.Stop();
            SlideOut(() =>
            {
                // Remove from parent
                if (this.Parent is Panel panel)
                {
                    panel.Children.Remove(this);
                }
            });
        }

        #endregion

        #region Private Methods

        private void SetTypeStyle(string type)
        {
            Color color;
            PackIconKind icon;

            switch (type?.ToLower())
            {
                case "newpatient":
                    color = Color.FromRgb(76, 175, 80); // Green
                    icon = PackIconKind.AccountPlus;
                    break;
                case "patientupdated":
                    color = Color.FromRgb(33, 150, 243); // Blue
                    icon = PackIconKind.AccountEdit;
                    break;
                case "newappointment":
                    color = Color.FromRgb(76, 175, 80);
                    icon = PackIconKind.CalendarPlus;
                    break;
                case "appointmentupdated":
                    color = Color.FromRgb(255, 152, 0); // Orange
                    icon = PackIconKind.CalendarEdit;
                    break;
                case "appointmentcancelled":
                    color = Color.FromRgb(244, 67, 54); // Red
                    icon = PackIconKind.CalendarRemove;
                    break;
                case "appointmentconfirmed":
                    color = Color.FromRgb(76, 175, 80);
                    icon = PackIconKind.CalendarCheck;
                    break;
                case "appointmentcompleted":
                    color = Color.FromRgb(76, 175, 80);
                    icon = PackIconKind.CheckCircle;
                    break;
                case "appointmentreminder":
                    color = Color.FromRgb(33, 150, 243);
                    icon = PackIconKind.CalendarClock;
                    break;
                case "lowstock":
                    color = Color.FromRgb(255, 152, 0);
                    icon = PackIconKind.PackageVariant;
                    break;
                case "outofstock":
                    color = Color.FromRgb(244, 67, 54);
                    icon = PackIconKind.PackageVariantRemove;
                    break;
                case "expiringmedicine":
                    color = Color.FromRgb(255, 152, 0);
                    icon = PackIconKind.TimerSand;
                    break;
                case "prescriptionready":
                    color = Color.FromRgb(156, 39, 176); // Purple
                    icon = PackIconKind.Pill;
                    break;
                case "newuser":
                    color = Color.FromRgb(33, 150, 243);
                    icon = PackIconKind.AccountPlus;
                    break;
                case "securityalert":
                    color = Color.FromRgb(244, 67, 54);
                    icon = PackIconKind.ShieldAlert;
                    break;
                case "accountlocked":
                    color = Color.FromRgb(244, 67, 54);
                    icon = PackIconKind.AccountLock;
                    break;
                case "backupsuccess":
                    color = Color.FromRgb(76, 175, 80);
                    icon = PackIconKind.DatabaseCheck;
                    break;
                case "backupfailed":
                    color = Color.FromRgb(244, 67, 54);
                    icon = PackIconKind.DatabaseRemove;
                    break;
                case "success":
                    color = Color.FromRgb(76, 175, 80);
                    icon = PackIconKind.CheckCircle;
                    break;
                case "error":
                    color = Color.FromRgb(244, 67, 54);
                    icon = PackIconKind.AlertCircle;
                    break;
                case "warning":
                    color = Color.FromRgb(255, 152, 0);
                    icon = PackIconKind.Alert;
                    break;
                case "info":
                default:
                    color = Color.FromRgb(33, 150, 243);
                    icon = PackIconKind.Information;
                    break;
            }

            // Apply colors
            var brush = new SolidColorBrush(color);
            ToastBorder.BorderBrush = brush;
            IconElement.Foreground = brush;
            IconElement.Kind = icon;

            // Set icon background (lighter version)
            var lightColor = Color.FromArgb(40, color.R, color.G, color.B);
            ((Border)IconElement.Parent).Background = new SolidColorBrush(lightColor);
        }

        private void SlideIn()
        {
            this.RenderTransform = new TranslateTransform(400, 0);
            this.Opacity = 0;

            var translateAnim = new DoubleAnimation
            {
                From = 400,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            ((TranslateTransform)this.RenderTransform).BeginAnimation(TranslateTransform.XProperty, translateAnim);
            this.BeginAnimation(OpacityProperty, opacityAnim);
        }

        private void SlideOut(Action onComplete = null)
        {
            var translateAnim = new DoubleAnimation
            {
                To = 400,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            var opacityAnim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(250)
            };

            if (onComplete != null)
                opacityAnim.Completed += (s, e) => onComplete();

            if (this.RenderTransform is TranslateTransform transform)
            {
                transform.BeginAnimation(TranslateTransform.XProperty, translateAnim);
            }
            this.BeginAnimation(OpacityProperty, opacityAnim);
        }

        private void StartAutoCloseTimer(int milliseconds)
        {
            _autoCloseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(milliseconds)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                Close();
            };
            _autoCloseTimer.Start();
        }

        #endregion

        #region Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
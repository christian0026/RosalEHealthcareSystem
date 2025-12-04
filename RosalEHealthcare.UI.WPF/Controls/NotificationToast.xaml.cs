using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Controls
{
    public partial class NotificationToast : UserControl
    {
        private DispatcherTimer _autoCloseTimer;

        public event EventHandler Closed;
        public event EventHandler Clicked;

        public string Title
        {
            get => txtTitle.Text;
            set => txtTitle.Text = value;
        }

        public string Message
        {
            get => txtMessage.Text;
            set => txtMessage.Text = value;
        }

        public NotificationToast()
        {
            InitializeComponent();

            // Setup auto-close timer (5 seconds default)
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _autoCloseTimer.Tick += (s, e) =>
            {
                _autoCloseTimer.Stop();
                CloseToast();
            };

            // Click handler
            MainBorder.MouseLeftButtonUp += (s, e) =>
            {
                Clicked?.Invoke(this, EventArgs.Empty);
            };
            MainBorder.Cursor = System.Windows.Input.Cursors.Hand;

            Loaded += NotificationToast_Loaded;
        }

        private void NotificationToast_Loaded(object sender, RoutedEventArgs e)
        {
            // Play slide-in animation if available
            try
            {
                var slideIn = (Storyboard)FindResource("SlideIn");
                slideIn?.Begin(this);
            }
            catch
            {
                // Animation not found, continue without it
            }

            // Set time
            txtTime.Text = "Just now";
        }

        /// <summary>
        /// Show the notification toast with specified parameters
        /// </summary>
        public void Show(string title, string message, string type, int durationMs, bool playSound = true)
        {
            // Set title and message
            Title = title;
            Message = message;

            // Set type/style
            SetTypeFromString(type);

            // Set duration
            _autoCloseTimer.Interval = TimeSpan.FromMilliseconds(durationMs);
            _autoCloseTimer.Start();

            // Play sound if enabled
            if (playSound)
            {
                PlaySoundForType(type);
            }
        }

        private void SetTypeFromString(string type)
        {
            // Convert string type to enum and set
            if (Enum.TryParse<NotificationType>(type, true, out var notifType))
            {
                SetType(notifType);
            }
            else
            {
                // Try to match common type names
                switch (type?.ToLower())
                {
                    case "success":
                        SetType(NotificationType.Success);
                        break;
                    case "error":
                        SetType(NotificationType.Error);
                        break;
                    case "warning":
                        SetType(NotificationType.Warning);
                        break;
                    case "info":
                        SetType(NotificationType.Info);
                        break;
                    case "newpatient":
                        SetType(NotificationType.NewPatient);
                        break;
                    case "newappointment":
                        SetType(NotificationType.NewAppointment);
                        break;
                    case "appointmentcompleted":
                        SetType(NotificationType.AppointmentCompleted);
                        break;
                    case "appointmentcancelled":
                        SetType(NotificationType.Warning);
                        break;
                    case "lowstock":
                        SetType(NotificationType.LowStock);
                        break;
                    case "expiringmedicine":
                        SetType(NotificationType.ExpiringMedicine);
                        break;
                    default:
                        SetType(NotificationType.Info);
                        break;
                }
            }
        }

        private void PlaySoundForType(string type)
        {
            try
            {
                switch (type?.ToLower())
                {
                    case "success":
                    case "appointmentcompleted":
                        RosalEHealthcare.UI.WPF.Helpers.NotificationSoundPlayer.PlaySuccess();
                        break;
                    case "error":
                        RosalEHealthcare.UI.WPF.Helpers.NotificationSoundPlayer.PlayError();
                        break;
                    case "warning":
                    case "lowstock":
                    case "expiringmedicine":
                    case "appointmentcancelled":
                        RosalEHealthcare.UI.WPF.Helpers.NotificationSoundPlayer.PlayWarning();
                        break;
                    case "newpatient":
                    case "newappointment":
                        RosalEHealthcare.UI.WPF.Helpers.NotificationSoundPlayer.PlayNotification();
                        break;
                    default:
                        RosalEHealthcare.UI.WPF.Helpers.NotificationSoundPlayer.PlayNotification();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing sound: {ex.Message}");
            }
        }

        public void SetType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.Success:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ToastIcon.Kind = PackIconKind.CheckCircle;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    break;

                case NotificationType.Error:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ToastIcon.Kind = PackIconKind.AlertCircle;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    break;

                case NotificationType.Warning:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    ToastIcon.Kind = PackIconKind.Alert;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    break;

                case NotificationType.Info:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    ToastIcon.Kind = PackIconKind.Information;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    break;

                case NotificationType.NewPatient:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ToastIcon.Kind = PackIconKind.AccountPlus;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    break;

                case NotificationType.NewAppointment:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    ToastIcon.Kind = PackIconKind.CalendarPlus;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                    break;

                case NotificationType.AppointmentCompleted:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ToastIcon.Kind = PackIconKind.CalendarCheck;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    break;

                case NotificationType.LowStock:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    ToastIcon.Kind = PackIconKind.PackageDown;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
                    break;

                case NotificationType.ExpiringMedicine:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ToastIcon.Kind = PackIconKind.PillOff;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    break;

                default:
                    AccentBar.Background = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    ToastIcon.Kind = PackIconKind.Bell;
                    ToastIcon.Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158));
                    ((Border)ToastIcon.Parent).Background = new SolidColorBrush(Color.FromRgb(245, 245, 245));
                    break;
            }
        }

        public void SetIcon(PackIconKind iconKind)
        {
            ToastIcon.Kind = iconKind;
        }

        public void SetAccentColor(Color color)
        {
            AccentBar.Background = new SolidColorBrush(color);
            ToastIcon.Foreground = new SolidColorBrush(color);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer.Stop();
            CloseToast();
        }

        private void CloseToast()
        {
            try
            {
                var slideOut = (Storyboard)FindResource("SlideOut");
                if (slideOut != null)
                {
                    slideOut.Completed += (s, e) =>
                    {
                        Closed?.Invoke(this, EventArgs.Empty);
                    };
                    slideOut.Begin(this);
                }
                else
                {
                    // No animation, just invoke closed
                    Closed?.Invoke(this, EventArgs.Empty);
                }
            }
            catch
            {
                // If animation fails, just invoke closed
                Closed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ExtendTimer(int seconds = 5)
        {
            _autoCloseTimer.Stop();
            _autoCloseTimer.Interval = TimeSpan.FromSeconds(seconds);
            _autoCloseTimer.Start();
        }
    }

    public enum NotificationType
    {
        Success,
        Error,
        Warning,
        Info,
        NewPatient,
        NewAppointment,
        AppointmentCompleted,
        LowStock,
        ExpiringMedicine
    }
}
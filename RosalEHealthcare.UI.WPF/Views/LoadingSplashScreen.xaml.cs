using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LoadingSplashScreen : Window
    {
        private DispatcherTimer _timer;
        private int _progress = 0;
        private readonly string[] _loadingMessages = new[]
        {
            "Initializing system...",
            "Loading database connection...",
            "Loading user modules...",
            "Preparing user interface...",
            "Almost ready..."
        };

        public LoadingSplashScreen()
        {
            InitializeComponent();
            StartLoadingAnimation();
        }

        private void StartLoadingAnimation()
        {
            // Initialize timer for progress updates
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // Update every 50ms for smooth animation
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _progress += 2; // Increment progress

            if (_progress >= 100)
            {
                _progress = 100;
                _timer.Stop();

                // Small delay before closing
                Task.Delay(300).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => CloseAndShowLogin());
                });
            }

            // Update progress bar width
            double maxWidth = 440; // Total width minus margins (600 - 80*2)
            progressBar.Width = (maxWidth * _progress) / 100;

            // Update percentage text
            txtPercentage.Text = $"{_progress}%";

            // Update status message based on progress
            int messageIndex = Math.Min(_progress / 20, _loadingMessages.Length - 1);
            txtStatus.Text = _loadingMessages[messageIndex];
        }

        private void CloseAndShowLogin()
        {
            // Create fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, e) =>
            {
                // Open login window
                var loginWindow = new LoginWindow();
                loginWindow.Show();

                // Close splash screen
                this.Close();
            };

            // Apply fade out animation
            MainGrid.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
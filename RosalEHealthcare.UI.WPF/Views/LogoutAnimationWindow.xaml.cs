using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LogoutAnimationWindow : Window
    {
        private DispatcherTimer _timer;
        private int _progress = 0;
        private readonly bool _returnToLogin;

        /// <summary>
        /// Creates a logout animation window
        /// </summary>
        /// <param name="returnToLogin">If true, returns to login screen. If false, exits application.</param>
        public LogoutAnimationWindow(bool returnToLogin = true)
        {
            InitializeComponent();
            _returnToLogin = returnToLogin;

            // Set user name from session
            if (SessionManager.CurrentUser != null)
            {
                txtUserName.Text = SessionManager.CurrentUser.FullName ?? SessionManager.CurrentUser.Email;
            }

            // Set message based on action
            if (!returnToLogin)
            {
                txtMessage.Text = "Goodbye!";
                txtSubMessage.Text = "Thank you for using Rosal Healthcare System";
            }

            StartLogoutProcess();
        }

        private void StartLogoutProcess()
        {
            // Animate progress bar
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30) // Smooth animation
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _progress += 3; // Increment progress

            if (_progress >= 100)
            {
                _progress = 100;
                _timer.Stop();

                // Small delay before completing logout
                Task.Delay(400).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() => CompleteLogout());
                });
            }

            // Update progress bar width
            // Calculation: Window Width (500) - Outer Margins (60) - Inner StackPanel Margins (60) = 380
            double maxWidth = 380;
            progressBar.Width = (maxWidth * _progress) / 100;
        }

        private void CompleteLogout()
        {
            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, e) =>
            {
                // Clear session
                SessionManager.EndSession();

                if (_returnToLogin)
                {
                    // Return to login window
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();
                }
                else
                {
                    // Exit application completely
                    Application.Current.Shutdown();
                }

                // Close this window
                this.Close();
            };

            // Apply fade out
            MainGrid.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
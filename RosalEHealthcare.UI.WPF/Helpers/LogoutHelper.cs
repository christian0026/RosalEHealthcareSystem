using RosalEHealthcare.UI.WPF.Views;
using System.Windows;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class LogoutHelper
    {
        /// <summary>
        /// Performs logout with animation and returns to login screen
        /// </summary>
        /// <param name="currentWindow">The current window to close</param>
        public static void Logout(Window currentWindow)
        {
            // Show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to logout?\n\nYour session will be ended and you'll return to the login screen.",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Fade out current window
                FadeOutWindow(currentWindow, () =>
                {
                    // Show logout animation (returns to login)
                    var logoutWindow = new LogoutAnimationWindow(returnToLogin: true);
                    logoutWindow.Show();

                    // Close current dashboard
                    currentWindow.Close();
                });
            }
        }

        /// <summary>
        /// Performs exit with animation and closes the application
        /// </summary>
        /// <param name="currentWindow">The current window to close</param>
        public static void ExitApplication(Window currentWindow)
        {
            // Show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to exit?\n\nThe application will be closed completely.",
                "Exit Application",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Fade out current window
                FadeOutWindow(currentWindow, () =>
                {
                    // Show logout animation (exits app)
                    var logoutWindow = new LogoutAnimationWindow(returnToLogin: false);
                    logoutWindow.Show();

                    // Close current dashboard
                    currentWindow.Close();
                });
            }
        }

        /// <summary>
        /// Quick logout without confirmation (for forced logout scenarios)
        /// </summary>
        /// <param name="currentWindow">The current window to close</param>
        /// <param name="returnToLogin">If true, returns to login. If false, exits app.</param>
        public static void QuickLogout(Window currentWindow, bool returnToLogin = true)
        {
            FadeOutWindow(currentWindow, () =>
            {
                var logoutWindow = new LogoutAnimationWindow(returnToLogin);
                logoutWindow.Show();
                currentWindow.Close();
            });
        }

        /// <summary>
        /// Fades out a window with smooth animation
        /// </summary>
        private static void FadeOutWindow(Window window, System.Action onComplete)
        {
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = System.TimeSpan.FromSeconds(0.3)
            };

            fadeOut.Completed += (s, e) => onComplete?.Invoke();

            window.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
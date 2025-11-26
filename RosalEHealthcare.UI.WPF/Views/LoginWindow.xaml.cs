using MahApps.Metro.Controls;
using MahApps.Metro.IconPacks;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class LoginWindow : MetroWindow
    {
        // Login attempt tracking
        private int _failedAttempts = 0;
        private const int MAX_ATTEMPTS = 3;
        private const int LOCKOUT_SECONDS = 60;
        private DateTime _lockoutEndTime;
        private DispatcherTimer _lockoutTimer;
        private bool _isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
            UpdatePasswordPlaceholder();
            InitializeLockoutTimer();
        }

        #region Password Visibility Toggle

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                // Show password - switch to TextBox
                txtPasswordVisible.Text = txtPassword.Password;
                txtPasswordVisible.Visibility = Visibility.Visible;
                txtPassword.Visibility = Visibility.Collapsed;
                iconEye.Kind = PackIconMaterialKind.EyeOff;
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                // Hide password - switch to PasswordBox
                txtPassword.Password = txtPasswordVisible.Text;
                txtPassword.Visibility = Visibility.Visible;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                iconEye.Kind = PackIconMaterialKind.Eye;
                txtPassword.Focus();
            }

            UpdatePasswordPlaceholder();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            // Sync with visible textbox if needed
            if (_isPasswordVisible && txtPasswordVisible.Visibility == Visibility.Visible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
            }
            UpdatePasswordPlaceholder();
        }

        private void txtPasswordVisible_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Sync with passwordbox if needed
            if (!_isPasswordVisible && txtPassword.Visibility == Visibility.Visible)
            {
                txtPassword.Password = txtPasswordVisible.Text;
            }
            UpdatePasswordPlaceholder();
        }

        private void UpdatePasswordPlaceholder()
        {
            if (pwdPlaceholder == null) return;

            bool isEmpty = _isPasswordVisible
                ? string.IsNullOrEmpty(txtPasswordVisible.Text)
                : string.IsNullOrEmpty(txtPassword.Password);

            pwdPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private string GetPassword()
        {
            return _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;
        }

        #endregion

        #region Lockout System

        private void InitializeLockoutTimer()
        {
            _lockoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _lockoutTimer.Tick += LockoutTimer_Tick;
        }

        private void LockoutTimer_Tick(object sender, EventArgs e)
        {
            var remaining = (_lockoutEndTime - DateTime.Now).TotalSeconds;

            if (remaining <= 0)
            {
                // Unlock
                _lockoutTimer.Stop();
                _failedAttempts = 0;
                lockoutPanel.Visibility = Visibility.Collapsed;
                warningPanel.Visibility = Visibility.Collapsed;
                btnSignIn.IsEnabled = true;
                txtUsername.IsEnabled = true;
                txtPassword.IsEnabled = true;
                txtPasswordVisible.IsEnabled = true;
                rbAdmin.IsEnabled = true;
                rbDoctor.IsEnabled = true;
                rbReceptionist.IsEnabled = true;
                btnTogglePassword.IsEnabled = true;
            }
            else
            {
                // Update countdown
                txtLockout.Text = $"Too many failed login attempts. Please wait {Math.Ceiling(remaining)} seconds before trying again.";
            }
        }

        private void StartLockout()
        {
            _lockoutEndTime = DateTime.Now.AddSeconds(LOCKOUT_SECONDS);
            lockoutPanel.Visibility = Visibility.Visible;
            warningPanel.Visibility = Visibility.Collapsed;
            btnSignIn.IsEnabled = false;
            txtUsername.IsEnabled = false;
            txtPassword.IsEnabled = false;
            txtPasswordVisible.IsEnabled = false;
            rbAdmin.IsEnabled = false;
            rbDoctor.IsEnabled = false;
            rbReceptionist.IsEnabled = false;
            btnTogglePassword.IsEnabled = false;
            _lockoutTimer.Start();

            txtLockout.Text = $"Too many failed login attempts. Please wait {LOCKOUT_SECONDS} seconds before trying again.";
        }

        private void UpdateFailedAttempts()
        {
            _failedAttempts++;

            if (_failedAttempts >= MAX_ATTEMPTS)
            {
                StartLockout();
            }
            else
            {
                int remaining = MAX_ATTEMPTS - _failedAttempts;
                warningPanel.Visibility = Visibility.Visible;
                txtWarning.Text = remaining == 1
                    ? "Warning: Last attempt remaining before account lockout!"
                    : $"Warning: {remaining} failed attempts remaining before lockout";
            }
        }

        #endregion

        #region Login Logic

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            LoginUser();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && btnSignIn.IsEnabled)
            {
                e.Handled = true;
                LoginUser();
            }
        }

        private void LoginUser()
        {
            // Check if locked out
            if (!btnSignIn.IsEnabled)
            {
                MessageBox.Show(
                    "Account is temporarily locked due to multiple failed login attempts.\n\nPlease wait for the lockout period to expire.",
                    "Account Locked",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string username = txtUsername.Text?.Trim() ?? "";
            string password = GetPassword()?.Trim() ?? "";
            string role = rbAdmin.IsChecked == true ? "Administrator" :
                          rbDoctor.IsChecked == true ? "Doctor" : "Receptionist";

            // Validation
            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show(
                    "Please enter your username.",
                    "Login",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter your password.",
                    "Login",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                if (_isPasswordVisible)
                    txtPasswordVisible.Focus();
                else
                    txtPassword.Focus();
                return;
            }

            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var userService = new UserService(db);
                    var user = userService.GetByUsername(username);

                    if (user != null && user.Role == role && userService.ValidateUserByUsername(username, password))
                    {
                        // Successful login - reset failed attempts
                        _failedAttempts = 0;
                        warningPanel.Visibility = Visibility.Collapsed;

                        // SET SESSION MANAGER
                        SessionManager.StartSession(user);

                        // Update last login
                        user.LastLogin = DateTime.Now;
                        userService.UpdateUser(user);

                        // Open appropriate dashboard
                        if (user.Role == "Administrator")
                        {
                            var dashboard = new AdminDashboard(user);
                            dashboard.Show();
                        }
                        else if (user.Role == "Doctor")
                        {
                            var dashboard = new DoctorDashboard(user);
                            dashboard.Show();
                        }
                        else // Receptionist
                        {
                            var dashboard = new ReceptionistDashboard(user);
                            dashboard.Show();
                        }

                        this.Close();
                    }
                    else
                    {
                        // Failed login
                        UpdateFailedAttempts();

                        string message = _failedAttempts >= MAX_ATTEMPTS
                            ? "Too many failed attempts. Your account has been temporarily locked."
                            : "Invalid username, password, or role.\n\nPlease check your credentials and try again.";

                        MessageBox.Show(
                            message,
                            "Login Failed",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);

                        // Clear password fields
                        txtPassword.Clear();
                        txtPasswordVisible.Clear();
                        UpdatePasswordPlaceholder();

                        if (!btnSignIn.IsEnabled)
                            return;

                        txtUsername.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                string details = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show(
                    $"An error occurred during login:\n\n{details}\n\nPlease contact system administrator if this problem persists.",
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                // Log the error
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            }
        }

        #endregion

        #region Window Management

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window by clicking anywhere
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try
                {
                    this.DragMove();
                }
                catch
                {
                    // Ignore drag errors (happens when clicking on controls)
                }
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit the application?",
                "Exit Application",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        #endregion
    }
}
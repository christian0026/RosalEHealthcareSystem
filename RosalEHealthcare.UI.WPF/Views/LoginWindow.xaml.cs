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
        // Services
        private RosalEHealthcareDbContext _db;
        private UserService _userService;
        private LoginHistoryService _loginHistoryService;
        private SystemSettingsService _settingsService;
        private ActivityLogService _activityLogService;

        // Login attempt tracking (now uses database settings)
        private int _failedAttempts = 0;
        private int _maxAttempts = 3;
        private int _lockoutSeconds = 60;
        private DateTime _lockoutEndTime;
        private DispatcherTimer _lockoutTimer;
        private bool _isPasswordVisible = false;

        public LoginWindow()
        {
            InitializeComponent();
            InitializeServices();
            LoadSecuritySettings();
            txtUsername.Focus();
            UpdatePasswordPlaceholder();
            InitializeLockoutTimer();
        }

        #region Initialization

        private void InitializeServices()
        {
            try
            {
                _db = new RosalEHealthcareDbContext();
                _userService = new UserService(_db);
                _loginHistoryService = new LoginHistoryService(_db);
                _settingsService = new SystemSettingsService(_db);
                _activityLogService = new ActivityLogService(_db);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing services: {ex.Message}");
                // Use defaults if database is unavailable
                _maxAttempts = 3;
                _lockoutSeconds = 60;
            }
        }

        private void LoadSecuritySettings()
        {
            try
            {
                if (_settingsService != null)
                {
                    _maxAttempts = _settingsService.GetMaxFailedLoginAttempts();
                    _lockoutSeconds = _settingsService.GetLockoutDurationMinutes() * 60; // Convert to seconds

                    // If settings return 0 or -1, use defaults
                    if (_maxAttempts <= 0) _maxAttempts = 3;
                    if (_lockoutSeconds <= 0) _lockoutSeconds = 60;
                }
            }
            catch
            {
                // Use defaults
                _maxAttempts = 3;
                _lockoutSeconds = 60;
            }
        }

        #endregion

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

        private void StartLockout(string username)
        {
            _lockoutEndTime = DateTime.Now.AddSeconds(_lockoutSeconds);
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

            txtLockout.Text = $"Too many failed login attempts. Please wait {_lockoutSeconds} seconds before trying again.";

            // Record lockout in database
            try
            {
                _loginHistoryService?.RecordAccountLocked(username);
            }
            catch { /* Ignore logging errors */ }
        }

        private void UpdateFailedAttempts(string username, string reason)
        {
            _failedAttempts++;

            // Record failed attempt in database
            try
            {
                _loginHistoryService?.RecordFailedLogin(username, reason);
            }
            catch { /* Ignore logging errors */ }

            if (_failedAttempts >= _maxAttempts)
            {
                StartLockout(username);
            }
            else
            {
                int remaining = _maxAttempts - _failedAttempts;
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
                // Reinitialize db context to ensure fresh connection
                using (var db = new RosalEHealthcareDbContext())
                {
                    var userService = new UserService(db);
                    var loginHistoryService = new LoginHistoryService(db);
                    var activityLogService = new ActivityLogService(db);

                    var user = userService.GetByUsername(username);

                    if (user != null && user.Role == role && userService.ValidateUserByUsername(username, password))
                    {
                        // Check if user is active
                        if (!user.IsActive)
                        {
                            UpdateFailedAttempts(username, "Account is inactive");
                            MessageBox.Show(
                                "Your account has been deactivated.\n\nPlease contact an administrator.",
                                "Account Inactive",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Check if user is deleted
                        if (user.IsDeleted)
                        {
                            UpdateFailedAttempts(username, "Account is deleted");
                            MessageBox.Show(
                                "This account no longer exists.",
                                "Account Not Found",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Successful login - reset failed attempts
                        _failedAttempts = 0;
                        warningPanel.Visibility = Visibility.Collapsed;

                        // Record successful login in database
                        int? loginHistoryId = null;
                        try
                        {
                            var loginRecord = loginHistoryService.RecordLogin(
                                userId: user.Id,
                                username: user.Username,
                                fullName: user.FullName,
                                role: user.Role
                            );
                            loginHistoryId = loginRecord?.Id;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error recording login: {ex.Message}");
                        }

                        // SET SESSION MANAGER with login history ID
                        SessionManager.StartSession(user, loginHistoryId);

                        // Log activity
                        try
                        {
                            activityLogService.LogActivity(
                                activityType: "Login",
                                description: "User logged in successfully",
                                module: "Authentication",
                                performedBy: user.FullName,
                                performedByRole: user.Role
                            );
                        }
                        catch { /* Ignore logging errors */ }

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
                        // Failed login - determine reason
                        string failureReason;
                        if (user == null)
                        {
                            failureReason = "Invalid username";
                        }
                        else if (user.Role != role)
                        {
                            failureReason = "Invalid role selected";
                        }
                        else
                        {
                            failureReason = "Invalid password";
                        }

                        UpdateFailedAttempts(username, failureReason);

                        string message = _failedAttempts >= _maxAttempts
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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Dispose database context
            try
            {
                _db?.Dispose();
            }
            catch { }
        }

        #endregion
    }
}
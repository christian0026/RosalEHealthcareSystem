using RosalEHealthcare.UI.WPF.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace RosalEHealthcare.UI.WPF.Views.UserSettings
{
    public partial class UserAboutTab : UserControl
    {
        public UserAboutTab()
        {
            InitializeComponent();
        }

        #region Load Settings

        public void LoadSettings()
        {
            try
            {
                LoadApplicationInfo();
                LoadSystemInfo();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading about info: {ex.Message}");
            }
        }

        private void LoadApplicationInfo()
        {
            try
            {
                // Get version from assembly
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                TxtVersion.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";

                // Get build date
                var buildDate = File.GetLastWriteTime(assembly.Location);
                TxtBuildDate.Text = $"Build: {buildDate:MMM yyyy}";
            }
            catch
            {
                TxtVersion.Text = "Version 1.0.0";
                TxtBuildDate.Text = "Build: 2024";
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                // Operating System
                TxtOS.Text = Environment.OSVersion.ToString();

                // .NET Version
                TxtDotNet.Text = Environment.Version.ToString();

                // Machine Name
                TxtMachineName.Text = Environment.MachineName;

                // Windows User
                TxtWindowsUser.Text = Environment.UserName;

                // Memory Usage
                var process = Process.GetCurrentProcess();
                var memoryMB = process.WorkingSet64 / (1024 * 1024);
                TxtMemoryUsage.Text = $"{memoryMB} MB";

                // Session Duration
                var sessionDuration = SessionManager.SessionDuration;
                if (sessionDuration.HasValue)
                {
                    var duration = sessionDuration.Value;
                    if (duration.TotalHours >= 1)
                    {
                        TxtSessionDuration.Text = $"{(int)duration.TotalHours}h {duration.Minutes}m";
                    }
                    else if (duration.TotalMinutes >= 1)
                    {
                        TxtSessionDuration.Text = $"{(int)duration.TotalMinutes}m";
                    }
                    else
                    {
                        TxtSessionDuration.Text = $"{duration.Seconds}s";
                    }
                }
                else
                {
                    TxtSessionDuration.Text = "N/A";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading system info: {ex.Message}");
            }
        }

        #endregion

        #region Help & Support Actions

        private void BtnUserManual_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Try to open user manual PDF
                var manualPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documentation", "UserManual.pdf");

                if (File.Exists(manualPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = manualPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        "User manual is not available at this time.\n\n" +
                        "Please contact the administrator for assistance.",
                        "User Manual",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening user manual: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnContactSupport_Click(object sender, RoutedEventArgs e)
        {
            var supportInfo = new StringBuilder();
            supportInfo.AppendLine("📧 Support Contact Information");
            supportInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            supportInfo.AppendLine();
            supportInfo.AppendLine("Email: support@rosalhealthcare.com");
            supportInfo.AppendLine("Phone: (02) 8888-ROSAL");
            supportInfo.AppendLine("Hours: Mon-Fri, 8:00 AM - 5:00 PM");
            supportInfo.AppendLine();
            supportInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            supportInfo.AppendLine($"Your Version: {TxtVersion.Text}");
            supportInfo.AppendLine($"Your Role: {SessionManager.CurrentUser?.Role ?? "Unknown"}");

            MessageBox.Show(
                supportInfo.ToString(),
                "Contact Support",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnReportBug_Click(object sender, RoutedEventArgs e)
        {
            var bugReportInfo = new StringBuilder();
            bugReportInfo.AppendLine("🐛 How to Report a Bug");
            bugReportInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            bugReportInfo.AppendLine();
            bugReportInfo.AppendLine("Please provide the following information:");
            bugReportInfo.AppendLine();
            bugReportInfo.AppendLine("1. Description of the problem");
            bugReportInfo.AppendLine("2. Steps to reproduce the issue");
            bugReportInfo.AppendLine("3. What you expected to happen");
            bugReportInfo.AppendLine("4. What actually happened");
            bugReportInfo.AppendLine("5. Any error messages you saw");
            bugReportInfo.AppendLine();
            bugReportInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            bugReportInfo.AppendLine("Send bug reports to:");
            bugReportInfo.AppendLine("bugs@rosalhealthcare.com");
            bugReportInfo.AppendLine();
            bugReportInfo.AppendLine("Or contact your system administrator.");

            MessageBox.Show(
                bugReportInfo.ToString(),
                "Report a Bug",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnFeatureRequest_Click(object sender, RoutedEventArgs e)
        {
            var featureInfo = new StringBuilder();
            featureInfo.AppendLine("💡 Feature Request");
            featureInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            featureInfo.AppendLine();
            featureInfo.AppendLine("We value your feedback!");
            featureInfo.AppendLine();
            featureInfo.AppendLine("To suggest a new feature or improvement:");
            featureInfo.AppendLine();
            featureInfo.AppendLine("1. Describe the feature you'd like");
            featureInfo.AppendLine("2. Explain how it would help your work");
            featureInfo.AppendLine("3. Include any examples if possible");
            featureInfo.AppendLine();
            featureInfo.AppendLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            featureInfo.AppendLine("Send feature requests to:");
            featureInfo.AppendLine("features@rosalhealthcare.com");
            featureInfo.AppendLine();
            featureInfo.AppendLine("Or speak with your system administrator.");

            MessageBox.Show(
                featureInfo.ToString(),
                "Feature Request",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        #endregion
    }

    // Helper class for StringBuilder (if not using System.Text)
    internal class StringBuilder
    {
        private string _value = "";

        public void AppendLine(string text = "")
        {
            _value += text + "\n";
        }

        public override string ToString()
        {
            return _value;
        }
    }
}
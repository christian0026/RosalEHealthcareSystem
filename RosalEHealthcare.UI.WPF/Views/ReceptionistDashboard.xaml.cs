using MahApps.Metro.Controls;
using RosalEHealthcare.Core.Models;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ReceptionistDashboard : MetroWindow
    {
        private User _currentUser;

        public ReceptionistDashboard()
        {
            InitializeComponent();
        }

        // constructor that receives logged-in user
        public ReceptionistDashboard(User user) : this()
        {
            _currentUser = user;
            ApplyUserInfo();
        }

        private void ApplyUserInfo()
        {
            if (_currentUser == null) return;

            TxtUserFullName.Text = _currentUser.FullName ?? _currentUser.Email;
            TxtUserRole.Text = _currentUser.Role ?? "Receptionist";

            if (!string.IsNullOrEmpty(_currentUser.ProfileImagePath) && File.Exists(_currentUser.ProfileImagePath))
            {
                try
                {
                    var img = new BitmapImage(new Uri(_currentUser.ProfileImagePath, UriKind.RelativeOrAbsolute));
                    ProfileEllipse.Fill = new ImageBrush(img) { Stretch = Stretch.UniformToFill };
                }
                catch
                {
                    ProfileEllipse.Fill = Brushes.LightGray;
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}

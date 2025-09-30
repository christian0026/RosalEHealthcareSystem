using System.Windows;
using MahApps.Metro.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class DoctorDashboard : MetroWindow
    {
        public DoctorDashboard()
        {
            InitializeComponent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();    // close dashboard window
        }
    }
}

using System.Windows;
using MahApps.Metro.Controls;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class ReceptionistDashboard : MetroWindow
    {
        public ReceptionistDashboard()
        {
            InitializeComponent();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            var login = new LoginWindow();
            login.Show();
            this.Close();   // this works correctly now
        }
    }
}

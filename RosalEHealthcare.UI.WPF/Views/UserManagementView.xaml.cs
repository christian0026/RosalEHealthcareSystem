using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RosalEHealthcare.UI.WPF.Views
{
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            var users = new List<UserModel>
            {
                new UserModel { Name = "Dr. John Doe", UserID = "USR-001", Role = "Administrator", Email = "john.doe@gmail.com", LastLogin = "January 15, 2025", Status = "Active" },
                new UserModel { Name = "Dr. Jane Doe", UserID = "USR-002", Role = "Doctor", Email = "jane.doe@gmail.com", LastLogin = "March 16, 2025", Status = "Active" },
                new UserModel { Name = "Alice Lopez", UserID = "USR-003", Role = "Receptionist", Email = "alice.lopez@gmail.com", LastLogin = "March 17, 2025", Status = "Inactive" },
                new UserModel { Name = "Dr. Bob Rivera", UserID = "USR-004", Role = "Doctor", Email = "bob.rivera@gmail.com", LastLogin = "January 19, 2025", Status = "Active" },
                new UserModel { Name = "Maya Garcia", UserID = "USR-005", Role = "Receptionist", Email = "maya.garcia@gmail.com", LastLogin = "January 3, 2025", Status = "Inactive" }
            };

            UsersDataGrid.ItemsSource = users;
        }
    }

    public class UserModel
    {
        public string Name { get; set; }
        public string UserID { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string LastLogin { get; set; }
        public string Status { get; set; }
    }
}

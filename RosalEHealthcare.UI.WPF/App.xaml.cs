using RosalEHealthcare.Core.Services;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RosalEHealthcare.UI.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // SEED ADMIN ACCOUNT (Run once only)
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var userService = new UserService(db);

                    if (!db.Users.Any(u => u.Email == "admin@rosal.com"))
                    {
                        userService.Register(
                            "Admin User",
                            "admin@rosal.com",
                            "admin123",
                            "Admin"
                        );

                        MessageBox.Show("✅ Admin account created successfully!");
                    }
                    else
                    {
                        MessageBox.Show("ℹ️ Admin account already exists.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error creating admin: {ex.Message}");
            }

            // Continue to your login window after seeding
            var login = new LoginWindow();
            login.Show();
        }
    }
}

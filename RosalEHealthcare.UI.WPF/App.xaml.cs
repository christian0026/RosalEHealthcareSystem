using System;
using System.Linq;
using System.Windows;
using RosalEHealthcare.Data.Contexts;
using RosalEHealthcare.Data.Services;
using RosalEHealthcare.UI.WPF.Views;



namespace RosalEHealthcare.UI.WPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Seed (run once)
            try
            {
                using (var db = new RosalEHealthcareDbContext())
                {
                    var userService = new UserService(db);

                    if (!db.Users.Any(u => u.Email == "admin@rosal.com"))
                    {
                        userService.Register("Admin User", "admin@rosal.com", "admin123", "Admin");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error seeding admin: " + ex.Message);
            }

            var splashScreen = new LoadingSplashScreen();
            splashScreen.Show();
        }
    }
}

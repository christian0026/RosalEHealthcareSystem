using RosalEHealthcare.Core.Models;
using System;
namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class SessionManager
    {
        public static User CurrentUser { get; set; }
        public static DateTime LoginTime { get; set; }
        public static string SessionId { get; set; }
        public static void StartSession(User user)
        {
            CurrentUser = user;
            LoginTime = DateTime.Now;
            SessionId = Guid.NewGuid().ToString();
        }
        public static void EndSession()
        {
            CurrentUser = null;
            LoginTime = DateTime.MinValue;
            SessionId = null;
        }
        public static bool IsLoggedIn()
        {
            return CurrentUser != null;
        }
        public static bool HasRole(string role)
        {
            return CurrentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
        }
        public static bool IsDoctor()
        {
            return HasRole("Doctor");
        }
        public static bool IsReceptionist()
        {
            return HasRole("Receptionist");
        }
        public static bool IsAdministrator()
        {
            return HasRole("Administrator");
        }
        public static string GetUserFullName()
        {
            return CurrentUser?.FullName ?? "Unknown User";
        }
        public static string GetUserRole()
        {
            return CurrentUser?.Role ?? "Unknown";
        }
        public static TimeSpan GetSessionDuration()
        {
            if (LoginTime == DateTime.MinValue)
                return TimeSpan.Zero;
            return DateTime.Now - LoginTime;
        }
        public static void ClearSession()
        {
            CurrentUser = null;
        }
    }
}
using RosalEHealthcare.Core.Models;
using System;

namespace RosalEHealthcare.UI.WPF.Helpers
{
    public static class SessionManager
    {
        public static User CurrentUser { get; set; }
        public static string CurrentSessionId { get; private set; }
        public static int? CurrentLoginHistoryId { get; private set; }
        public static DateTime? SessionStartTime { get; private set; }

        /// <summary>
        /// Starts a new session for the user
        /// </summary>
        public static void StartSession(User user, int? loginHistoryId = null)
        {
            CurrentUser = user;
            CurrentSessionId = Guid.NewGuid().ToString();
            CurrentLoginHistoryId = loginHistoryId;
            SessionStartTime = DateTime.Now;
        }

        /// <summary>
        /// Ends the current session
        /// </summary>
        public static void EndSession()
        {
            CurrentUser = null;
            CurrentSessionId = null;
            CurrentLoginHistoryId = null;
            SessionStartTime = null;
        }

        /// <summary>
        /// Clears the current session (alias for EndSession)
        /// </summary>
        public static void ClearSession()
        {
            EndSession();
        }

        /// <summary>
        /// Checks if a user is currently logged in
        /// </summary>
        public static bool IsLoggedIn => CurrentUser != null;

        /// <summary>
        /// Gets the current user's full name (for PdfExportService compatibility)
        /// </summary>
        public static string GetUserFullName()
        {
            return CurrentUser?.FullName ?? CurrentUser?.Email ?? "System";
        }

        /// <summary>
        /// Gets the session duration
        /// </summary>
        public static TimeSpan? SessionDuration
        {
            get
            {
                if (SessionStartTime.HasValue)
                {
                    return DateTime.Now - SessionStartTime.Value;
                }
                return null;
            }
        }
    }
}
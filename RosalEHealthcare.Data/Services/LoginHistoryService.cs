using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class LoginHistoryService
    {
        private readonly RosalEHealthcareDbContext _db;

        public LoginHistoryService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Record Login

        /// <summary>
        /// Record a successful login
        /// </summary>
        public LoginHistory RecordLogin(User user, string sessionId)
        {
            var loginRecord = new LoginHistory
            {
                UserId = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                LoginTime = DateTime.Now,
                Status = "Success",
                IsActive = true,
                SessionId = sessionId,
                MachineName = Environment.MachineName,
                IpAddress = GetLocalIpAddress()
            };

            _db.LoginHistories.Add(loginRecord);
            _db.SaveChanges();

            return loginRecord;
        }

        /// <summary>
        /// Record a failed login attempt
        /// </summary>
        public LoginHistory RecordFailedLogin(string username, string failureReason)
        {
            var loginRecord = new LoginHistory
            {
                Username = username,
                LoginTime = DateTime.Now,
                Status = "Failed",
                FailureReason = failureReason,
                IsActive = false,
                MachineName = Environment.MachineName,
                IpAddress = GetLocalIpAddress()
            };

            _db.LoginHistories.Add(loginRecord);
            _db.SaveChanges();

            return loginRecord;
        }

        /// <summary>
        /// Record a locked account login attempt
        /// </summary>
        public LoginHistory RecordLockedLogin(string username)
        {
            var loginRecord = new LoginHistory
            {
                Username = username,
                LoginTime = DateTime.Now,
                Status = "Locked",
                FailureReason = "Account is locked due to multiple failed login attempts",
                IsActive = false,
                MachineName = Environment.MachineName,
                IpAddress = GetLocalIpAddress()
            };

            _db.LoginHistories.Add(loginRecord);
            _db.SaveChanges();

            return loginRecord;
        }

        #endregion

        #region Record Logout

        /// <summary>
        /// Record logout for a session
        /// </summary>
        public void RecordLogout(string sessionId)
        {
            var session = _db.LoginHistories
                .FirstOrDefault(l => l.SessionId == sessionId && l.IsActive);

            if (session != null)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Record logout by user ID
        /// </summary>
        public void RecordLogoutByUserId(int userId)
        {
            var sessions = _db.LoginHistories
                .Where(l => l.UserId == userId && l.IsActive)
                .ToList();

            foreach (var session in sessions)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;
            }

            _db.SaveChanges();
        }

        /// <summary>
        /// Mark session as expired
        /// </summary>
        public void MarkSessionExpired(string sessionId)
        {
            var session = _db.LoginHistories
                .FirstOrDefault(l => l.SessionId == sessionId && l.IsActive);

            if (session != null)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.Status = "Expired";
                session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;
                _db.SaveChanges();
            }
        }

        #endregion

        #region Query History

        /// <summary>
        /// Get all login history
        /// </summary>
        public IEnumerable<LoginHistory> GetAll()
        {
            return _db.LoginHistories
                .OrderByDescending(l => l.LoginTime)
                .ToList();
        }

        /// <summary>
        /// Get login history with pagination
        /// </summary>
        public IEnumerable<LoginHistory> GetPaged(int pageNumber, int pageSize)
        {
            return _db.LoginHistories
                .OrderByDescending(l => l.LoginTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        /// <summary>
        /// Get total count
        /// </summary>
        public int GetTotalCount()
        {
            return _db.LoginHistories.Count();
        }

        /// <summary>
        /// Get login history for a specific user
        /// </summary>
        public IEnumerable<LoginHistory> GetByUserId(int userId, int count = 50)
        {
            return _db.LoginHistories
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get login history by username
        /// </summary>
        public IEnumerable<LoginHistory> GetByUsername(string username, int count = 50)
        {
            return _db.LoginHistories
                .Where(l => l.Username.ToLower() == username.ToLower())
                .OrderByDescending(l => l.LoginTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get login history by date range
        /// </summary>
        public IEnumerable<LoginHistory> GetByDateRange(DateTime startDate, DateTime endDate)
        {
            return _db.LoginHistories
                .Where(l => l.LoginTime >= startDate && l.LoginTime <= endDate)
                .OrderByDescending(l => l.LoginTime)
                .ToList();
        }

        /// <summary>
        /// Get failed login attempts
        /// </summary>
        public IEnumerable<LoginHistory> GetFailedLogins(int count = 50)
        {
            return _db.LoginHistories
                .Where(l => l.Status == "Failed" || l.Status == "Locked")
                .OrderByDescending(l => l.LoginTime)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Search login history
        /// </summary>
        public IEnumerable<LoginHistory> Search(string query, string status, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize)
        {
            var q = _db.LoginHistories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(l => l.Username.ToLower().Contains(query) ||
                                 (l.FullName != null && l.FullName.ToLower().Contains(query)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                q = q.Where(l => l.Status == status);
            }

            if (startDate.HasValue)
            {
                q = q.Where(l => l.LoginTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDatePlusOne = endDate.Value.AddDays(1);
                q = q.Where(l => l.LoginTime < endDatePlusOne);
            }

            return q.OrderByDescending(l => l.LoginTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
        }

        /// <summary>
        /// Get search count
        /// </summary>
        public int GetSearchCount(string query, string status, DateTime? startDate, DateTime? endDate)
        {
            var q = _db.LoginHistories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(l => l.Username.ToLower().Contains(query) ||
                                 (l.FullName != null && l.FullName.ToLower().Contains(query)));
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All")
            {
                q = q.Where(l => l.Status == status);
            }

            if (startDate.HasValue)
            {
                q = q.Where(l => l.LoginTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                var endDatePlusOne = endDate.Value.AddDays(1);
                q = q.Where(l => l.LoginTime < endDatePlusOne);
            }

            return q.Count();
        }

        #endregion

        #region Active Sessions

        /// <summary>
        /// Get all active sessions
        /// </summary>
        public IEnumerable<LoginHistory> GetActiveSessions()
        {
            return _db.LoginHistories
                .Where(l => l.IsActive)
                .OrderByDescending(l => l.LoginTime)
                .ToList();
        }

        /// <summary>
        /// Get active session count
        /// </summary>
        public int GetActiveSessionCount()
        {
            return _db.LoginHistories.Count(l => l.IsActive);
        }

        /// <summary>
        /// Force logout a session
        /// </summary>
        public bool ForceLogout(int loginHistoryId, string forcedBy)
        {
            var session = _db.LoginHistories.Find(loginHistoryId);
            if (session == null || !session.IsActive) return false;

            session.LogoutTime = DateTime.Now;
            session.IsActive = false;
            session.Status = "Expired";
            session.FailureReason = $"Forced logout by {forcedBy}";
            session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;

            _db.SaveChanges();
            return true;
        }

        /// <summary>
        /// Force logout all sessions for a user
        /// </summary>
        public int ForceLogoutUser(int userId, string forcedBy, string excludeSessionId = null)
        {
            var sessions = _db.LoginHistories
                .Where(l => l.UserId == userId && l.IsActive)
                .ToList();

            if (!string.IsNullOrEmpty(excludeSessionId))
            {
                sessions = sessions.Where(l => l.SessionId != excludeSessionId).ToList();
            }

            foreach (var session in sessions)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.Status = "Expired";
                session.FailureReason = $"Forced logout by {forcedBy}";
                session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;
            }

            _db.SaveChanges();
            return sessions.Count;
        }

        /// <summary>
        /// Force logout all sessions
        /// </summary>
        public int ForceLogoutAll(string forcedBy, string excludeSessionId = null)
        {
            var sessions = _db.LoginHistories
                .Where(l => l.IsActive)
                .ToList();

            if (!string.IsNullOrEmpty(excludeSessionId))
            {
                sessions = sessions.Where(l => l.SessionId != excludeSessionId).ToList();
            }

            foreach (var session in sessions)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.Status = "Expired";
                session.FailureReason = $"Forced logout by {forcedBy}";
                session.SessionDuration = (int)(DateTime.Now - session.LoginTime).TotalMinutes;
            }

            _db.SaveChanges();
            return sessions.Count;
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get login statistics for today
        /// </summary>
        public LoginStatistics GetTodayStatistics()
        {
            var today = DateTime.Today;

            return new LoginStatistics
            {
                TotalLogins = _db.LoginHistories.Count(l => DbFunctions.TruncateTime(l.LoginTime) == today),
                SuccessfulLogins = _db.LoginHistories.Count(l => DbFunctions.TruncateTime(l.LoginTime) == today && l.Status == "Success"),
                FailedLogins = _db.LoginHistories.Count(l => DbFunctions.TruncateTime(l.LoginTime) == today && l.Status == "Failed"),
                LockedLogins = _db.LoginHistories.Count(l => DbFunctions.TruncateTime(l.LoginTime) == today && l.Status == "Locked"),
                ActiveSessions = _db.LoginHistories.Count(l => l.IsActive)
            };
        }

        /// <summary>
        /// Get recent failed login attempts count (last hour)
        /// </summary>
        public int GetRecentFailedAttempts(string username, int withinMinutes = 60)
        {
            var since = DateTime.Now.AddMinutes(-withinMinutes);
            return _db.LoginHistories.Count(l =>
                l.Username.ToLower() == username.ToLower() &&
                l.Status == "Failed" &&
                l.LoginTime >= since);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Delete old login history records
        /// </summary>
        public int CleanupOldRecords(int daysToKeep)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);

            var oldRecords = _db.LoginHistories
                .Where(l => l.LoginTime < cutoffDate && !l.IsActive)
                .ToList();

            int count = oldRecords.Count;
            _db.LoginHistories.RemoveRange(oldRecords);
            _db.SaveChanges();

            return count;
        }

        #endregion

        #region Helpers

        private string GetLocalIpAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "127.0.0.1";
        }

        #endregion
    }

    /// <summary>
    /// Login statistics class
    /// </summary>
    public class LoginStatistics
    {
        public int TotalLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public int LockedLogins { get; set; }
        public int ActiveSessions { get; set; }

        public double SuccessRate => TotalLogins > 0 ? Math.Round((double)SuccessfulLogins / TotalLogins * 100, 1) : 0;
    }
}
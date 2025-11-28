using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
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

        #region Core Methods

        /// <summary>
        /// Records a successful login
        /// </summary>
        public LoginHistory RecordLogin(int userId, string username, string fullName, string role,
            string machineName = null, string ipAddress = null)
        {
            var loginHistory = new LoginHistory
            {
                UserId = userId,
                Username = username,
                FullName = fullName,
                Role = role,
                LoginTime = DateTime.Now,
                MachineName = machineName ?? Environment.MachineName,
                IpAddress = ipAddress ?? GetLocalIpAddress(),
                Status = "Success",
                IsActive = true
            };

            _db.LoginHistories.Add(loginHistory);
            _db.SaveChanges();

            return loginHistory;
        }

        /// <summary>
        /// Records a failed login attempt
        /// </summary>
        public void RecordFailedLogin(string username, string reason, string machineName = null, string ipAddress = null)
        {
            var loginHistory = new LoginHistory
            {
                UserId = null,
                Username = username,
                FullName = "Unknown",
                Role = "Unknown",
                LoginTime = DateTime.Now,
                MachineName = machineName ?? Environment.MachineName,
                IpAddress = ipAddress ?? GetLocalIpAddress(),
                Status = "Failed",
                FailureReason = reason,
                IsActive = false
            };

            _db.LoginHistories.Add(loginHistory);
            _db.SaveChanges();
        }

        /// <summary>
        /// Records when an account is locked
        /// </summary>
        public void RecordAccountLocked(string username, string machineName = null, string ipAddress = null)
        {
            var loginHistory = new LoginHistory
            {
                UserId = null,
                Username = username,
                FullName = "Unknown",
                Role = "Unknown",
                LoginTime = DateTime.Now,
                MachineName = machineName ?? Environment.MachineName,
                IpAddress = ipAddress ?? GetLocalIpAddress(),
                Status = "Locked",
                FailureReason = "Account locked due to too many failed attempts",
                IsActive = false
            };

            _db.LoginHistories.Add(loginHistory);
            _db.SaveChanges();
        }

        /// <summary>
        /// Records a logout
        /// </summary>
        public void RecordLogout(int loginHistoryId)
        {
            var record = _db.LoginHistories.Find(loginHistoryId);
            if (record != null)
            {
                record.LogoutTime = DateTime.Now;
                record.IsActive = false;
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Records logout by user ID (logs out all active sessions)
        /// </summary>
        public void RecordLogoutByUserId(int userId)
        {
            var activeSessions = _db.LoginHistories
                .Where(l => l.UserId == userId && l.IsActive)
                .ToList();

            foreach (var session in activeSessions)
            {
                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
            }

            _db.SaveChanges();
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets all login history records
        /// </summary>
        public IEnumerable<LoginHistory> GetAll()
        {
            return _db.LoginHistories.OrderByDescending(l => l.LoginTime).ToList();
        }

        /// <summary>
        /// Gets login history with pagination
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
        /// Searches login history with filters
        /// </summary>
        public IEnumerable<LoginHistory> Search(string query = null, string status = null,
            DateTime? startDate = null, DateTime? endDate = null, int pageNumber = 1, int pageSize = 20)
        {
            var queryable = _db.LoginHistories.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(l => l.Username.Contains(query) ||
                                                 l.FullName.Contains(query) ||
                                                 l.MachineName.Contains(query));
            }

            if (!string.IsNullOrEmpty(status))
            {
                queryable = queryable.Where(l => l.Status == status);
            }

            if (startDate.HasValue)
            {
                queryable = queryable.Where(l => l.LoginTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                queryable = queryable.Where(l => l.LoginTime <= endDate.Value);
            }

            return queryable.OrderByDescending(l => l.LoginTime)
                           .Skip((pageNumber - 1) * pageSize)
                           .Take(pageSize)
                           .ToList();
        }

        /// <summary>
        /// Gets count for search results
        /// </summary>
        public int GetSearchCount(string query = null, string status = null,
            DateTime? startDate = null, DateTime? endDate = null)
        {
            var queryable = _db.LoginHistories.AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                queryable = queryable.Where(l => l.Username.Contains(query) ||
                                                 l.FullName.Contains(query) ||
                                                 l.MachineName.Contains(query));
            }

            if (!string.IsNullOrEmpty(status))
            {
                queryable = queryable.Where(l => l.Status == status);
            }

            if (startDate.HasValue)
            {
                queryable = queryable.Where(l => l.LoginTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                queryable = queryable.Where(l => l.LoginTime <= endDate.Value);
            }

            return queryable.Count();
        }

        /// <summary>
        /// Gets active sessions
        /// </summary>
        public IEnumerable<LoginHistory> GetActiveSessions()
        {
            return _db.LoginHistories
                .Where(l => l.IsActive && l.Status == "Success")
                .OrderByDescending(l => l.LoginTime)
                .ToList();
        }

        /// <summary>
        /// Gets login history for a specific user
        /// </summary>
        public IEnumerable<LoginHistory> GetByUserId(int userId, int limit = 50)
        {
            return _db.LoginHistories
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginTime)
                .Take(limit)
                .ToList();
        }

        /// <summary>
        /// Gets failed login attempts for a username in the last X minutes
        /// </summary>
        public int GetRecentFailedAttempts(string username, int minutes = 30)
        {
            var cutoff = DateTime.Now.AddMinutes(-minutes);
            return _db.LoginHistories
                .Count(l => l.Username == username &&
                           l.Status == "Failed" &&
                           l.LoginTime >= cutoff);
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets today's login statistics
        /// </summary>
        public LoginStatistics GetTodayStatistics()
        {
            var today = DateTime.Today;
            var todayLogins = _db.LoginHistories.Where(l => l.LoginTime >= today).ToList();

            int total = todayLogins.Count;
            int successful = todayLogins.Count(l => l.Status == "Success");
            int failed = todayLogins.Count(l => l.Status == "Failed");

            return new LoginStatistics
            {
                TotalLogins = total,
                SuccessfulLogins = successful,
                FailedLogins = failed,
                SuccessRate = total > 0 ? (double)successful / total * 100 : 0
            };
        }

        #endregion

        #region Force Logout

        /// <summary>
        /// Force logout a specific session
        /// </summary>
        public void ForceLogout(int loginHistoryId, string forcedBy)
        {
            var record = _db.LoginHistories.Find(loginHistoryId);
            if (record != null)
            {
                record.LogoutTime = DateTime.Now;
                record.IsActive = false;
                record.FailureReason = $"Force logged out by {forcedBy}";
                _db.SaveChanges();
            }
        }

        /// <summary>
        /// Force logout all sessions except the specified one
        /// </summary>
        public void ForceLogoutAll(string forcedBy, string exceptSessionId = null)
        {
            var activeSessions = _db.LoginHistories
                .Where(l => l.IsActive && l.Status == "Success")
                .ToList();

            foreach (var session in activeSessions)
            {
                // Skip the current session if specified
                if (!string.IsNullOrEmpty(exceptSessionId) &&
                    session.Id.ToString() == exceptSessionId)
                {
                    continue;
                }

                session.LogoutTime = DateTime.Now;
                session.IsActive = false;
                session.FailureReason = $"Force logged out by {forcedBy}";
            }

            _db.SaveChanges();
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
    /// Login statistics model
    /// </summary>
    public class LoginStatistics
    {
        public int TotalLogins { get; set; }
        public int SuccessfulLogins { get; set; }
        public int FailedLogins { get; set; }
        public double SuccessRate { get; set; }
    }
}
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;

namespace RosalEHealthcare.Data.Services
{
    public class UserService
    {
        private readonly RosalEHealthcareDbContext _db;
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private const int LOCKOUT_DURATION_MINUTES = 5;
        private const int PASSWORD_HISTORY_COUNT = 5;

        public UserService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Basic CRUD

        public IEnumerable<User> GetAll()
        {
            return _db.Users.OrderByDescending(u => u.DateCreated).ToList();
        }

        public User GetById(int id)
        {
            return _db.Users.Find(id);
        }

        public User GetByEmail(string email)
        {
            return _db.Users.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
        }

        public User GetByUserCode(string userCode)
        {
            return _db.Users.FirstOrDefault(u => u.UserCode == userCode);
        }

        public User AddUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            // Generate UserCode if not provided
            if (string.IsNullOrEmpty(user.UserCode))
            {
                user.UserCode = GenerateUserCode();
            }

            // Set defaults
            user.DateCreated = DateTime.Now;
            user.Status = user.Status ?? "Active";
            user.FailedLoginAttempts = 0;
            user.PasswordChangedAt = DateTime.Now;

            _db.Users.Add(user);
            _db.SaveChanges();
            return user;
        }

        public void UpdateUser(User user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            user.ModifiedAt = DateTime.Now;

            var entry = _db.Entry(user);
            if (entry.State == EntityState.Detached)
            {
                _db.Users.Attach(user);
            }
            entry.State = EntityState.Modified;
            _db.SaveChanges();
        }

        public void DeleteUser(int id)
        {
            var user = GetById(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                _db.SaveChanges();
            }
        }

        #endregion

        #region User Code Generation

        public string GenerateUserCode()
        {
            var year = DateTime.Now.Year;
            var prefix = $"USR-{year}-";

            var lastUser = _db.Users
                .Where(u => u.UserCode != null && u.UserCode.StartsWith(prefix))
                .OrderByDescending(u => u.UserCode)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastUser != null && !string.IsNullOrEmpty(lastUser.UserCode))
            {
                var parts = lastUser.UserCode.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int lastNumber))
                {
                    nextNumber = lastNumber + 1;
                }
            }

            return $"USR-{year}-{nextNumber:D4}";
        }

        #endregion

        #region Authentication & Security

        public bool ValidateUser(string email, string plainPassword)
        {
            var user = GetByEmail(email);
            if (user == null) return false;

            // Check if account is locked
            if (IsAccountLocked(user))
            {
                return false;
            }

            // Verify password
            bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);

            if (isValid)
            {
                // Reset failed attempts on successful login
                user.FailedLoginAttempts = 0;
                user.LockoutEndTime = null;
                user.LastLogin = DateTime.Now;

                // Unlock if was locked
                if (user.Status == "Locked")
                {
                    user.Status = "Active";
                }

                UpdateUser(user);
            }
            else
            {
                // Increment failed attempts
                RecordFailedLogin(user);
            }

            return isValid;
        }

        public bool IsAccountLocked(User user)
        {
            if (user.Status == "Locked")
            {
                // Check if lockout has expired
                if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value <= DateTime.Now)
                {
                    // Unlock the account
                    user.Status = "Active";
                    user.FailedLoginAttempts = 0;
                    user.LockoutEndTime = null;
                    UpdateUser(user);
                    return false;
                }
                return true;
            }
            return false;
        }

        public void RecordFailedLogin(User user)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MAX_LOGIN_ATTEMPTS)
            {
                user.Status = "Locked";
                user.LockoutEndTime = DateTime.Now.AddMinutes(LOCKOUT_DURATION_MINUTES);
            }

            UpdateUser(user);
        }

        public void UnlockAccount(int userId)
        {
            var user = GetById(userId);
            if (user != null)
            {
                user.Status = "Active";
                user.FailedLoginAttempts = 0;
                user.LockoutEndTime = null;
                UpdateUser(user);
            }
        }

        public void LockAccount(int userId)
        {
            var user = GetById(userId);
            if (user != null)
            {
                user.Status = "Locked";
                user.LockoutEndTime = DateTime.Now.AddYears(100); // Permanent lock until manual unlock
                UpdateUser(user);
            }
        }

        #endregion

        #region Password Management

        public bool ChangePassword(int userId, string newPassword, string changedBy)
        {
            var user = GetById(userId);
            if (user == null) return false;

            // Check password history
            if (IsPasswordInHistory(user, newPassword))
            {
                return false; // Password was used recently
            }

            // Add current password to history before changing
            AddPasswordToHistory(user, user.PasswordHash);

            // Set new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordChangedAt = DateTime.Now;
            user.ModifiedBy = changedBy;
            user.ModifiedAt = DateTime.Now;

            // If status was Pending (first login), set to Active
            if (user.Status == "Pending")
            {
                user.Status = "Active";
            }

            UpdateUser(user);
            return true;
        }

        public string ResetPassword(int userId, string resetBy)
        {
            var user = GetById(userId);
            if (user == null) return null;

            // Generate temporary password
            string tempPassword = GenerateTemporaryPassword();

            // Add current password to history
            AddPasswordToHistory(user, user.PasswordHash);

            // Set new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword);
            user.PasswordChangedAt = DateTime.Now;
            user.Status = "Pending"; // Force password change on next login
            user.ModifiedBy = resetBy;
            user.ModifiedAt = DateTime.Now;

            UpdateUser(user);
            return tempPassword;
        }

        private bool IsPasswordInHistory(User user, string newPassword)
        {
            if (string.IsNullOrEmpty(user.PasswordHistory)) return false;

            try
            {
                var history = JsonConvert.DeserializeObject<List<string>>(user.PasswordHistory);
                if (history == null) return false;

                foreach (var oldHash in history)
                {
                    if (BCrypt.Net.BCrypt.Verify(newPassword, oldHash))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Invalid JSON, ignore
            }

            // Also check current password
            if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
            {
                return true;
            }

            return false;
        }

        private void AddPasswordToHistory(User user, string passwordHash)
        {
            List<string> history;

            try
            {
                history = string.IsNullOrEmpty(user.PasswordHistory)
                    ? new List<string>()
                    : JsonConvert.DeserializeObject<List<string>>(user.PasswordHistory) ?? new List<string>();
            }
            catch
            {
                history = new List<string>();
            }

            history.Insert(0, passwordHash);

            // Keep only last N passwords
            if (history.Count > PASSWORD_HISTORY_COUNT)
            {
                history = history.Take(PASSWORD_HISTORY_COUNT).ToList();
            }

            user.PasswordHistory = JsonConvert.SerializeObject(history);
        }

        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
            var random = new Random();
            var password = new char[12];

            // Ensure at least one of each type
            password[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[random.Next(24)];
            password[1] = "abcdefghjkmnpqrstuvwxyz"[random.Next(23)];
            password[2] = "23456789"[random.Next(8)];
            password[3] = "!@#$%"[random.Next(5)];

            // Fill the rest randomly
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            // Shuffle
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }

        #endregion

        #region Search & Filter

        public IEnumerable<User> Search(string query, string role = null, string status = null)
        {
            var q = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(u =>
                    u.FullName.ToLower().Contains(query) ||
                    u.Email.ToLower().Contains(query) ||
                    u.Username.ToLower().Contains(query) ||
                    (u.UserCode != null && u.UserCode.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All Roles")
            {
                q = q.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                q = q.Where(u => u.Status == status);
            }

            return q.OrderByDescending(u => u.DateCreated).ToList();
        }

        public IEnumerable<User> SearchPaged(string query, string role, string status, int pageNumber, int pageSize, string sortBy = "DateCreated", bool sortDescending = true)
        {
            var q = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(u =>
                    u.FullName.ToLower().Contains(query) ||
                    u.Email.ToLower().Contains(query) ||
                    u.Username.ToLower().Contains(query) ||
                    (u.UserCode != null && u.UserCode.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All Roles")
            {
                q = q.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                q = q.Where(u => u.Status == status);
            }

            // Sorting
            switch (sortBy)
            {
                case "FullName":
                    q = sortDescending ? q.OrderByDescending(u => u.FullName) : q.OrderBy(u => u.FullName);
                    break;
                case "Email":
                    q = sortDescending ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email);
                    break;
                case "Role":
                    q = sortDescending ? q.OrderByDescending(u => u.Role) : q.OrderBy(u => u.Role);
                    break;
                case "LastLogin":
                    q = sortDescending ? q.OrderByDescending(u => u.LastLogin) : q.OrderBy(u => u.LastLogin);
                    break;
                case "Status":
                    q = sortDescending ? q.OrderByDescending(u => u.Status) : q.OrderBy(u => u.Status);
                    break;
                default:
                    q = sortDescending ? q.OrderByDescending(u => u.DateCreated) : q.OrderBy(u => u.DateCreated);
                    break;
            }

            return q.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        }

        public int GetFilteredCount(string query, string role, string status)
        {
            var q = _db.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                q = q.Where(u =>
                    u.FullName.ToLower().Contains(query) ||
                    u.Email.ToLower().Contains(query) ||
                    (u.UserCode != null && u.UserCode.ToLower().Contains(query))
                );
            }

            if (!string.IsNullOrWhiteSpace(role) && role != "All Roles")
            {
                q = q.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status) && status != "All Status")
            {
                q = q.Where(u => u.Status == status);
            }

            return q.Count();
        }

        #endregion

        #region Statistics

        public int GetTotalUsers()
        {
            return _db.Users.Count();
        }

        public int GetUsersByRole(string role)
        {
            return _db.Users.Count(u => u.Role == role);
        }

        public int GetUsersByStatus(string status)
        {
            return _db.Users.Count(u => u.Status == status);
        }

        public int GetNewUsersThisMonth()
        {
            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            return _db.Users.Count(u => u.DateCreated >= startOfMonth);
        }

        public int GetNewUsersLastMonth()
        {
            var startOfLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
            var endOfLastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddDays(-1);
            return _db.Users.Count(u => u.DateCreated >= startOfLastMonth && u.DateCreated <= endOfLastMonth);
        }

        public int GetActiveUsersCount()
        {
            return _db.Users.Count(u => u.Status == "Active");
        }

        public int GetLockedUsersCount()
        {
            return _db.Users.Count(u => u.Status == "Locked");
        }

        #endregion

        #region Validation

        public bool IsEmailUnique(string email, int? excludeUserId = null)
        {
            var query = _db.Users.Where(u => u.Email.ToLower() == email.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !query.Any();
        }

        public bool CanDeleteUser(int userId, int currentUserId, out string reason)
        {
            reason = null;

            // Cannot delete yourself
            if (userId == currentUserId)
            {
                reason = "You cannot delete your own account.";
                return false;
            }

            var user = GetById(userId);
            if (user == null)
            {
                reason = "User not found.";
                return false;
            }

            // Cannot delete the last administrator
            if (user.Role == "Administrator")
            {
                var adminCount = _db.Users.Count(u => u.Role == "Administrator");
                if (adminCount <= 1)
                {
                    reason = "Cannot delete the last administrator account.";
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Legacy Support

        public void Register(string fullName, string email, string password, string role)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                UserCode = GenerateUserCode(),
                FullName = fullName,
                Email = email,
                PasswordHash = hashed,
                Role = role,
                Status = "Active",
                DateCreated = DateTime.Now,
                PasswordChangedAt = DateTime.Now
            };
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        #endregion

        #region Username Methods

        public User GetByUsername(string username)
        {
            return _db.Users.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
        }

        public bool IsUsernameUnique(string username, int? excludeUserId = null)
        {
            var query = _db.Users.Where(u => u.Username.ToLower() == username.ToLower());

            if (excludeUserId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUserId.Value);
            }

            return !query.Any();
        }

        /// <summary>
        /// Generate username from full name (firstname.lastname format)
        /// </summary>
        public string GenerateUsername(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return $"user{new Random().Next(1000, 9999)}";

            // Clean and split the name
            var cleanName = fullName.Trim().ToLower();
            var parts = cleanName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            string baseUsername;
            if (parts.Length >= 2)
            {
                // firstname.lastname
                baseUsername = $"{parts[0]}.{parts[parts.Length - 1]}";
            }
            else
            {
                baseUsername = parts[0];
            }

            // Remove special characters, keep only letters, numbers, dots, and underscores
            baseUsername = System.Text.RegularExpressions.Regex.Replace(baseUsername, @"[^a-z0-9._]", "");

            // Ensure it's unique
            string username = baseUsername;
            int counter = 1;
            while (!IsUsernameUnique(username))
            {
                username = $"{baseUsername}{counter}";
                counter++;
            }

            return username;
        }

        /// <summary>
        /// Validate user login by username
        /// </summary>
        public bool ValidateUserByUsername(string username, string plainPassword)
        {
            var user = GetByUsername(username);
            if (user == null) return false;

            // Check if account is locked
            if (IsAccountLocked(user))
            {
                return false;
            }

            // Verify password
            bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);

            if (isValid)
            {
                // Reset failed attempts on successful login
                user.FailedLoginAttempts = 0;
                user.LockoutEndTime = null;
                user.LastLogin = DateTime.Now;

                // Unlock if was locked
                if (user.Status == "Locked")
                {
                    user.Status = "Active";
                }

                UpdateUser(user);
            }
            else
            {
                // Increment failed attempts
                RecordFailedLogin(user);
            }

            return isValid;
        }

        #endregion
    }
}
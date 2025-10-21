using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class UserService
    {
        private readonly RosalEHealthcareDbContext _db;

        public UserService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        public User GetByEmail(string email)
        {
            return _db.Users.FirstOrDefault(u => u.Email == email);
        }

        public User GetById(int id)
        {
            return _db.Users.Find(id);
        }

        public User AddUser(User u)
        {
            if (u == null) throw new ArgumentNullException(nameof(u));
            _db.Users.Add(u);
            _db.SaveChanges();
            return u;
        }

        public void UpdateUser(User u)
        {
            if (u == null) throw new ArgumentNullException(nameof(u));
            // attach if not tracked
            var entry = _db.Entry(u);
            if (entry.State == System.Data.Entity.EntityState.Detached)
            {
                _db.Users.Attach(u);
            }
            entry.State = System.Data.Entity.EntityState.Modified;
            _db.SaveChanges();
        }

        public bool ValidateUser(string email, string plainPassword)
        {
            var u = GetByEmail(email);
            if (u == null) return false;

            // Verify hashed password using BCrypt
            return BCrypt.Net.BCrypt.Verify(plainPassword, u.PasswordHash);
        }

        public void Register(string fullName, string email, string password, string role)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = hashed,
                Role = role,
                DateCreated = DateTime.Now
            };

            _db.Users.Add(user);
            _db.SaveChanges();
        }

    }
}

using System.Linq;
using BCrypt.Net;
using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Core.Services;
using RosalEHealthcare.Data.Contexts;

namespace RosalEHealthcare.Data.Services
{
    public class UserService : IUserService
    {
        private readonly RosalEHealthcareDbContext _db;
        public UserService(RosalEHealthcareDbContext db) => _db = db;

        public void Register(string fullName, string email, string password, string role)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = hashed,
                Role = role
            };
            _db.Users.Add(user);
            _db.SaveChanges();
        }

        public bool ValidateUser(string email, string password)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            return user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
    }
}

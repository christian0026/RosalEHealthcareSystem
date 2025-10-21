using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.Core.Services
{
    public interface IUserService
    {
        User GetByEmail(string email);
        User GetById(int id);
        User AddUser(User u);
        void UpdateUser(User u);
        bool ValidateUser(string email, string plainPassword);
        void Register(string fullName, string email, string password, string role);
    }
}





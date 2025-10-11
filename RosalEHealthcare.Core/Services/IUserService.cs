using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosalEHealthcare.Core.Services
{
    public interface IUserService
    {
        void Register(string fullName, string email, string password, string role);
        bool ValidateUser(string email, string password);
    }
}




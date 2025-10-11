using System.Data.Entity;
using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.Data.Contexts
{
    public class RosalEHealthcareDbContext : DbContext
    {
        public RosalEHealthcareDbContext() : base("name=RosalEHealthcareConnection") { }
        public DbSet<User> Users { get; set; }
    }
}


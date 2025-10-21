using System.Data.Entity;
using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.Data.Contexts
{
    public class RosalEHealthcareDbContext : DbContext
    {
        public RosalEHealthcareDbContext()
            : base("name=RosalEHealthcareDbConnection")
        {
            Database.SetInitializer<RosalEHealthcareDbContext>(null);
        }

        public DbSet<User> Users { get; set; }
    }
}

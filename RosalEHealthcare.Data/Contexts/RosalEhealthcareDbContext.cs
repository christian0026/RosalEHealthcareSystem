using LiveChartsCore.Painting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RosalEHealthcare.Core.Models;

namespace RosalEHealthcare.Data.Contexts
{
    public class RosalEHealthcareDbContext : DbContext
    {
        public RosalEHealthcareDbContext()
            : base("name=RosalEHealthcareConnection") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
    }
}

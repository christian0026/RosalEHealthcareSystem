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

        // Existing DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<PrescriptionMedicine> PrescriptionMedicines { get; set; }
        public DbSet<Medicine> Medicines { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<MedicalHistory> MedicalHistories { get; set; }
        public DbSet<MedicalDocument> MedicalDocuments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PrescriptionTemplate> PrescriptionTemplates { get; set; }

        // NEW DbSets for System Settings
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<BackupHistory> BackupHistories { get; set; }
        public DbSet<LoginHistory> LoginHistories { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MedicalHistory>()
                .HasRequired<Patient>(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<MedicalDocument>()
                .HasRequired<Patient>(m => m.Patient)
                .WithMany()
                .HasForeignKey(m => m.PatientId)
                .WillCascadeOnDelete(false);
        }
    }
}
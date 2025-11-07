using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }                 // PK
        public string PatientId { get; set; }      // PT-001
        public string FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }         // Male / Female / Other
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }

        // Medical fields (simple inferred fields)
        public string PrimaryDiagnosis { get; set; }
        public string SecondaryDiagnosis { get; set; }
        public string Allergies { get; set; }
        public string BloodType { get; set; }

        public DateTime? LastVisit { get; set; }
        public string Status { get; set; }         // Active / Archived / Follow-up / etc.
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}

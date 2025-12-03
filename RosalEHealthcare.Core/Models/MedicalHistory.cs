using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("MedicalHistory")]
    public class MedicalHistory
    {
        [Key]
        public int Id { get; set; }

        public int PatientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Diagnosis { get; set; }

        [MaxLength(500)]
        public string Treatment { get; set; }

        [MaxLength(100)]
        public string DoctorName { get; set; }

        public DateTime VisitDate { get; set; }

        [MaxLength(50)]
        public string VisitType { get; set; } // Regular, Follow-up, Emergency

        // Vital Signs
        public string BloodPressure { get; set; } // e.g., "120/80"
        public decimal? Temperature { get; set; } // Celsius
        public int? HeartRate { get; set; } // BPM
        public int? RespiratoryRate { get; set; }
        public decimal? Weight { get; set; } // kg
        public decimal? Height { get; set; } // cm

        // Lab Results
        [MaxLength(100)]
        public string LabTestName { get; set; }
        public string LabTestResult { get; set; }
        public DateTime? LabTestDate { get; set; }

        // Clinical Notes
        public string Symptoms { get; set; }
        public string ClinicalNotes { get; set; }
        public string Recommendations { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } // Mild, Moderate, Severe

        public bool FollowUpRequired { get; set; }
        public DateTime? NextFollowUpDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        // Link to appointment
        public int? AppointmentId { get; set; }
        public virtual Appointment Appointment { get; set; }

        // Navigation
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }
    }
}
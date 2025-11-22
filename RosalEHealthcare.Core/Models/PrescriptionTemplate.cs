using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class PrescriptionTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string TemplateName { get; set; }

        public int? DoctorId { get; set; }

        public string DiagnosisTemplate { get; set; }
        public string PrimaryDiagnosis { get; set; }
        public string SecondaryDiagnosis { get; set; }

        [MaxLength(50)]
        public string ConditionSeverity { get; set; }

        public string MedicinesJson { get; set; } // JSON array of medicines
        public string InstructionsTemplate { get; set; }

        public bool FollowUpRequired { get; set; }

        [MaxLength(50)]
        public string PriorityLevel { get; set; }

        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastUsedAt { get; set; }

        public int UsageCount { get; set; } = 0;
    }
}
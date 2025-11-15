using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("Notifications")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } // NewPatient, AppointmentReminder, LowStock, SystemAlert

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [MaxLength(100)]
        public string TargetUser { get; set; } // Username or "All"

        [MaxLength(50)]
        public string TargetRole { get; set; } // Doctor, Receptionist, Admin, or "All"

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ReadAt { get; set; }

        [MaxLength(200)]
        public string ActionUrl { get; set; } // Navigation target

        [MaxLength(50)]
        public string Priority { get; set; } // Low, Normal, High, Urgent

        public string RelatedEntityId { get; set; } // PatientId, AppointmentId, etc.

        [MaxLength(50)]
        public string Icon { get; set; } // Icon name for UI

        [MaxLength(50)]
        public string Color { get; set; } // Color code for UI

        // Helper property
        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - CreatedAt;
                if (timeSpan.TotalSeconds < 60)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                return CreatedAt.ToString("MMM dd");
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosalEHealthcare.Core.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string AppointmentId { get; set; }

        // Make PatientId nullable (appointments can be scheduled before patient is registered)
        public int? PatientId { get; set; }

        // Patient Information (stored directly in appointment)
        public string PatientName { get; set; }
        public string Contact { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Gender { get; set; }

        // Appointment Details
        public string Type { get; set; }
        public string Condition { get; set; } // Chief Complaint
        public string Status { get; set; } // PENDING, CONFIRMED, COMPLETED, CANCELLED
        public DateTime Time { get; set; }
        public DateTime? LastVisit { get; set; }

        // Metadata
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        // Add this navigation property at the end of the Appointment class
        public virtual Patient Patient { get; set; }
        public DateTime? ConsultationStartedAt { get; set; }
        public DateTime? ConsultationCompletedAt { get; set; }

        // Computed property for consultation duration
        public string ConsultationDuration
        {
            get
            {
                if (ConsultationStartedAt.HasValue && ConsultationCompletedAt.HasValue)
                {
                    var duration = ConsultationCompletedAt.Value - ConsultationStartedAt.Value;
                    return $"{(int)duration.TotalMinutes} mins";
                }
                return null;
            }
        }
    }
}

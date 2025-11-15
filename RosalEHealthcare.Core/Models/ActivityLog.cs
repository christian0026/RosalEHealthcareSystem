using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string ActivityType { get; set; }

        [Required, MaxLength(500)]
        public string Description { get; set; }

        [Required, MaxLength(50)]
        public string Module { get; set; }

        [Required, MaxLength(100)]
        public string PerformedBy { get; set; }

        [MaxLength(50)]
        public string PerformedByRole { get; set; }

        [MaxLength(50)]
        public string RelatedEntityId { get; set; }

        public DateTime PerformedAt { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string IpAddress { get; set; }

        public string AdditionalData { get; set; }

        // Helper property for time ago display
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.Now - PerformedAt;

                if (timeSpan.TotalSeconds < 60)
                    return "Just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes} minute{((int)timeSpan.TotalMinutes > 1 ? "s" : "")} ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours > 1 ? "s" : "")} ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays > 1 ? "s" : "")} ago";

                return PerformedAt.ToString("MMM dd, yyyy");
            }
        }
    }
}
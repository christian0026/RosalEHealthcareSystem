using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("LoginHistory")]
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        [Required, MaxLength(100)]
        public string Username { get; set; }

        [MaxLength(200)]
        public string FullName { get; set; }

        [MaxLength(50)]
        public string Role { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public DateTime? LogoutTime { get; set; }

        public int? SessionDuration { get; set; } // Duration in minutes

        [MaxLength(50)]
        public string IpAddress { get; set; }

        [MaxLength(100)]
        public string MachineName { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } // Success, Failed, Locked, Expired

        [MaxLength(500)]
        public string FailureReason { get; set; }

        public bool IsActive { get; set; } = false;

        [MaxLength(100)]
        public string SessionId { get; set; }

        #region Computed Properties

        [NotMapped]
        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case "Success":
                        return "✓";
                    case "Failed":
                        return "✕";
                    case "Locked":
                        return "🔒";
                    case "Expired":
                        return "⏱";
                    default:
                        return "•";
                }
            }
        }

        [NotMapped]
        public string SessionDurationFormatted
        {
            get
            {
                if (!SessionDuration.HasValue) return "-";

                var minutes = SessionDuration.Value;
                if (minutes < 60)
                    return $"{minutes} min";

                var hours = minutes / 60;
                var remainingMinutes = minutes % 60;

                return remainingMinutes > 0
                    ? $"{hours}h {remainingMinutes}m"
                    : $"{hours}h";
            }
        }

        [NotMapped]
        public string LoginTimeFormatted => LoginTime.ToString("MMM dd, yyyy HH:mm:ss");

        [NotMapped]
        public string LogoutTimeFormatted => LogoutTime?.ToString("MMM dd, yyyy HH:mm:ss") ?? "-";

        #endregion
    }
}
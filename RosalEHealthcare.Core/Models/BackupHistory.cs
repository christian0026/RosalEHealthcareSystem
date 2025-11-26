using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("BackupHistory")]
    public class BackupHistory
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(500)]
        public string FileName { get; set; }

        [Required, MaxLength(1000)]
        public string FilePath { get; set; }

        public long? FileSize { get; set; } // Size in bytes

        [Required, MaxLength(50)]
        public string BackupType { get; set; } // Manual, Scheduled, BeforeRestore

        [Required, MaxLength(50)]
        public string Status { get; set; } // Success, Failed, InProgress, Cancelled

        public bool IsEncrypted { get; set; } = false;

        public bool IsCompressed { get; set; } = false;

        public DateTime StartedAt { get; set; } = DateTime.Now;

        public DateTime? CompletedAt { get; set; }

        [MaxLength(200)]
        public string CreatedBy { get; set; }

        [MaxLength(1000)]
        public string Notes { get; set; }

        public string ErrorMessage { get; set; }

        #region Computed Properties

        [NotMapped]
        public string FileSizeFormatted
        {
            get
            {
                if (!FileSize.HasValue || FileSize.Value == 0) return "Unknown";

                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                double len = FileSize.Value;
                int order = 0;

                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }

                return $"{len:0.##} {sizes[order]}";
            }
        }

        [NotMapped]
        public string Duration
        {
            get
            {
                if (!CompletedAt.HasValue) return "In Progress";
                var duration = CompletedAt.Value - StartedAt;

                if (duration.TotalSeconds < 60)
                    return $"{duration.TotalSeconds:0} seconds";
                if (duration.TotalMinutes < 60)
                    return $"{duration.TotalMinutes:0} minutes";

                return $"{duration.TotalHours:0.#} hours";
            }
        }

        [NotMapped]
        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case "Success": return "✓";
                    case "Failed": return "✗";
                    case "InProgress": return "⟳";
                    case "Cancelled": return "○";
                    default: return "?";
                }
            }
        }

        #endregion
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("MedicalDocuments")]
    public class MedicalDocument
    {
        [Key]
        public int Id { get; set; }

        public int PatientId { get; set; }

        [Required]
        [MaxLength(200)]
        public string DocumentName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } // Lab Result, X-Ray, CT Scan, MRI, Prescription, Other

        [MaxLength(500)]
        public string Description { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } // Stored in: Documents/Patients/{PatientId}/{Category}/

        [MaxLength(10)]
        public string FileExtension { get; set; } // .pdf

        public long FileSize { get; set; } // in bytes

        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string UploadedBy { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } // Active, Archived

        // Navigation
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        // Helper property
        [NotMapped]
        public string FileSizeFormatted
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                double len = FileSize;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return string.Format("{0:0.##} {1}", len, sizes[order]);
            }
        }
    }
}
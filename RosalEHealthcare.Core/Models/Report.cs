using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("Reports")]
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReportId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReportType { get; set; }

        public string Description { get; set; }

        public DateTime DateGenerated { get; set; }

        [MaxLength(100)]
        public string GeneratedBy { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(50)]
        public string Format { get; set; }

        public string FilePath { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        public string ReportData { get; set; } // JSON data
        public string Parameters { get; set; }
    }
}
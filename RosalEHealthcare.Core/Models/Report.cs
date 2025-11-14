using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Report
    {
        [Key]
        public int Id { get; set; }
        public string ReportId { get; set; }
        public string ReportType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime DateGenerated { get; set; }
        public string GeneratedBy { get; set; }
        public string Status { get; set; }
        public string FilePath { get; set; }
        public string Parameters { get; set; }
    }
}
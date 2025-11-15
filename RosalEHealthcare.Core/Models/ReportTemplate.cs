using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("ReportTemplates")]
    public class ReportTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string TemplateName { get; set; }

        [MaxLength(100)]
        public string TemplateType { get; set; }

        public string Description { get; set; }

        public string TemplateContent { get; set; } // JSON or XML template content

        [MaxLength(100)]
        public string CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
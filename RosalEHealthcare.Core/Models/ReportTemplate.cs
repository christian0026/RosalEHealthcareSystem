using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class ReportTemplate
    {
        [Key]
        public int Id { get; set; }
        public string TemplateName { get; set; }
        public string TemplateType { get; set; }
        public string Description { get; set; }
        public string TemplateContent { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
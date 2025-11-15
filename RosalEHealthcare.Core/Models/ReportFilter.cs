using System;

namespace RosalEHealthcare.Core.Models
{
    public class ReportFilter
    {
        public string ReportType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string DateRange { get; set; }
        public string Format { get; set; }
        public string Status { get; set; }
        public string Category { get; set; }
    }
}
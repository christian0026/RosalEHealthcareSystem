using System;
using System.Collections.Generic;

namespace RosalEHealthcare.Core.Models
{
    public class ReportData
    {
        public string Title { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string GeneratedBy { get; set; }
        public Dictionary<string, object> Data { get; set; }
        public List<ReportStatistic> Statistics { get; set; }

        public ReportData()
        {
            Data = new Dictionary<string, object>();
            Statistics = new List<ReportStatistic>();
        }
    }

    public class ReportStatistic
    {
        public string Label { get; set; }
        public object Value { get; set; }
        public string Category { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public string Color { get; set; }
    }
}
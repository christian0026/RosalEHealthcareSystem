using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosalEHealthcare.Core.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string Type { get; set; }
        public string Condition { get; set; }
        public string Status { get; set; }
        public DateTime Time { get; set; }
        public DateTime? LastVisit { get; set; }
        public string Contact { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

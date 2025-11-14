using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosalEHealthcare.Core.Models
{
    public class Prescription
    {
        public int Id { get; set; } // PK
        public string PrescriptionId { get; set; } // e.g. PR-2025-0001
        public int PatientId { get; set; }
        public string PatientName { get; set; }

        public string PrimaryDiagnosis { get; set; }
        public string SecondaryDiagnosis { get; set; }
        public string ConditionSeverity { get; set; } // e.g. Mild / Moderate / Severe

        public string SpecialInstructions { get; set; }
        public bool FollowUpRequired { get; set; }
        public DateTime? NextAppointment { get; set; }
        public string PriorityLevel { get; set; } // Routine / Urgent / High

        public string CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<PrescriptionMedicine> Medicines { get; set; } = new List<PrescriptionMedicine>();

        public Prescription()
        {
            Medicines = new List<PrescriptionMedicine>();
        }
    }
}

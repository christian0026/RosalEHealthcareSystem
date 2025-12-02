using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosalEHealthcare.Core.Models
{
    public class PrescriptionMedicine
    {
        public int Id { get; set; } // PK
        public int PrescriptionId { get; set; } // FK to Prescription
        public int? MedicineId { get; set; } // optional link to your Medicines table
        public string MedicineName { get; set; }
        public string Dosage { get; set; } // e.g. "1 tablet"
        public string Frequency { get; set; } // e.g. "Once daily"
        public string Duration { get; set; } // e.g. "30 days"
        public int? Quantity { get; set; }
        public string Route { get; set; } // e.g. Oral, IV

        public virtual Prescription Prescription { get; set; }
        public virtual Medicine Medicine { get; set; }
    }
}

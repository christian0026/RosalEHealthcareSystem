using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(10)]
        public string Gender { get; set; }

        public DateTime BirthDate { get; set; }

        [MaxLength(15)]
        public string ContactNumber { get; set; }

        [MaxLength(255)]
        public string Condition { get; set; }
    }
}

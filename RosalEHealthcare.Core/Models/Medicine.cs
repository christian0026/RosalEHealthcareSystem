using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Medicine
    {
        [Key]
        public int MedicineId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        public int Stock { get; set; }

        public decimal Price { get; set; }

        public DateTime ExpiryDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }
    }
}

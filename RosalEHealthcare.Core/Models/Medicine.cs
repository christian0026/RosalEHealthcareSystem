using System;
using System.ComponentModel.DataAnnotations;

namespace RosalEHealthcare.Core.Models
{
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        public string MedicineId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(50)]
        public string Category { get; set; }

        public int Stock { get; set; }

        public decimal Price { get; set; }

        public DateTime ExpiryDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; }

        // Extended Properties
        public string GenericName { get; set; }
        public string Brand { get; set; }
        public string Type { get; set; }
        public string Strength { get; set; }
        public string Unit { get; set; }

        // New Columns for Archive & Audit
        public int MinimumStockLevel { get; set; } = 10;

        [MaxLength(100)]
        public string LastModifiedBy { get; set; }

        public DateTime? LastModifiedAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string Notes { get; set; }
    }
}
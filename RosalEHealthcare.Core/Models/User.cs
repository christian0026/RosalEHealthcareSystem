using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("Id")] // Maps explicitly to the Id column in SQL
        public int Id { get; set; }

        // Optional: This is a business-generated user code (not PK)
        [StringLength(50)]
        public string UserCode { get; set; } // Renamed from UserID to avoid EF confusion

        [Required, StringLength(200)]
        public string FullName { get; set; }

        [Required, StringLength(200)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        public string Role { get; set; } // Administrator / Doctor / Receptionist

        [StringLength(50)]
        public string Status { get; set; } // Active / Inactive

        public DateTime? LastLogin { get; set; }

        [Column("DateCreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        public string ProfileImagePath { get; set; } // Path to profile image
    }
}

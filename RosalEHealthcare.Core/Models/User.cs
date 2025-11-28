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
        [Column("Id")]
        public int Id { get; set; }

        [StringLength(50)]
        public string UserCode { get; set; }

        /// <summary>
        /// Username for login (unique, lowercase, no spaces)
        /// Format: firstname.lastname or custom
        /// </summary>
        [Required, StringLength(100)]
        public string Username { get; set; }

        [Required, StringLength(200)]
        public string FullName { get; set; }

        [Required, StringLength(200)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [StringLength(50)]
        public string Role { get; set; }

        [StringLength(50)]
        public string Status { get; set; }

        public DateTime? LastLogin { get; set; }

        [Column("DateCreated")]
        public DateTime DateCreated { get; set; } = DateTime.Now;

        // Add this alias property for compatibility
        [NotMapped]
        public DateTime CreatedAt
        {
            get => DateCreated;
            set => DateCreated = value;
        }

        public string ProfileImagePath { get; set; }

        // Add these contact properties
        [StringLength(100)]
        public string Contact { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        // Security Properties
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockoutEndTime { get; set; }
        public DateTime? PasswordChangedAt { get; set; }
        public string PasswordHistory { get; set; }

        [StringLength(200)]
        public string CreatedBy { get; set; }

        public DateTime? ModifiedAt { get; set; }

        [StringLength(200)]
        public string ModifiedBy { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Computed Properties
        [NotMapped]
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "?";
                var parts = FullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{parts[0][0]}{parts[parts.Length - 1][0]}".ToUpper();
                return FullName.Length >= 2 ? FullName.Substring(0, 2).ToUpper() : FullName.ToUpper();
            }
        }

        [NotMapped]
        public bool IsLocked => Status == "Locked" || (LockoutEndTime.HasValue && LockoutEndTime.Value > DateTime.Now);

        [NotMapped]
        public string LastLoginFormatted => LastLogin.HasValue ? LastLogin.Value.ToString("MMM dd, yyyy HH:mm") : "Never";
    }
}
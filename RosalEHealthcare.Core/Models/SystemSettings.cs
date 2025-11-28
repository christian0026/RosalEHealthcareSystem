using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("SystemSettings")]
    public class SystemSetting
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string SettingKey { get; set; }

        public string SettingValue { get; set; }

        [Required, MaxLength(50)]
        public string SettingType { get; set; } = "String"; // String, Int, Bool, DateTime, Json

        [MaxLength(50)]
        public string DataType { get; set; } = "String"; // Add this property

        [Required, MaxLength(50)]
        public string Category { get; set; } // General, Security, Backup, Notification, Database, Application

        [MaxLength(500)]
        public string Description { get; set; }

        public DateTime LastModified { get; set; } = DateTime.Now;

        [MaxLength(200)]
        public string ModifiedBy { get; set; }

        // Add these new properties
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        #region Helper Methods

        /// <summary>
        /// Get value as string
        /// </summary>
        public string GetString() => SettingValue ?? "";

        /// <summary>
        /// Get value as integer
        /// </summary>
        public int GetInt(int defaultValue = 0)
        {
            return int.TryParse(SettingValue, out int result) ? result : defaultValue;
        }

        /// <summary>
        /// Get value as boolean
        /// </summary>
        public bool GetBool(bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(SettingValue)) return defaultValue;
            return SettingValue.ToLower() == "true" || SettingValue == "1";
        }

        /// <summary>
        /// Get value as DateTime
        /// </summary>
        public DateTime? GetDateTime()
        {
            return DateTime.TryParse(SettingValue, out DateTime result) ? result : (DateTime?)null;
        }

        #endregion
    }
}
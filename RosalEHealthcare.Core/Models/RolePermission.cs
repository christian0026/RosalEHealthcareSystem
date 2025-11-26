using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RosalEHealthcare.Core.Models
{
    [Table("RolePermissions")]
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; } // Administrator, Doctor, Receptionist

        [Required, MaxLength(100)]
        public string Module { get; set; } // PatientManagement, Appointments, etc.

        public bool CanView { get; set; } = false;

        public bool CanCreate { get; set; } = false;

        public bool CanEdit { get; set; } = false;

        public bool CanDelete { get; set; } = false;

        public bool CanExport { get; set; } = false;

        public DateTime LastModified { get; set; } = DateTime.Now;

        [MaxLength(200)]
        public string ModifiedBy { get; set; }

        #region Computed Properties

        [NotMapped]
        public string PermissionLevel
        {
            get
            {
                if (CanView && CanCreate && CanEdit && CanDelete && CanExport)
                    return "Full Access";
                if (CanView && CanCreate && CanEdit)
                    return "Read & Write";
                if (CanView && CanCreate)
                    return "View & Create";
                if (CanView)
                    return "View Only";
                return "No Access";
            }
        }

        [NotMapped]
        public string ModuleDisplayName
        {
            get
            {
                switch (Module)
                {
                    case "Dashboard": return "Dashboard";
                    case "PatientManagement": return "Patient Management";
                    case "Appointments": return "Appointments";
                    case "MedicineInventory": return "Medicine Inventory";
                    case "Prescriptions": return "Prescriptions";
                    case "UserManagement": return "User Management";
                    case "Reports": return "Reports & Analytics";
                    case "SystemSettings": return "System Settings";
                    default: return Module;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if user has specific permission
        /// </summary>
        public bool HasPermission(string permissionType)
        {
            switch (permissionType?.ToLower())
            {
                case "view": return CanView;
                case "create": return CanCreate;
                case "edit": return CanEdit;
                case "delete": return CanDelete;
                case "export": return CanExport;
                default: return false;
            }
        }

        /// <summary>
        /// Set all permissions at once
        /// </summary>
        public void SetAllPermissions(bool value)
        {
            CanView = value;
            CanCreate = value;
            CanEdit = value;
            CanDelete = value;
            CanExport = value;
        }

        /// <summary>
        /// Copy permissions from another RolePermission
        /// </summary>
        public void CopyPermissionsFrom(RolePermission source)
        {
            CanView = source.CanView;
            CanCreate = source.CanCreate;
            CanEdit = source.CanEdit;
            CanDelete = source.CanDelete;
            CanExport = source.CanExport;
        }

        #endregion
    }
}
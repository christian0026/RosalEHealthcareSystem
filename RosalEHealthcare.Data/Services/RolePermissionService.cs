using RosalEHealthcare.Core.Models;
using RosalEHealthcare.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RosalEHealthcare.Data.Services
{
    public class RolePermissionService
    {
        private readonly RosalEHealthcareDbContext _db;
        private static Dictionary<string, Dictionary<string, RolePermission>> _cache;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private const int CACHE_MINUTES = 5;

        // Available modules in the system
        public static readonly string[] Modules = new[]
        {
            "Dashboard",
            "PatientManagement",
            "Appointments",
            "MedicineInventory",
            "Prescriptions",
            "UserManagement",
            "Reports",
            "SystemSettings"
        };

        // Available roles
        public static readonly string[] Roles = new[]
        {
            "Administrator",
            "Doctor",
            "Receptionist"
        };

        public RolePermissionService(RosalEHealthcareDbContext db)
        {
            _db = db;
        }

        #region Cache Management

        private void RefreshCacheIfNeeded()
        {
            if (_cache == null || DateTime.Now > _cacheExpiry)
            {
                RefreshCache();
            }
        }

        public void RefreshCache()
        {
            try
            {
                var permissions = _db.RolePermissions.ToList();
                _cache = new Dictionary<string, Dictionary<string, RolePermission>>();

                foreach (var perm in permissions)
                {
                    if (!_cache.ContainsKey(perm.RoleName))
                    {
                        _cache[perm.RoleName] = new Dictionary<string, RolePermission>();
                    }
                    _cache[perm.RoleName][perm.Module] = perm;
                }

                _cacheExpiry = DateTime.Now.AddMinutes(CACHE_MINUTES);
            }
            catch
            {
                _cache = new Dictionary<string, Dictionary<string, RolePermission>>();
            }
        }

        public void ClearCache()
        {
            _cache = null;
            _cacheExpiry = DateTime.MinValue;
        }

        #endregion

        #region Get Permissions

        /// <summary>
        /// Get all role permissions
        /// </summary>
        public IEnumerable<RolePermission> GetAll()
        {
            return _db.RolePermissions
                .OrderBy(p => p.RoleName)
                .ThenBy(p => p.Module)
                .ToList();
        }

        /// <summary>
        /// Get permissions for a specific role
        /// </summary>
        public IEnumerable<RolePermission> GetByRole(string roleName)
        {
            return _db.RolePermissions
                .Where(p => p.RoleName == roleName)
                .OrderBy(p => p.Module)
                .ToList();
        }

        /// <summary>
        /// Get permission for a specific role and module
        /// </summary>
        public RolePermission GetPermission(string roleName, string module)
        {
            RefreshCacheIfNeeded();

            if (_cache.TryGetValue(roleName, out var rolePerms))
            {
                if (rolePerms.TryGetValue(module, out var perm))
                {
                    return perm;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if a role has a specific permission on a module
        /// </summary>
        public bool HasPermission(string roleName, string module, string permissionType)
        {
            var permission = GetPermission(roleName, module);
            return permission?.HasPermission(permissionType) ?? false;
        }

        /// <summary>
        /// Check if role can view a module
        /// </summary>
        public bool CanView(string roleName, string module)
        {
            return HasPermission(roleName, module, "view");
        }

        /// <summary>
        /// Check if role can create in a module
        /// </summary>
        public bool CanCreate(string roleName, string module)
        {
            return HasPermission(roleName, module, "create");
        }

        /// <summary>
        /// Check if role can edit in a module
        /// </summary>
        public bool CanEdit(string roleName, string module)
        {
            return HasPermission(roleName, module, "edit");
        }

        /// <summary>
        /// Check if role can delete in a module
        /// </summary>
        public bool CanDelete(string roleName, string module)
        {
            return HasPermission(roleName, module, "delete");
        }

        /// <summary>
        /// Check if role can export from a module
        /// </summary>
        public bool CanExport(string roleName, string module)
        {
            return HasPermission(roleName, module, "export");
        }

        #endregion

        #region Update Permissions

        /// <summary>
        /// Update a single permission
        /// </summary>
        public void UpdatePermission(string roleName, string module, bool canView, bool canCreate, bool canEdit, bool canDelete, bool canExport, string modifiedBy = null)
        {
            var permission = _db.RolePermissions
                .FirstOrDefault(p => p.RoleName == roleName && p.Module == module);

            if (permission != null)
            {
                permission.CanView = canView;
                permission.CanCreate = canCreate;
                permission.CanEdit = canEdit;
                permission.CanDelete = canDelete;
                permission.CanExport = canExport;
                permission.LastModified = DateTime.Now;
                permission.ModifiedBy = modifiedBy;
            }
            else
            {
                permission = new RolePermission
                {
                    RoleName = roleName,
                    Module = module,
                    CanView = canView,
                    CanCreate = canCreate,
                    CanEdit = canEdit,
                    CanDelete = canDelete,
                    CanExport = canExport,
                    LastModified = DateTime.Now,
                    ModifiedBy = modifiedBy
                };
                _db.RolePermissions.Add(permission);
            }

            _db.SaveChanges();
            ClearCache();
        }

        /// <summary>
        /// Update permission using quick access level
        /// </summary>
        public void SetAccessLevel(string roleName, string module, string accessLevel, string modifiedBy = null)
        {
            bool canView = false, canCreate = false, canEdit = false, canDelete = false, canExport = false;

            switch (accessLevel)
            {
                case "Full Access":
                    canView = canCreate = canEdit = canDelete = canExport = true;
                    break;
                case "Read & Write":
                    canView = canCreate = canEdit = true;
                    canExport = true;
                    break;
                case "View & Create":
                    canView = canCreate = true;
                    canExport = true;
                    break;
                case "View Only":
                    canView = true;
                    break;
                case "No Access":
                default:
                    // All false
                    break;
            }

            UpdatePermission(roleName, module, canView, canCreate, canEdit, canDelete, canExport, modifiedBy);
        }

        /// <summary>
        /// Update all permissions for a role
        /// </summary>
        public void UpdateRolePermissions(string roleName, List<RolePermission> permissions, string modifiedBy = null)
        {
            foreach (var perm in permissions)
            {
                UpdatePermission(roleName, perm.Module, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete, perm.CanExport, modifiedBy);
            }
        }

        #endregion

        #region Reset to Defaults

        /// <summary>
        /// Reset permissions for a role to defaults
        /// </summary>
        public void ResetRoleToDefault(string roleName, string modifiedBy = null)
        {
            var defaults = GetDefaultPermissions(roleName);

            foreach (var perm in defaults)
            {
                UpdatePermission(roleName, perm.Module, perm.CanView, perm.CanCreate, perm.CanEdit, perm.CanDelete, perm.CanExport, modifiedBy);
            }
        }

        /// <summary>
        /// Reset all roles to defaults
        /// </summary>
        public void ResetAllToDefaults(string modifiedBy = null)
        {
            foreach (var role in Roles)
            {
                ResetRoleToDefault(role, modifiedBy);
            }
        }

        /// <summary>
        /// Get default permissions for a role
        /// </summary>
        private List<RolePermission> GetDefaultPermissions(string roleName)
        {
            var permissions = new List<RolePermission>();

            switch (roleName)
            {
                case "Administrator":
                    foreach (var module in Modules)
                    {
                        permissions.Add(new RolePermission
                        {
                            RoleName = roleName,
                            Module = module,
                            CanView = true,
                            CanCreate = true,
                            CanEdit = true,
                            CanDelete = true,
                            CanExport = true
                        });
                    }
                    break;

                case "Doctor":
                    permissions.Add(CreatePermission(roleName, "Dashboard", true, false, false, false, true));
                    permissions.Add(CreatePermission(roleName, "PatientManagement", true, true, true, false, true));
                    permissions.Add(CreatePermission(roleName, "Appointments", true, true, true, true, true));
                    permissions.Add(CreatePermission(roleName, "MedicineInventory", true, false, false, false, false));
                    permissions.Add(CreatePermission(roleName, "Prescriptions", true, true, true, true, true));
                    permissions.Add(CreatePermission(roleName, "UserManagement", false, false, false, false, false));
                    permissions.Add(CreatePermission(roleName, "Reports", true, true, false, false, true));
                    permissions.Add(CreatePermission(roleName, "SystemSettings", false, false, false, false, false));
                    break;

                case "Receptionist":
                    permissions.Add(CreatePermission(roleName, "Dashboard", true, false, false, false, false));
                    permissions.Add(CreatePermission(roleName, "PatientManagement", true, true, true, false, true));
                    permissions.Add(CreatePermission(roleName, "Appointments", true, true, true, true, true));
                    permissions.Add(CreatePermission(roleName, "MedicineInventory", true, false, false, false, false));
                    permissions.Add(CreatePermission(roleName, "Prescriptions", true, false, false, false, true));
                    permissions.Add(CreatePermission(roleName, "UserManagement", false, false, false, false, false));
                    permissions.Add(CreatePermission(roleName, "Reports", true, false, false, false, true));
                    permissions.Add(CreatePermission(roleName, "SystemSettings", false, false, false, false, false));
                    break;
            }

            return permissions;
        }

        private RolePermission CreatePermission(string role, string module, bool view, bool create, bool edit, bool delete, bool export)
        {
            return new RolePermission
            {
                RoleName = role,
                Module = module,
                CanView = view,
                CanCreate = create,
                CanEdit = edit,
                CanDelete = delete,
                CanExport = export
            };
        }

        #endregion

        #region Permission Matrix

        /// <summary>
        /// Get complete permission matrix for all roles and modules
        /// </summary>
        public Dictionary<string, Dictionary<string, RolePermission>> GetPermissionMatrix()
        {
            RefreshCacheIfNeeded();
            return _cache ?? new Dictionary<string, Dictionary<string, RolePermission>>();
        }

        /// <summary>
        /// Get modules that a role can access
        /// </summary>
        public IEnumerable<string> GetAccessibleModules(string roleName)
        {
            return _db.RolePermissions
                .Where(p => p.RoleName == roleName && p.CanView)
                .Select(p => p.Module)
                .ToList();
        }

        #endregion
    }
}
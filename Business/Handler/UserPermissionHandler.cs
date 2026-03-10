using Business.Data;
using Business.Model;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers
{
    public class UserPermissionHandler
    {
        private readonly DBContext _ctx;

        public UserPermissionHandler(DBContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Get all available permission definitions grouped by category
        /// </summary>
        public AvailablePermissionsResponseVM GetAvailablePermissions()
        {
            var allPermissions = PermissionKeys.GetAllPermissions();
            var categories = new Dictionary<string, List<PermissionDefinitionVM>>();

            foreach (var perm in allPermissions)
            {
                var parts = perm.Key.Split('.');
                var category = parts[0] switch
                {
                    "inventory" => "Inventory",
                    "estimates" => "Estimates",
                    "clients" => "Clients",
                    "jobs" => "Jobs",
                    "financial" => "Financial",
                    "team" => "Team",
                    "timecards" => "Time Tracking",
                    _ => "Other"
                };

                if (!categories.ContainsKey(category))
                {
                    categories[category] = new List<PermissionDefinitionVM>();
                }

                categories[category].Add(new PermissionDefinitionVM
                {
                    key = perm.Key,
                    name = perm.Value,
                    category = category,
                    description = perm.Value
                });
            }

            return new AvailablePermissionsResponseVM
            {
                categories = categories.Select(c => new PermissionCategoryVM
                {
                    name = c.Key,
                    permissions = c.Value
                }).ToList()
            };
        }

        /// <summary>
        /// Get all permissions for a specific user in a company
        /// </summary>
        public async Task<UserPermissionsResponseVM> GetUserPermissionsAsync(Guid userId, Guid companyId)
        {
            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.user)
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null)
            {
                return null;
            }

            var role = userCompany.role;
            var defaultPermissions = PermissionKeys.GetDefaultPermissionsForRole(role);
            var allPermissions = PermissionKeys.GetAllPermissions();

            // Get explicit permissions for this user
            var explicitPermissions = await _ctx.UserPermissions
                .Include(up => up.grantedByUser)
                .Where(up => up.userId == userId && up.companyId == companyId && up.revokedDatetime == null)
                .ToListAsync();

            var permissionStatuses = new List<UserPermissionStatusVM>();

            foreach (var perm in allPermissions)
            {
                var explicitPerm = explicitPermissions.FirstOrDefault(ep => ep.permissionKey == perm.Key);
                var isDefault = defaultPermissions.Contains(perm.Key);
                var granted = explicitPerm?.granted ?? isDefault;

                var parts = perm.Key.Split('.');
                var category = parts[0] switch
                {
                    "inventory" => "Inventory",
                    "estimates" => "Estimates",
                    "clients" => "Clients",
                    "jobs" => "Jobs",
                    "financial" => "Financial",
                    "team" => "Team",
                    "timecards" => "Time Tracking",
                    _ => "Other"
                };

                permissionStatuses.Add(new UserPermissionStatusVM
                {
                    permissionKey = perm.Key,
                    permissionName = perm.Value,
                    category = category,
                    granted = granted,
                    isDefault = isDefault && explicitPerm == null,
                    grantedDatetime = explicitPerm?.grantedDatetime,
                    grantedByUserName = explicitPerm?.grantedByUser?.fullName
                });
            }

            return new UserPermissionsResponseVM
            {
                userId = userId,
                userFullName = userCompany.user.fullName,
                userEmail = userCompany.user.email,
                role = role,
                permissions = permissionStatuses
            };
        }

        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        public async Task<bool> HasPermissionAsync(Guid userId, Guid companyId, string permissionKey)
        {
            var userCompany = await _ctx.UserCompany
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null)
            {
                return false;
            }

            var role = userCompany.role;
            var defaultPermissions = PermissionKeys.GetDefaultPermissionsForRole(role);

            // Check explicit permission first
            var explicitPermission = await _ctx.UserPermissions
                .FirstOrDefaultAsync(up => 
                    up.userId == userId && 
                    up.companyId == companyId && 
                    up.permissionKey == permissionKey && 
                    up.revokedDatetime == null);

            if (explicitPermission != null)
            {
                return explicitPermission.granted;
            }

            // Fall back to role default
            return defaultPermissions.Contains(permissionKey);
        }

        /// <summary>
        /// Get all permissions for a user (for frontend to cache)
        /// </summary>
        public async Task<List<string>> GetUserGrantedPermissionsAsync(Guid userId, Guid companyId)
        {
            var userCompany = await _ctx.UserCompany
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null)
            {
                return new List<string>();
            }

            var role = userCompany.role;
            var defaultPermissions = PermissionKeys.GetDefaultPermissionsForRole(role);

            // Get explicit permissions
            var explicitPermissions = await _ctx.UserPermissions
                .Where(up => up.userId == userId && up.companyId == companyId && up.revokedDatetime == null)
                .ToListAsync();

            var grantedPermissions = new HashSet<string>(defaultPermissions);

            foreach (var ep in explicitPermissions)
            {
                if (ep.granted)
                {
                    grantedPermissions.Add(ep.permissionKey);
                }
                else
                {
                    grantedPermissions.Remove(ep.permissionKey);
                }
            }

            return grantedPermissions.ToList();
        }

        /// <summary>
        /// Grant or revoke a permission for a user
        /// </summary>
        public async Task<bool> UpdatePermissionAsync(Guid userId, Guid companyId, string permissionKey, bool granted, Guid grantedByUserId)
        {
            // Validate permission key
            var allPermissions = PermissionKeys.GetAllPermissions();
            if (!allPermissions.ContainsKey(permissionKey))
            {
                return false;
            }

            // Check if user exists in company
            var userCompany = await _ctx.UserCompany
                .FirstOrDefaultAsync(uc => uc.userId == userId && uc.companyId == companyId);

            if (userCompany == null)
            {
                return false;
            }

            var now = DateTime.UtcNow;
            var defaultPermissions = PermissionKeys.GetDefaultPermissionsForRole(userCompany.role);
            var isDefault = defaultPermissions.Contains(permissionKey);

            // Check if permission already exists (active or revoked)
            var existingPermission = await _ctx.UserPermissions
                .FirstOrDefaultAsync(up => 
                    up.userId == userId && 
                    up.companyId == companyId && 
                    up.permissionKey == permissionKey);

            if (existingPermission != null)
            {
                // If setting back to default, just delete the explicit permission
                if (granted == isDefault)
                {
                    _ctx.UserPermissions.Remove(existingPermission);
                }
                else
                {
                    // Update existing record instead of creating new one
                    existingPermission.granted = granted;
                    existingPermission.grantedByUserId = grantedByUserId;
                    existingPermission.grantedDatetime = now;
                    existingPermission.revokedDatetime = null; // Reactivate if was revoked
                }
            }
            else
            {
                // Only create explicit permission if different from default
                if (granted != isDefault)
                {
                    var newPermission = new UserPermission
                    {
                        id = Guid.NewGuid(),
                        userId = userId,
                        companyId = companyId,
                        permissionKey = permissionKey,
                        granted = granted,
                        grantedByUserId = grantedByUserId,
                        grantedDatetime = now
                    };

                    _ctx.UserPermissions.Add(newPermission);
                }
            }

            await _ctx.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Update multiple permissions at once
        /// </summary>
        public async Task<bool> UpdatePermissionsAsync(Guid userId, Guid companyId, List<PermissionUpdate> updates, Guid grantedByUserId)
        {
            foreach (var update in updates)
            {
                await UpdatePermissionAsync(userId, companyId, update.permissionKey, update.granted, grantedByUserId);
            }
            return true;
        }

        /// <summary>
        /// Reset all explicit permissions for a user (revert to role defaults)
        /// </summary>
        public async Task<bool> ResetPermissionsAsync(Guid userId, Guid companyId)
        {
            var now = DateTime.UtcNow;
            var existingPermissions = await _ctx.UserPermissions
                .Where(up => up.userId == userId && up.companyId == companyId && up.revokedDatetime == null)
                .ToListAsync();

            foreach (var perm in existingPermissions)
            {
                perm.revokedDatetime = now;
            }

            await _ctx.SaveChangesAsync();
            return true;
        }
    }
}

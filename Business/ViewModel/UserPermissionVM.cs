namespace Business.ViewModel
{
    /// <summary>
    /// Request to get user permissions
    /// </summary>
    public class GetUserPermissionsVM
    {
        public Guid userId { get; set; }
    }

    /// <summary>
    /// Request to grant or revoke a permission
    /// </summary>
    public class UpdatePermissionVM
    {
        public Guid userId { get; set; }
        public string permissionKey { get; set; }
        public bool granted { get; set; }
    }

    /// <summary>
    /// Request to update multiple permissions at once
    /// </summary>
    public class UpdatePermissionsVM
    {
        public Guid userId { get; set; }
        public List<PermissionUpdate> permissions { get; set; } = new();
    }

    public class PermissionUpdate
    {
        public string permissionKey { get; set; }
        public bool granted { get; set; }
    }

    /// <summary>
    /// Permission definition for UI display
    /// </summary>
    public class PermissionDefinitionVM
    {
        public string key { get; set; }
        public string name { get; set; }
        public string category { get; set; }
        public string description { get; set; }
    }

    /// <summary>
    /// User permission status
    /// </summary>
    public class UserPermissionStatusVM
    {
        public string permissionKey { get; set; }
        public string permissionName { get; set; }
        public string category { get; set; }
        public bool granted { get; set; }
        public bool isDefault { get; set; } // true if comes from role, false if explicitly granted
        public DateTime? grantedDatetime { get; set; }
        public string grantedByUserName { get; set; }
    }

    /// <summary>
    /// Response with all user permissions
    /// </summary>
    public class UserPermissionsResponseVM
    {
        public Guid userId { get; set; }
        public string userFullName { get; set; }
        public string userEmail { get; set; }
        public string role { get; set; }
        public List<UserPermissionStatusVM> permissions { get; set; } = new();
    }

    /// <summary>
    /// Response with all available permissions
    /// </summary>
    public class AvailablePermissionsResponseVM
    {
        public List<PermissionCategoryVM> categories { get; set; } = new();
    }

    public class PermissionCategoryVM
    {
        public string name { get; set; }
        public List<PermissionDefinitionVM> permissions { get; set; } = new();
    }
}

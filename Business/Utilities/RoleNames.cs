namespace Business.Utilities
{
    /// <summary>
    /// Standard role names used across the application.
    /// These roles are stored in the UserCompany.role field and define permissions within a company context.
    /// </summary>
    public static class RoleNames
    {
        /// <summary>
        /// SuperAdmin - Full system access across all companies
        /// </summary>
        public const string SuperAdmin = "SuperAdmin";

        /// <summary>
        /// Admin - Administrative access within assigned companies
        /// </summary>
        public const string Admin = "Admin";

        /// <summary>
        /// Leader - Team lead or manager role within assigned companies
        /// </summary>
        public const string Leader = "Leader";

        /// <summary>
        /// Crew - Standard team member within assigned companies
        /// </summary>
        public const string Crew = "Crew";

        /// <summary>
        /// Client - External client with limited access
        /// </summary>
        public const string Client = "Client";

        /// <summary>
        /// Get role hierarchy level (higher number = more permissions)
        /// </summary>
        public static int GetRoleLevel(string role)
        {
            return role switch
            {
                SuperAdmin => 5,
                Admin => 4,
                Leader => 3,
                Crew => 2,
                Client => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Check if a role has sufficient level to access a required role
        /// </summary>
        public static bool HasSufficientLevel(string userRole, string requiredRole)
        {
            return GetRoleLevel(userRole) >= GetRoleLevel(requiredRole);
        }

        /// <summary>
        /// Get all available role names
        /// </summary>
        public static string[] GetAllRoles()
        {
            return new[] { SuperAdmin, Admin, Leader, Crew, Client };
        }
    }
}

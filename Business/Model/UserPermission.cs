using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Business.Model
{
    /// <summary>
    /// Represents granular permissions granted to a user within a company.
    /// These permissions extend the base role permissions.
    /// </summary>
    public class UserPermission
    {
        public Guid id { get; set; }
        public Guid userId { get; set; }
        public Guid companyId { get; set; }
        public string permissionKey { get; set; }
        public bool granted { get; set; }
        public Guid grantedByUserId { get; set; }
        public DateTime grantedDatetime { get; set; }
        public DateTime? revokedDatetime { get; set; }

        // Navigation properties
        public User user { get; set; }
        public Company company { get; set; }
        public User grantedByUser { get; set; }
    }

    /// <summary>
    /// Available permission keys that can be granted to users
    /// </summary>
    public static class PermissionKeys
    {
        // Inventory permissions
        public const string ViewInventory = "inventory.view";
        public const string ManageInventory = "inventory.manage";
        
        // Estimate permissions
        public const string ViewEstimates = "estimates.view";
        public const string CreateEstimates = "estimates.create";
        public const string EditEstimates = "estimates.edit";
        
        // Client permissions
        public const string ViewClients = "clients.view";
        public const string ManageClients = "clients.manage";
        
        // Job permissions
        public const string ViewAllJobs = "jobs.view_all";
        public const string CreateJobs = "jobs.create";
        public const string EditJobs = "jobs.edit";
        public const string AssignTeamMembers = "jobs.assign_team";
        public const string ManageJobStatus = "jobs.manage_status";
        
        // Financial permissions
        public const string ViewCosts = "financial.view_costs";
        public const string ViewMargins = "financial.view_margins";
        
        // Team permissions
        public const string ViewTeamMembers = "team.view";
        public const string InviteTeamMembers = "team.invite";
        
        // Time tracking
        public const string ViewAllTimeCards = "timecards.view_all";
        public const string EditTimeCards = "timecards.edit";

        /// <summary>
        /// Get all available permission keys with descriptions
        /// </summary>
        public static Dictionary<string, string> GetAllPermissions()
        {
            return new Dictionary<string, string>
            {
                { ViewInventory, "View Inventory" },
                { ManageInventory, "Manage Inventory (add, edit, delete)" },
                { ViewEstimates, "View Estimates" },
                { CreateEstimates, "Create Estimates" },
                { EditEstimates, "Edit Estimates" },
                { ViewClients, "View Clients" },
                { ManageClients, "Manage Clients (add, edit, delete)" },
                { ViewAllJobs, "View All Jobs (not just assigned)" },
                { CreateJobs, "Create Jobs" },
                { EditJobs, "Edit Jobs" },
                { AssignTeamMembers, "Assign Team Members to Jobs" },
                { ManageJobStatus, "Manage Job Status (start, complete, cancel)" },
                { ViewCosts, "View Costs" },
                { ViewMargins, "View Profit Margins" },
                { ViewTeamMembers, "View Team Members" },
                { InviteTeamMembers, "Invite Team Members" },
                { ViewAllTimeCards, "View All Time Cards" },
                { EditTimeCards, "Edit Time Cards" },
            };
        }

        /// <summary>
        /// Get default permissions for a role
        /// </summary>
        public static List<string> GetDefaultPermissionsForRole(string role)
        {
            var normalizedRole = role?.ToLower() ?? "";
            
            return normalizedRole switch
            {
                "admin" or "superadmin" => GetAllPermissions().Keys.ToList(),
                "leader" => new List<string>
                {
                    ViewInventory, ManageInventory,
                    ViewEstimates, CreateEstimates, EditEstimates,
                    ViewClients, ManageClients,
                    ViewAllJobs, CreateJobs, EditJobs, AssignTeamMembers, ManageJobStatus,
                    ViewCosts,
                    ViewTeamMembers,
                    ViewAllTimeCards, EditTimeCards,
                },
                "crew" => new List<string>
                {
                    // Crew has minimal default permissions
                    // Admin can grant more via UserPermission
                },
                _ => new List<string>()
            };
        }
    }

    public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> builder)
        {
            builder.ToTable("UserPermissions");

            builder.HasKey(up => up.id);

            builder.Property(up => up.id)
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(up => up.permissionKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(up => up.granted)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(up => up.grantedDatetime)
                .IsRequired();

            // Indexes
            builder.HasIndex(up => new { up.userId, up.companyId, up.permissionKey })
                .IsUnique();

            builder.HasIndex(up => new { up.userId, up.companyId });

            // Relationships
            builder.HasOne(up => up.user)
                .WithMany()
                .HasForeignKey(up => up.userId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(up => up.company)
                .WithMany()
                .HasForeignKey(up => up.companyId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(up => up.grantedByUser)
                .WithMany()
                .HasForeignKey(up => up.grantedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}

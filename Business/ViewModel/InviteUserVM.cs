namespace Business.ViewModel
{
    /// <summary>
    /// ViewModel for inviting a new team member to a company
    /// </summary>
    public class InviteUserVM
    {
        public string email { get; set; } = string.Empty;
        public string fullName { get; set; } = string.Empty;
        public string role { get; set; } = "Crew"; // Default role: Crew, Leader, or Admin
        public string? phone { get; set; }
    }

    /// <summary>
    /// Response model for invite operation
    /// </summary>
    public class InviteUserResponseVM
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public Guid? userId { get; set; }
        public string? email { get; set; }
        public string? fullName { get; set; }
        public string? temporaryPassword { get; set; }
        public bool isNewUser { get; set; }
        public string? inviteToken { get; set; }
        public string? companyName { get; set; }
        public string? tempPassword { get; set; }
    }

    /// <summary>
    /// ViewModel for updating a team member's role
    /// </summary>
    public class UpdateTeamMemberRoleVM
    {
        public Guid userId { get; set; }
        public string role { get; set; } = string.Empty;
    }

    /// <summary>
    /// Team member details for listing
    /// </summary>
    public class TeamMemberRM
    {
        public Guid id { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string role { get; set; } = string.Empty;
        public string? personalPhoto { get; set; }
        public string? personalPhotoThumbnail { get; set; }
        public DateTime joinedDate { get; set; }
        public bool active { get; set; }
        public int activeJobsCount { get; set; }
        public int completedJobsCount { get; set; }
    }

    /// <summary>
    /// Request for listing team members
    /// </summary>
    public class TeamMemberListRequestVM
    {
        public int pageNumber { get; set; } = 1;
        public int pageSize { get; set; } = 20;
        public string? searchTerm { get; set; }
        public string? role { get; set; } // Filter by role
        public bool? active { get; set; } = true;
    }
}

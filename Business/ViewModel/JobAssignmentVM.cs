namespace Business.ViewModel;

/// <summary>
/// Request to assign a team member to a job
/// </summary>
public class AssignTeamMemberVM
{
    public Guid jobId { get; set; }
    public Guid userId { get; set; }
    public string? notes { get; set; }
}

/// <summary>
/// Request to unassign a team member from a job
/// </summary>
public class UnassignTeamMemberVM
{
    public Guid jobId { get; set; }
    public Guid userId { get; set; }
}

/// <summary>
/// Request to list job assignments
/// </summary>
public class JobAssignmentListRequestVM
{
    public Guid? jobId { get; set; }
    public Guid? userId { get; set; }
    public bool? active { get; set; } = true;
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
}

/// <summary>
/// Request to get jobs for a specific user (for non-admin filtering)
/// </summary>
public class UserJobsRequestVM
{
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
    public string? status { get; set; }
    public string? searchTerm { get; set; }
}

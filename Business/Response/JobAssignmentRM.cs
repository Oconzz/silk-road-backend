namespace Business.Response;

/// <summary>
/// Response model for job assignment
/// </summary>
public class JobAssignmentRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string jobTitle { get; set; } = string.Empty;
    public string jobStatus { get; set; } = string.Empty;
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public string userEmail { get; set; } = string.Empty;
    public string? userPhoto { get; set; }
    public string role { get; set; } = string.Empty;
    public string? notes { get; set; }
    public bool active { get; set; }
    public DateTime assignedDatetime { get; set; }
    public DateTime? unassignedDatetime { get; set; }
    public string assignedByUserName { get; set; } = string.Empty;
}

/// <summary>
/// Response model for job assignment list item
/// </summary>
public class JobAssignmentListItemRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string jobTitle { get; set; } = string.Empty;
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public string? userPhotoThumbnail { get; set; }
    public string role { get; set; } = string.Empty;
    public bool active { get; set; }
    public DateTime assignedDatetime { get; set; }
}

/// <summary>
/// Response model for assigned team member on a job
/// </summary>
public class AssignedTeamMemberRM
{
    public Guid userId { get; set; }
    public string fullName { get; set; } = string.Empty;
    public string email { get; set; } = string.Empty;
    public string? photoThumbnail { get; set; }
    public string role { get; set; } = string.Empty;
    public DateTime assignedDatetime { get; set; }
}

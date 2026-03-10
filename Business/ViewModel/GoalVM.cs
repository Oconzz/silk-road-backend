namespace Business.ViewModel;

/// <summary>
/// ViewModel for creating a new goal
/// </summary>
public class CreateGoalVM
{
    public Guid jobId { get; set; }
    public string title { get; set; } = string.Empty;
    public string? description { get; set; }
    public DateTime deadline { get; set; } // targetDate
    public Guid? assignedToUserId { get; set; } // null = entire team
    public decimal bonusAmount { get; set; } = 0;
    public bool requiresVerification { get; set; } = false;
    public int sortOrder { get; set; }
}

/// <summary>
/// ViewModel for updating a goal
/// </summary>
public class UpdateGoalVM
{
    public string? title { get; set; }
    public string? description { get; set; }
    public DateTime? deadline { get; set; }
    public Guid? assignedToUserId { get; set; }
    public int? sortOrder { get; set; }
}

/// <summary>
/// ViewModel for completing a goal
/// </summary>
public class CompleteGoalVM
{
    public Guid goalId { get; set; }
}

/// <summary>
/// Response model for goal
/// </summary>
public class GoalRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    
    public string title { get; set; } = string.Empty;
    public string? description { get; set; }
    
    public DateTime deadline { get; set; }
    public bool isOverdue { get; set; }
    public int daysUntilDeadline { get; set; }
    
    // Assignment info
    public Guid? assignedToUserId { get; set; }
    public string? assignedToUserName { get; set; }
    public string? assignedToUserPhoto { get; set; }
    public bool isTeamGoal { get; set; } // true if assignedToUserId is null
    
    // Creator info
    public Guid createdByUserId { get; set; }
    public string? createdByUserName { get; set; }
    
    // Status
    public string status { get; set; } = "pending";
    public DateTime? completedAt { get; set; }
    public bool? completedOnTime { get; set; }
    public Guid? completedByUserId { get; set; }
    public string? completedByUserName { get; set; }
    
    // Voting (for team goals)
    public List<GoalVoteRM> votes { get; set; } = new List<GoalVoteRM>();
    public int totalVotesNeeded { get; set; }
    public int votesReceived { get; set; }
    
    public int sortOrder { get; set; }
    public decimal bonusAmount { get; set; }
    
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

/// <summary>
/// Vote response model
/// </summary>
public class GoalVoteRM
{
    public Guid id { get; set; }
    public Guid goalId { get; set; }
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public bool voteComplete { get; set; }
    public DateTime voteDate { get; set; }
}

/// <summary>
/// Request for listing goals
/// </summary>
public class GoalListRequestVM
{
    public Guid? jobId { get; set; }
    public Guid? assignedToUserId { get; set; }
    public string? status { get; set; } // pending, completed, overdue, all
    public bool? myGoalsOnly { get; set; }
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
}

/// <summary>
/// Summary of goals for a job
/// </summary>
public class JobGoalsSummaryRM
{
    public Guid jobId { get; set; }
    public int totalGoals { get; set; }
    public int completedGoals { get; set; }
    public int pendingGoals { get; set; }
    public int overdueGoals { get; set; }
    public int completedOnTime { get; set; }
    public int completedLate { get; set; }
    public double completionRate { get; set; } // percentage
    public double onTimeRate { get; set; } // percentage of completed that were on time
}

/// <summary>
/// Summary of goals for a user (My Goals)
/// </summary>
public class UserGoalsSummaryRM
{
    public Guid userId { get; set; }
    public int totalAssigned { get; set; }
    public int completed { get; set; }
    public int pending { get; set; }
    public int overdue { get; set; }
    public int completedOnTime { get; set; }
    public int completedLate { get; set; }
    public double completionRate { get; set; }
    public double onTimeRate { get; set; }
    public List<GoalRM> upcomingGoals { get; set; } = new();
    public List<GoalRM> overdueGoals { get; set; } = new();
}

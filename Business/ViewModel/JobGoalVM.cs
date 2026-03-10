using System;

namespace Business.ViewModel;

public class CreateJobGoalVM
{
    public Guid jobId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public decimal bonusAmount { get; set; }
    public DateTime targetDate { get; set; }
    public bool requiresVerification { get; set; } = true;
}

public class UpdateJobGoalVM
{
    public Guid id { get; set; }
    public string? title { get; set; }
    public string? description { get; set; }
    public decimal? bonusAmount { get; set; }
    public DateTime? targetDate { get; set; }
    public bool? requiresVerification { get; set; }
}

public class CompleteJobGoalVM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
}

public class VerifyJobGoalVM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public bool approved { get; set; }
}

public class AddJobGoalMediaVM
{
    public Guid goalId { get; set; }
    public string base64Image { get; set; } = string.Empty;
}

public class JobGoalListRequestVM
{
    public Guid jobId { get; set; }
    public bool? completed { get; set; }
    public bool? verified { get; set; }
}

/// <summary>
/// ViewModel for casting a vote on a team goal
/// </summary>
public class CastGoalVoteVM
{
    public Guid goalId { get; set; }
    public Guid jobId { get; set; }
    public bool voteComplete { get; set; }
}

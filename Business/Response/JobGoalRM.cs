using System;
using System.Collections.Generic;

namespace Business.Response;

public class JobGoalRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public decimal bonusAmount { get; set; }
    public string? completionBonus { get; set; }
    public Guid? assignedToUserId { get; set; }
    public string? assignedToUserName { get; set; }
    public DateTime targetDate { get; set; }
    public bool completed { get; set; }
    public DateTime? completedDate { get; set; }
    public Guid? completedBy { get; set; }
    public string? completedByName { get; set; }
    public bool requiresVerification { get; set; }
    public bool verified { get; set; }
    public Guid? verifiedBy { get; set; }
    public string? verifiedByName { get; set; }
    public DateTime? verifiedDate { get; set; }
    public List<JobGoalMediaRM> media { get; set; } = new List<JobGoalMediaRM>();
    public List<JobGoalVoteRM> votes { get; set; } = new List<JobGoalVoteRM>();
    public int totalVotesNeeded { get; set; }
    public int votesReceived { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobGoalListItem
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string title { get; set; } = string.Empty;
    public decimal bonusAmount { get; set; }
    public DateTime targetDate { get; set; }
    public bool completed { get; set; }
    public bool verified { get; set; }
    public int mediaCount { get; set; }
}

public class JobGoalVoteRM
{
    public Guid id { get; set; }
    public Guid goalId { get; set; }
    public Guid userId { get; set; }
    public string userName { get; set; } = string.Empty;
    public bool voteComplete { get; set; }
    public DateTime voteDate { get; set; }
}

public class JobGoalMediaRM
{
    public Guid id { get; set; }
    public Guid goalId { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
}

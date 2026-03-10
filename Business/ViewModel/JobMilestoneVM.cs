using System;

namespace Business.ViewModel;

public class CreateJobMilestoneVM
{
    public Guid jobId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public int orderIndex { get; set; }
}

public class UpdateJobMilestoneVM
{
    public Guid id { get; set; }
    public string? title { get; set; }
    public string? description { get; set; }
    public bool? completed { get; set; }
    public int? orderIndex { get; set; }
}

public class CompleteJobMilestoneVM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
}

public class JobMilestoneListRequestVM
{
    public Guid jobId { get; set; }
    public bool? completed { get; set; }
}

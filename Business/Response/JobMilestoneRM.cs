using System;

namespace Business.Response;

public class JobMilestoneRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public string title { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public bool completed { get; set; }
    public DateTime? completedDate { get; set; }
    public Guid? completedBy { get; set; }
    public string? completedByName { get; set; }
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobMilestoneListItem
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string title { get; set; } = string.Empty;
    public bool completed { get; set; }
    public DateTime? completedDate { get; set; }
    public int orderIndex { get; set; }
}

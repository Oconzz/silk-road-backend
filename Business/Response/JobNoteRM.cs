using System;

namespace Business.Response;

public class JobNoteRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public Guid createdBy { get; set; }
    public string createdByName { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobNoteListItem
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string createdByName { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
}

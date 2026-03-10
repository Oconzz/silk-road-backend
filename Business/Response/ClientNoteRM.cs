using System;

namespace Business.Response;

public class ClientNoteRM
{
    public Guid id { get; set; }
    public Guid clientId { get; set; }
    public Guid companyId { get; set; }
    public Guid createdBy { get; set; }
    public string createdByName { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class ClientNoteListItem
{
    public Guid id { get; set; }
    public Guid clientId { get; set; }
    public string createdByName { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
}

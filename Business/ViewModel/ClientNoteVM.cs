using System;

namespace Business.ViewModel;

public class CreateClientNoteVM
{
    public Guid clientId { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general";
}

public class UpdateClientNoteVM
{
    public Guid id { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general";
}

public class ClientNoteListRequestVM
{
    public Guid clientId { get; set; }
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
    public string? type { get; set; }
}

using System;

namespace Business.ViewModel;

public class CreateJobNoteVM
{
    public Guid jobId { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general";
}

public class UpdateJobNoteVM
{
    public Guid id { get; set; }
    public string content { get; set; } = string.Empty;
    public string type { get; set; } = "general";
}

public class JobNoteListRequestVM
{
    public Guid jobId { get; set; }
    public int pageNumber { get; set; } = 1;
    public int pageSize { get; set; } = 50;
    public string? type { get; set; }
}

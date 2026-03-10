using System;
using System.Collections.Generic;

namespace Business.Response;

public class JobMediaRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public Guid? albumId { get; set; }
    public string? albumName { get; set; }
    public Guid? jobInventoryId { get; set; }
    public string? jobInventoryTitle { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobMediaListItem
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = string.Empty;
    public string category { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
}

public class JobAlbumRM
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int orderIndex { get; set; }
    public int mediaCount { get; set; }
    public List<JobMediaRM> media { get; set; } = new List<JobMediaRM>();
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class JobAlbumListItem
{
    public Guid id { get; set; }
    public Guid jobId { get; set; }
    public string name { get; set; } = string.Empty;
    public int mediaCount { get; set; }
    public int orderIndex { get; set; }
}

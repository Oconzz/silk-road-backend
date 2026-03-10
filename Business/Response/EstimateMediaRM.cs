using System;
using System.Collections.Generic;

namespace Business.Response;

public class EstimateMediaRM
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public Guid companyId { get; set; }
    public Guid? albumId { get; set; }
    public string? albumName { get; set; }
    public Guid? estimateItemId { get; set; }
    public string? estimateItemTitle { get; set; }
    public string webUrl { get; set; } = string.Empty;
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = string.Empty;
    public int orderIndex { get; set; }
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class EstimateMediaListItem
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public string thumbUrl { get; set; } = string.Empty;
    public string? caption { get; set; }
    public string type { get; set; } = string.Empty;
    public DateTime createDatetime { get; set; }
}

public class EstimateAlbumRM
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public Guid companyId { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int orderIndex { get; set; }
    public int mediaCount { get; set; }
    public List<EstimateMediaRM> media { get; set; } = new List<EstimateMediaRM>();
    public DateTime createDatetime { get; set; }
    public DateTime updateDatetime { get; set; }
}

public class EstimateAlbumListItem
{
    public Guid id { get; set; }
    public Guid estimateId { get; set; }
    public string name { get; set; } = string.Empty;
    public int mediaCount { get; set; }
    public int orderIndex { get; set; }
}

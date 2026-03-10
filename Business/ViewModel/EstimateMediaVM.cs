using System;

namespace Business.ViewModel;

public class CreateEstimateMediaVM
{
    public Guid estimateId { get; set; }
    public Guid? albumId { get; set; }
    public string? base64Image { get; set; }
    public string? s3Key { get; set; }
    public string? fileName { get; set; }
    public string? mimeType { get; set; }
    public long? sizeBytes { get; set; }
    public string? caption { get; set; }
    public string type { get; set; } = "photo";
    public int orderIndex { get; set; }
}

public class UpdateEstimateMediaVM
{
    public Guid id { get; set; }
    public Guid? albumId { get; set; }
    public string? caption { get; set; }
    public int? orderIndex { get; set; }
}

public class EstimateMediaListRequestVM
{
    public Guid estimateId { get; set; }
    public Guid? albumId { get; set; }
}

public class CreateEstimateAlbumVM
{
    public Guid estimateId { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int orderIndex { get; set; }
}

public class UpdateEstimateAlbumVM
{
    public Guid id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public int? orderIndex { get; set; }
}

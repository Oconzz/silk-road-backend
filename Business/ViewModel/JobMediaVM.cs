using System;

namespace Business.ViewModel;

public class CreateJobMediaVM
{
    public Guid jobId { get; set; }
    public Guid? albumId { get; set; }
    public string? base64Image { get; set; }
    public string? s3Key { get; set; }
    public string? fileName { get; set; }
    public string? mimeType { get; set; }
    public long? sizeBytes { get; set; }
    public string? caption { get; set; }
    public string type { get; set; } = "photo";
    public string category { get; set; } = "general";
    public int orderIndex { get; set; }
}

public class UpdateJobMediaVM
{
    public Guid id { get; set; }
    public Guid? albumId { get; set; }
    public string? caption { get; set; }
    public string? category { get; set; }
    public int? orderIndex { get; set; }
}

public class JobMediaListRequestVM
{
    public Guid jobId { get; set; }
    public Guid? albumId { get; set; }
    public string? category { get; set; }
}

public class CreateJobAlbumVM
{
    public Guid jobId { get; set; }
    public string name { get; set; } = string.Empty;
    public string? description { get; set; }
    public int orderIndex { get; set; }
}

public class UpdateJobAlbumVM
{
    public Guid id { get; set; }
    public string? name { get; set; }
    public string? description { get; set; }
    public int? orderIndex { get; set; }
}

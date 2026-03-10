using System;

namespace Business.Response;

/// <summary>
/// Response model for Quick Start Template
/// </summary>
public class QuickStartTemplateRM
{
    public Guid id { get; set; }
    public string category { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string description { get; set; } = string.Empty;
    public int displayOrder { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Business.ViewModel;

public class EmailVO
{
    public string from { get; set; }
    public string to { get; set; }
    public string subject { get; set; }
    public string body { get; set; }
    public List<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
}

public class EmailAttachment
{
    [Required]
    public string FileName { get; set; }

    [Required]
    public string ContentType { get; set; }

    [Required]
    public byte[] Data { get; set; }
}

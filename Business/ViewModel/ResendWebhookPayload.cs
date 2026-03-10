using System.Text.Json.Serialization;

namespace Business.ViewModel;

/// <summary>
/// Payload received from Resend's email.received webhook event.
/// Docs: https://resend.com/docs/dashboard/receiving/introduction
/// </summary>
public class ResendWebhookPayload
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public ResendEmailData Data { get; set; } = new();
}

public class ResendEmailData
{
    [JsonPropertyName("email_id")]
    public string EmailId { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("to")]
    public List<string> To { get; set; } = new();

    [JsonPropertyName("cc")]
    public List<string> Cc { get; set; } = new();

    [JsonPropertyName("bcc")]
    public List<string> Bcc { get; set; } = new();

    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    [JsonPropertyName("attachments")]
    public List<ResendAttachmentMeta> Attachments { get; set; } = new();
}

public class ResendAttachmentMeta
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = string.Empty;

    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("content_disposition")]
    public string ContentDisposition { get; set; } = string.Empty;

    [JsonPropertyName("content_id")]
    public string ContentId { get; set; } = string.Empty;
}

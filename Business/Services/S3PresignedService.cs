using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Business.Utilities;
using Microsoft.Extensions.Options;

namespace Business.Services;

/// <summary>
/// Interface for S3 presigned URL operations
/// </summary>
public interface IS3PresignedService
{
    /// <summary>
    /// Generate a presigned URL for uploading a file to S3
    /// </summary>
    /// <param name="fileName">Original file name</param>
    /// <param name="entityType">Type of entity (estimate, job, inventory)</param>
    /// <param name="entityId">ID of the entity</param>
    /// <param name="contentType">MIME type of the file</param>
    /// <returns>Presigned URL and the S3 key</returns>
    Task<(string PresignedUrl, string S3Key)> GeneratePresignedUploadUrl(
        string fileName, 
        string entityType, 
        Guid entityId, 
        string contentType = "image/jpeg");

    /// <summary>
    /// Generate a presigned URL for downloading a file from S3
    /// </summary>
    /// <param name="s3Key">The S3 key of the file</param>
    /// <returns>Presigned download URL</returns>
    Task<string> GeneratePresignedDownloadUrl(string s3Key);

    /// <summary>
    /// Generate both web and thumbnail presigned download URLs
    /// </summary>
    /// <param name="s3Key">The S3 key of the file</param>
    /// <returns>Web URL and Thumbnail URL</returns>
    Task<(string WebUrl, string ThumbUrl)> GeneratePresignedDownloadUrls(string s3Key);
}

/// <summary>
/// Service for generating presigned URLs for S3 operations
/// </summary>
public class S3PresignedService : IS3PresignedService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsSettings _awsSettings;
    private readonly string _bucketName;
    private readonly string _environment;

    // URL expiration times
    private const int UploadUrlExpirationMinutes = 15;
    private const int DownloadUrlExpirationMinutes = 60;

    public S3PresignedService(IOptions<AppSettings> appSettings)
    {
        var settings = appSettings.Value;
        _awsSettings = settings.AwsSettings ?? new AwsSettings();
        
        // Determine bucket name - prefer private bucket for media
        _bucketName = !string.IsNullOrEmpty(_awsSettings.S3PrivateBucket) 
            ? _awsSettings.S3PrivateBucket 
            : Environment.GetEnvironmentVariable("S3_PRIVATE_BUCKET") ?? "nmj-media-private";

        // Determine environment
        _environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "development";

        // Parse region
        var regionString = !string.IsNullOrEmpty(_awsSettings.S3Region) 
            ? _awsSettings.S3Region 
            : "us-east-1";
        var region = RegionEndpoint.GetBySystemName(regionString);

        // Initialize S3 client
        var config = new AmazonS3Config
        {
            RegionEndpoint = region
        };

        _s3Client = new AmazonS3Client(
            _awsSettings.AWS_ACCESS_KEY, 
            _awsSettings.AWS_SECRET_KEY, 
            config);
    }

    /// <inheritdoc />
    public Task<(string PresignedUrl, string S3Key)> GeneratePresignedUploadUrl(
        string fileName, 
        string entityType, 
        Guid entityId, 
        string contentType = "image/jpeg")
    {
        // Sanitize filename and generate unique key
        var sanitizedFileName = SanitizeFileName(fileName);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        
        // Build S3 key: /{environment}/{entityType}/{entityId}/{timestamp}-{uniqueId}-{filename}
        var s3Key = $"{_environment}/{entityType}/{entityId}/{timestamp}-{uniqueId}-{sanitizedFileName}";

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddMinutes(UploadUrlExpirationMinutes),
            ContentType = contentType
        };

        var presignedUrl = _s3Client.GetPreSignedURL(request);
        
        Console.WriteLine($"[S3PresignedService] Generated upload URL for key: {s3Key}");
        
        return Task.FromResult((presignedUrl, s3Key));
    }

    /// <inheritdoc />
    public Task<string> GeneratePresignedDownloadUrl(string s3Key)
    {
        if (string.IsNullOrEmpty(s3Key))
        {
            return Task.FromResult(string.Empty);
        }

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = s3Key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.AddMinutes(DownloadUrlExpirationMinutes)
        };

        var presignedUrl = _s3Client.GetPreSignedURL(request);
        
        return Task.FromResult(presignedUrl);
    }

    /// <inheritdoc />
    public async Task<(string WebUrl, string ThumbUrl)> GeneratePresignedDownloadUrls(string s3Key)
    {
        if (string.IsNullOrEmpty(s3Key))
        {
            return (string.Empty, string.Empty);
        }

        // For now, return the same URL for both web and thumb
        // In the future, we can generate different sizes on upload
        var url = await GeneratePresignedDownloadUrl(s3Key);
        
        return (url, url);
    }

    /// <summary>
    /// Sanitize filename to be S3-safe
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return $"image-{Guid.NewGuid():N}.jpg";
        }

        // Remove path separators and keep only the filename
        var name = Path.GetFileName(fileName);
        
        // Replace spaces and special characters
        name = name.Replace(" ", "-")
                   .Replace("'", "")
                   .Replace("\"", "")
                   .Replace("&", "and");

        // Ensure lowercase for consistency
        return name.ToLowerInvariant();
    }
}

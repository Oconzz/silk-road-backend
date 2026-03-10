using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Business.Utilities;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services
{
    public interface IReceiptService
    {
        Task<(string? s3Name, string? url, double size, string contentType)> UploadReceiptAsync(string base64, string companyId);
        Task<bool> DeleteReceiptAsync(string s3Name);
    }

    public class ReceiptService : IReceiptService
    {
        private readonly IAmazonS3 _s3;
        private readonly AwsSettings _aws;
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB limit to protect server
        private const string ReceiptKeyPrefix = "receipts";

        // Allowed MIME types for receipts
        private static readonly string[] AllowedMimeTypes = new[]
        {
            "image/jpeg",
            "image/jpg",
            "image/png",
            "image/webp",
            "image/gif",
            "image/bmp",
            "image/tiff"
        };

        public ReceiptService(IOptions<AppSettings> appSettings)
        {
            var settings = appSettings.Value;
            _aws = settings.AwsSettings ?? new AwsSettings();
            var cfg = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.USEast1
            };
            _s3 = new AmazonS3Client(_aws.AWS_ACCESS_KEY, _aws.AWS_SECRET_KEY, cfg);
        }

        public async Task<(string? s3Name, string? url, double size, string contentType)> UploadReceiptAsync(string base64, string companyId)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException("Receipt data cannot be empty");

            var (mime, bytes) = ParseBase64(base64);
            
            if (bytes == null || bytes.Length == 0)
                throw new ArgumentException("Invalid base64 data");

            // Validate file size to protect server
            if (bytes.Length > MaxFileSizeBytes)
                throw new InvalidOperationException($"File size exceeds maximum limit of {MaxFileSizeBytes / (1024 * 1024)}MB");

            // Validate content type
            if (mime == null || !IsAllowedMimeType(mime))
                throw new InvalidOperationException($"File type '{mime}' is not allowed. Only image files (JPEG, PNG, etc.) are accepted");

            // Get bucket from configuration or environment variable
            var bucket = Environment.GetEnvironmentVariable("PUBLIC_S3_BUCKET") 
                ?? _aws.S3PublicBucket 
                ?? "public-cnk";
            
            var ext = GetExtension(mime) ?? "jpg";
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var s3Name = $"{ReceiptKeyPrefix}/{companyId}/{timestamp}-{Guid.NewGuid():N}.{ext}";

            await PutPublicAsync(bucket, s3Name, bytes, mime);
            
            var urlBase = await GetPublicUrlAsync(bucket);
            var url = $"{urlBase}/{s3Name}";
            var size = bytes.Length / 1024.0; // Convert to KB

            return (s3Name, url, size, mime);
        }

        public async Task<bool> DeleteReceiptAsync(string s3Name)
        {
            if (string.IsNullOrWhiteSpace(s3Name))
                return false;

            try
            {
                // Get bucket from configuration or environment variable
                var bucket = Environment.GetEnvironmentVariable("PUBLIC_S3_BUCKET") 
                    ?? _aws.S3PublicBucket 
                    ?? "public-cnk";
                    
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = bucket,
                    Key = s3Name
                };
                await _s3.DeleteObjectAsync(deleteRequest);
                return true;
            }
            catch
            {
                // Log error in production, but don't fail the operation
                return false;
            }
        }

        private static bool IsAllowedMimeType(string mime)
        {
            return Array.Exists(AllowedMimeTypes, t => t.Equals(mime, StringComparison.OrdinalIgnoreCase));
        }

        private static (string? mime, byte[]? bytes) ParseBase64(string data)
        {
            try
            {
                // Try parsing data URL format: data:image/jpeg;base64,xxxxx
                var match = Regex.Match(data, @"^data:(?<mime>[^;]+);base64,(?<data>.+)$");
                if (match.Success)
                {
                    var mime = match.Groups["mime"].Value;
                    var b64 = match.Groups["data"].Value;
                    return (mime, Convert.FromBase64String(b64));
                }
                
                // Fallback: assume raw base64 with default JPEG type
                return ("image/jpeg", Convert.FromBase64String(data));
            }
            catch
            {
                return (null, null);
            }
        }

        private static string? GetExtension(string? mime)
        {
            return mime?.ToLower() switch
            {
                "image/png" => "png",
                "image/webp" => "webp",
                "image/gif" => "gif",
                "image/bmp" => "bmp",
                "image/tiff" => "tiff",
                "image/tif" => "tif",
                _ => "jpg"
            };
        }

        private async Task PutPublicAsync(string bucket, string key, byte[] bytes, string? mime)
        {
            using var ms = new MemoryStream(bytes);
            var req = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = ms,
                ContentType = mime ?? MediaTypeNames.Image.Jpeg,
                CannedACL = S3CannedACL.PublicRead
            };
            await _s3.PutObjectAsync(req);
        }

        private async Task<string> GetPublicUrlAsync(string bucket)
        {
            // For public buckets - construct standard S3 URL
            var region = (await _s3.GetBucketLocationAsync(bucket)).Location.Value;
            var regionSegment = string.IsNullOrEmpty(region) || region == "us-east-1" ? "" : $"-{region}";
            return $"https://{bucket}.s3{regionSegment}.amazonaws.com";
        }
    }
}

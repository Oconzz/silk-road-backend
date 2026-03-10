using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Business.Utilities;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Net.Mime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Business.Services
{
    public interface IPhotoService
    {
        Task<(string? webUrl, string? thumbUrl)> ProcessAndUploadAsync(string base64, string keyPrefix);
    }

    public class PhotoService : IPhotoService
    {
        private readonly IAmazonS3 _s3;
        private readonly AwsSettings _aws;
        private readonly string _bucketName;
        private const int WebWidth = 1280; // web size width
        private const int ThumbWidth = 256; // thumbnail width

        public PhotoService(IOptions<AppSettings> appSettings)
        {
            // CRITICAL FIX: Without this check, app crashes on startup if config is missing.
            // All users would be unable to use the application entirely.
            if (appSettings == null || appSettings.Value == null)
                throw new ArgumentNullException(nameof(appSettings), "AppSettings configuration is required");
            
            var settings = appSettings.Value;
            _aws = settings.AwsSettings ?? new AwsSettings();
            _bucketName = _aws.S3PublicBucket ?? _aws.S3PrivateBucket ?? "nmj-dev-gallery";
            var region = _aws.S3Region ?? "us-east-2";
            var cfg = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region)
            };
            _s3 = new AmazonS3Client(_aws.AWS_ACCESS_KEY, _aws.AWS_SECRET_KEY, cfg);
        }

        public async Task<(string? webUrl, string? thumbUrl)> ProcessAndUploadAsync(string base64, string keyPrefix)
        {
            if (string.IsNullOrWhiteSpace(base64)) return (null, null);
            var (mime, bytes) = ParseBase64(base64);
            if (bytes == null) return (null, null);
            var bucket = _bucketName;
            var ext = GetExtension(mime) ?? "jpg";
            var keyBase = $"{keyPrefix.Trim('/').Trim()}-{Guid.NewGuid():N}";
            var webKey = $"{keyBase}.web.{ext}";
            var thumbKey = $"{keyBase}.thumb.{ext}";
            var (webBytes, thumbBytes) = await ResizeAsync(bytes);
            await PutPublicAsync(bucket, webKey, webBytes, mime);
            await PutPublicAsync(bucket, thumbKey, thumbBytes, mime);
            var urlBase = await GetPublicUrlAsync(bucket);
            return ($"{urlBase}/{webKey}", $"{urlBase}/{thumbKey}");
        }

        private static (string? mime, byte[]? bytes) ParseBase64(string data)
        {
            try
            {
                var match = Regex.Match(data, @"^data:(?<mime>[^;]+);base64,(?<data>.+)$");
                if (match.Success)
                {
                    var mime = match.Groups["mime"].Value;
                    var b64 = match.Groups["data"].Value;
                    return (mime, Convert.FromBase64String(b64));
                }
                return ("image/jpeg", Convert.FromBase64String(data));
            }
            catch { return (null, null); }
        }

        private static string? GetExtension(string? mime)
        {
            return mime switch
            {
                "image/png" => "png",
                "image/webp" => "webp",
                _ => "jpg"
            };
        }

        private static async Task<(byte[] web, byte[] thumb)> ResizeAsync(byte[] original)
        {
            using var image = Image.Load(original);
            var web = await ResizeToWidthAsync(image, WebWidth);
            var thumb = await ResizeToWidthAsync(image, ThumbWidth);
            return (web, thumb);
        }

        private static async Task<byte[]> ResizeToWidthAsync(Image source, int targetWidth)
        {
            using var clone = source.Clone(ctx =>
            {
                var width = Math.Min(targetWidth, source.Width);
                var height = (int)Math.Round(source.Height * (width / (double)source.Width));
                ctx.Resize(width, height);
                ctx.BackgroundColor(SixLabors.ImageSharp.Color.Transparent);
            });
            using var ms = new MemoryStream();
            var encoder = new JpegEncoder { Quality = 85 };
            await clone.SaveAsJpegAsync(ms, encoder);
            return ms.ToArray();
        }

        private async Task PutPublicAsync(string bucket, string key, byte[] bytes, string? mime)
        {
            using var ms = new MemoryStream(bytes);
            var req = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                InputStream = ms,
                ContentType = mime ?? MediaTypeNames.Image.Jpeg
            };
            await _s3.PutObjectAsync(req);
        }

        private Task<string> GetPublicUrlAsync(string bucket)
        {
            // Use configured region directly for consistent URL format
            var region = _aws.S3Region ?? "us-east-2";
            return Task.FromResult($"https://{bucket}.s3.{region}.amazonaws.com");
        }
    }
}

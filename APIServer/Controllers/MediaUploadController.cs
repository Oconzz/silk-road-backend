using APIServer.AuthAttributes;
using Business.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for handling media upload operations via S3 presigned URLs
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class MediaUploadController : ControllerBase
    {
        private readonly IS3PresignedService _s3PresignedService;

        public MediaUploadController(IS3PresignedService s3PresignedService)
        {
            _s3PresignedService = s3PresignedService;
        }

        /// <summary>
        /// Request for generating a presigned upload URL
        /// </summary>
        public class SignUploadRequest
        {
            /// <summary>Original filename</summary>
            public string FileName { get; set; } = string.Empty;
            
            /// <summary>Entity type: estimate, job, or inventory</summary>
            public string EntityType { get; set; } = string.Empty;
            
            /// <summary>Entity ID (estimate, job, or inventory item ID)</summary>
            public Guid EntityId { get; set; }
            
            /// <summary>MIME type of the file (default: image/jpeg)</summary>
            public string MimeType { get; set; } = "image/jpeg";
        }

        /// <summary>
        /// Response containing the presigned URL and S3 key
        /// </summary>
        public class SignUploadResponse
        {
            /// <summary>Presigned URL for PUT operation</summary>
            public string UploadUrl { get; set; } = string.Empty;
            
            /// <summary>S3 key to store for later retrieval</summary>
            public string FileKey { get; set; } = string.Empty;
            
            /// <summary>Content-Type that must be used when uploading</summary>
            public string ContentType { get; set; } = string.Empty;
        }

        /// <summary>
        /// Generate a presigned URL for uploading a file to S3
        /// </summary>
        /// <param name="request">Upload request containing file details</param>
        /// <returns>Presigned upload URL and file key</returns>
        [TypeFilter(typeof(AuthorizationAttribute), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("sign")]
        public async Task<ActionResult<SignUploadResponse>> SignUpload([FromBody] SignUploadRequest request)
        {
            // Validate company header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Validate request
            if (string.IsNullOrEmpty(request.FileName))
            {
                return BadRequest(new { Message = "FileName is required" });
            }

            if (string.IsNullOrEmpty(request.EntityType))
            {
                return BadRequest(new { Message = "EntityType is required (estimate, job, or inventory)" });
            }

            if (request.EntityId == Guid.Empty)
            {
                return BadRequest(new { Message = "EntityId is required" });
            }

            // Validate entity type
            var allowedEntityTypes = new[] { "estimate", "job", "inventory" };
            var entityType = request.EntityType.ToLowerInvariant();
            if (!Array.Exists(allowedEntityTypes, t => t == entityType))
            {
                return BadRequest(new { Message = "EntityType must be one of: estimate, job, inventory" });
            }

            // Validate MIME type
            var allowedMimeTypes = new[] { 
                "image/jpeg", "image/jpg", "image/png", "image/gif", "image/heic", "image/heif", "image/webp",
                "video/mp4", "video/quicktime", "video/mov"
            };
            var mimeType = string.IsNullOrEmpty(request.MimeType) ? "image/jpeg" : request.MimeType.ToLowerInvariant();
            if (!Array.Exists(allowedMimeTypes, m => m == mimeType))
            {
                return BadRequest(new { Message = $"Unsupported MIME type. Allowed: {string.Join(", ", allowedMimeTypes)}" });
            }

            try
            {
                var (presignedUrl, s3Key) = await _s3PresignedService.GeneratePresignedUploadUrl(
                    request.FileName,
                    entityType,
                    request.EntityId,
                    mimeType);

                Console.WriteLine($"[MediaUpload] Generated presigned URL for {entityType}/{request.EntityId}, key: {s3Key}");

                return Ok(new SignUploadResponse
                {
                    UploadUrl = presignedUrl,
                    FileKey = s3Key,
                    ContentType = mimeType
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaUpload] Error generating presigned URL: {ex.Message}");
                return StatusCode(500, new { Message = "Failed to generate upload URL" });
            }
        }

        /// <summary>
        /// Generate a presigned URL for downloading/viewing a file from S3
        /// </summary>
        /// <param name="s3Key">The S3 key of the file</param>
        /// <returns>Presigned download URL</returns>
        [TypeFilter(typeof(AuthorizationAttribute), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("download-url")]
        public async Task<ActionResult<object>> GetDownloadUrl([FromQuery] string s3Key)
        {
            if (string.IsNullOrEmpty(s3Key))
            {
                return BadRequest(new { Message = "s3Key query parameter is required" });
            }

            try
            {
                var downloadUrl = await _s3PresignedService.GeneratePresignedDownloadUrl(s3Key);

                return Ok(new { DownloadUrl = downloadUrl, S3Key = s3Key });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MediaUpload] Error generating download URL: {ex.Message}");
                return StatusCode(500, new { Message = "Failed to generate download URL" });
            }
        }
    }
}

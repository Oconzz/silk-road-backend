using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Business.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for push notification management (device tokens, preferences, sending)
    /// </summary>
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly DBContext _ctx;

        public NotificationController(DBContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Register a device token for push notifications
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("register-device")]
        public async Task<ActionResult> RegisterDevice([FromBody] RegisterDeviceVM request)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { Message = "Token is required" });
            }

            // Check if this token already exists for this user
            var existing = await _ctx.DeviceTokens
                .FirstOrDefaultAsync(d => d.userId == userId.Value && d.token == request.Token);

            if (existing != null)
            {
                // Update existing
                existing.active = true;
                existing.companyId = companyId;
                existing.platform = request.Platform ?? existing.platform;
                existing.deviceId = request.DeviceId ?? existing.deviceId;
                existing.updateDatetime = DateTime.UtcNow;
            }
            else
            {
                // Create new
                var deviceToken = new DeviceToken
                {
                    userId = userId.Value,
                    companyId = companyId,
                    token = request.Token,
                    platform = request.Platform ?? "unknown",
                    deviceId = request.DeviceId,
                    active = true,
                    createDatetime = DateTime.UtcNow,
                    updateDatetime = DateTime.UtcNow,
                };
                _ctx.DeviceTokens.Add(deviceToken);
            }

            await _ctx.SaveChangesAsync();
            Console.WriteLine($"[Notification] Device registered for user {userId}");

            return Ok(new { Message = "Device registered successfully" });
        }

        /// <summary>
        /// Unregister a device token (e.g., on logout)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("unregister-device")]
        public async Task<ActionResult> UnregisterDevice([FromBody] UnregisterDeviceVM request)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { Message = "Token is required" });
            }

            var token = await _ctx.DeviceTokens
                .FirstOrDefaultAsync(d => d.userId == userId.Value && d.token == request.Token);

            if (token != null)
            {
                token.active = false;
                token.updateDatetime = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
            }

            return Ok(new { Message = "Device unregistered" });
        }

        /// <summary>
        /// Get notification preferences for the current user
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("preferences")]
        public async Task<ActionResult<NotificationPreferencesRM>> GetPreferences()
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var prefs = await _ctx.NotificationPreferences
                .FirstOrDefaultAsync(p => p.userId == userId.Value && p.companyId == companyId);

            if (prefs == null)
            {
                // Return defaults
                return Ok(new NotificationPreferencesRM
                {
                    Enabled = true,
                    MaterialReturn = true,
                    TimecardReminder = true,
                    JobAssignment = true,
                    RewardGoal = true,
                    ReviewPrompt = true,
                });
            }

            return Ok(new NotificationPreferencesRM
            {
                Enabled = prefs.enabled,
                MaterialReturn = prefs.materialReturn,
                TimecardReminder = prefs.timecardReminder,
                JobAssignment = prefs.jobAssignment,
                RewardGoal = prefs.rewardGoal,
                ReviewPrompt = prefs.reviewPrompt,
            });
        }

        /// <summary>
        /// Update notification preferences for the current user
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPut("preferences")]
        public async Task<ActionResult<NotificationPreferencesRM>> UpdatePreferences(
            [FromBody] UpdatePreferencesVM request)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var prefs = await _ctx.NotificationPreferences
                .FirstOrDefaultAsync(p => p.userId == userId.Value && p.companyId == companyId);

            if (prefs == null)
            {
                prefs = new NotificationPreference
                {
                    userId = userId.Value,
                    companyId = companyId,
                    createDatetime = DateTime.UtcNow,
                };
                _ctx.NotificationPreferences.Add(prefs);
            }

            prefs.enabled = request.Enabled;
            prefs.materialReturn = request.MaterialReturn;
            prefs.timecardReminder = request.TimecardReminder;
            prefs.jobAssignment = request.JobAssignment;
            prefs.rewardGoal = request.RewardGoal;
            prefs.reviewPrompt = request.ReviewPrompt;
            prefs.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return Ok(new NotificationPreferencesRM
            {
                Enabled = prefs.enabled,
                MaterialReturn = prefs.materialReturn,
                TimecardReminder = prefs.timecardReminder,
                JobAssignment = prefs.jobAssignment,
                RewardGoal = prefs.rewardGoal,
                ReviewPrompt = prefs.reviewPrompt,
            });
        }

        // Helper to get userId from auth header (set by AuthAttribute)
        private Guid? GetUserId()
        {
            if (HttpContext.Items.TryGetValue("UserId", out var userIdObj) && userIdObj is Guid uid)
            {
                return uid;
            }
            // Fallback: try from header
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var parsed))
            {
                return parsed;
            }
            return null;
        }
    }

    #region ViewModels

    public class RegisterDeviceVM
    {
        public string Token { get; set; } = string.Empty;
        public string? Platform { get; set; }
        public string? DeviceId { get; set; }
    }

    public class UnregisterDeviceVM
    {
        public string Token { get; set; } = string.Empty;
    }

    public class NotificationPreferencesRM
    {
        public bool Enabled { get; set; }
        public bool MaterialReturn { get; set; }
        public bool TimecardReminder { get; set; }
        public bool JobAssignment { get; set; }
        public bool RewardGoal { get; set; }
        public bool ReviewPrompt { get; set; }
    }

    public class UpdatePreferencesVM
    {
        public bool Enabled { get; set; } = true;
        public bool MaterialReturn { get; set; } = true;
        public bool TimecardReminder { get; set; } = true;
        public bool JobAssignment { get; set; } = true;
        public bool RewardGoal { get; set; } = true;
        public bool ReviewPrompt { get; set; } = true;
    }

    #endregion
}

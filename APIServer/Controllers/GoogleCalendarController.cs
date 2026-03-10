using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Business.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for Google Calendar integration (NMJ-40, NMJ-41, NMJ-42)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class GoogleCalendarController : ControllerBase
    {
        public GoogleCalendarController()
        {
        }

        /// <summary>
        /// Get the current user's Google Calendar connection status
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("status")]
        public async Task<ActionResult<GoogleCalendarStatusRM>> GetStatus(
            [FromServices] IGoogleCalendarService calendarService)
        {
            // Get userId from header
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var (isLinked, email) = await calendarService.GetConnectionStatusAsync(userId);

            return Ok(new GoogleCalendarStatusRM
            {
                IsLinked = isLinked,
                LinkedEmail = email
            });
        }

        /// <summary>
        /// Get the OAuth authorization URL to link Google Calendar
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("auth-url")]
        public ActionResult<GoogleCalendarAuthUrlRM> GetAuthUrl(
            [FromServices] IGoogleCalendarService calendarService)
        {
            // Get userId from header
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var authUrl = calendarService.GetAuthorizationUrl(userId);

            return Ok(new GoogleCalendarAuthUrlRM
            {
                AuthorizationUrl = authUrl
            });
        }

        /// <summary>
        /// OAuth callback - exchange authorization code for tokens.
        /// Also auto-syncs existing scheduled jobs to the calendar.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("callback")]
        public async Task<ActionResult<GoogleCalendarCallbackResultRM>> ExchangeCode(
            [FromBody] GoogleCalendarCallbackVM request,
            [FromServices] IGoogleCalendarService calendarService)
        {
            // Get userId from header
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { Message = "Authorization code is required" });
            }

            var (success, email, syncedJobsCount) = await calendarService.ExchangeCodeForTokensAsync(userId, request.Code);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to link Google Calendar. Please try again." });
            }

            return Ok(new GoogleCalendarCallbackResultRM
            {
                Success = true,
                LinkedEmail = email,
                SyncedJobsCount = syncedJobsCount
            });
        }

        /// <summary>
        /// Disconnect Google Calendar from the user's account
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("disconnect")]
        public async Task<ActionResult> Disconnect(
            [FromServices] IGoogleCalendarService calendarService)
        {
            // Get userId from header
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            var success = await calendarService.DisconnectCalendarAsync(userId);

            if (!success)
            {
                return BadRequest(new { Message = "Failed to disconnect Google Calendar" });
            }

            return Ok(new { Message = "Google Calendar disconnected successfully" });
        }

        /// <summary>
        /// Sync all existing scheduled jobs to Google Calendar (one-time bulk sync)
        /// Use this when a user links their calendar and wants to add existing jobs
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("sync-existing-jobs")]
        public async Task<ActionResult<SyncExistingJobsResultRM>> SyncExistingJobs(
            [FromServices] IGoogleCalendarService calendarService,
            [FromServices] DBContext ctx)
        {
            // Get userId from header
            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Verify calendar is linked
            var (isLinked, _) = await calendarService.GetConnectionStatusAsync(userId);
            if (!isLinked)
            {
                return BadRequest(new { Message = "Google Calendar is not linked. Please link your calendar first." });
            }

            // Get all jobs with scheduled dates that don't have a calendar event yet
            var jobsToSync = await ctx.Job
                .Include(j => j.client)
                .Include(j => j.location)
                .Where(j => j.companyId == companyId
                    && j.scheduledStartDate != null
                    && j.googleCalendarEventId == null
                    && j.status != null
                    && j.status.ToLower() != "cancelled"
                    && j.status.ToLower() != "completed")
                .ToListAsync();

            int synced = 0;
            int failed = 0;

            foreach (var job in jobsToSync)
            {
                try
                {
                    var eventId = await calendarService.CreateCalendarEventAsync(userId, job);
                    if (!string.IsNullOrEmpty(eventId))
                    {
                        job.googleCalendarEventId = eventId;
                        synced++;
                    }
                    else
                    {
                        failed++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GoogleCalendar] Failed to sync job {job.id}: {ex.Message}");
                    failed++;
                }
            }

            await ctx.SaveChangesAsync();

            Console.WriteLine($"[GoogleCalendar] Bulk sync completed: {synced} synced, {failed} failed");

            return Ok(new SyncExistingJobsResultRM
            {
                TotalJobsFound = jobsToSync.Count,
                Synced = synced,
                Failed = failed
            });
        }
    }

    #region ViewModels

    public class GoogleCalendarStatusRM
    {
        public bool IsLinked { get; set; }
        public string? LinkedEmail { get; set; }
    }

    public class GoogleCalendarAuthUrlRM
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
    }

    public class GoogleCalendarCallbackVM
    {
        public string Code { get; set; } = string.Empty;
        public string? State { get; set; }
    }

    public class GoogleCalendarCallbackResultRM
    {
        public bool Success { get; set; }
        public string? LinkedEmail { get; set; }
        public int SyncedJobsCount { get; set; }
    }

    public class SyncExistingJobsResultRM
    {
        public int TotalJobsFound { get; set; }
        public int Synced { get; set; }
        public int Failed { get; set; }
    }

    #endregion
}

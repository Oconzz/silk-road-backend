using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Business.Handlers;
using Business.Response;
using Business.Services;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobController : ControllerBase
    {
        private readonly IGoogleCalendarService _calendarService;
        private readonly DBContext _ctx;

        public JobController(IGoogleCalendarService calendarService, DBContext ctx)
        {
            _calendarService = calendarService;
            _ctx = ctx;
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<JobListItem>>> GetJobList(
            [FromBody] JobListRequestVM request,
            [FromServices] JobHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<JobRM>> GetJobById(
            Guid id,
            [FromServices] JobHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Job not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<JobRM>> CreateJob(
            [FromBody] CreateJobVM request,
            [FromServices] JobHandler handler,
            [FromServices] FreemiumLimitService freemiumService)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Freemium limit check
            var limitCheck = await freemiumService.CheckLimit(companyId, FreemiumResource.Jobs);
            if (!limitCheck.Allowed)
            {
                return BadRequest(new { Message = limitCheck.Message, code = "FREEMIUM_LIMIT", resource = "jobs", currentCount = limitCheck.CurrentCount, limit = limitCheck.Limit });
            }

            // Get userId from header for calendar sync
            Guid? userId = null;
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var result = await handler.CreateJobAsync(request, companyId);

            // NMJ-40: Sync to Google Calendar if user has calendar linked
            Console.WriteLine($"[JobController] CreateJob - userId: {userId?.ToString() ?? "NULL"}, result: {result?.id.ToString() ?? "NULL"}");

            if (userId.HasValue && result != null)
            {
                try
                {
                    var (isLinked, linkedEmail) = await _calendarService.GetConnectionStatusAsync(userId.Value);
                    Console.WriteLine($"[JobController] CreateJob - Calendar check: isLinked={isLinked}, email={linkedEmail ?? "NULL"}");

                    if (isLinked)
                    {
                        // Get the full job with relationships for calendar event
                        var job = await _ctx.Job
                            .Include(j => j.client)
                            .Include(j => j.location)
                            .FirstOrDefaultAsync(j => j.id == result.id);

                        if (job != null)
                        {
                            Console.WriteLine($"[JobController] CreateJob - Creating calendar event for job {job.id}, scheduledStartDate: {job.scheduledStartDate?.ToString() ?? "NULL"}");
                            var eventId = await _calendarService.CreateCalendarEventAsync(userId.Value, job);
                            Console.WriteLine($"[JobController] CreateJob - Calendar event result: {eventId ?? "NULL"}");

                            if (!string.IsNullOrEmpty(eventId))
                            {
                                // Save the event ID to the job for future updates (NMJ-41)
                                job.googleCalendarEventId = eventId;
                                await _ctx.SaveChangesAsync();
                                Console.WriteLine($"[JobController] Calendar event created for job {job.id}: {eventId}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[JobController] CreateJob - Job not found after creation: {result.id}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[JobController] CreateJob - Calendar not linked for user {userId.Value}");
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the job creation if calendar sync fails
                    Console.WriteLine($"[JobController] Calendar sync failed (non-blocking): {ex.Message}");
                    Console.WriteLine($"[JobController] Stack trace: {ex.StackTrace}");
                }
            }
            else
            {
                Console.WriteLine($"[JobController] CreateJob - Skipping calendar sync: userId.HasValue={userId.HasValue}, result!=null={result != null}");
            }

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<JobRM>> UpdateJob(
            [FromBody] UpdateJobVM request,
            [FromServices] JobHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Get userId from header for calendar sync
            Guid? userId = null;
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var result = await handler.UpdateJobAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Job not found" });

            // NMJ-41: Update Google Calendar event if user has calendar linked
            if (userId.HasValue)
            {
                try
                {
                    var (isLinked, _) = await _calendarService.GetConnectionStatusAsync(userId.Value);
                    Console.WriteLine($"[JobController] Calendar sync check - userId: {userId.Value}, isLinked: {isLinked}, requestStatus: {request.status}");

                    if (isLinked)
                    {
                        // Get the full job with relationships for calendar event
                        var job = await _ctx.Job
                            .Include(j => j.client)
                            .Include(j => j.location)
                            .FirstOrDefaultAsync(j => j.id == result.id);

                        if (job != null)
                        {
                            Console.WriteLine($"[JobController] Job {job.id} - googleCalendarEventId: {job.googleCalendarEventId ?? "NULL"}");

                            // NMJ-40: If status changed to Cancelled, delete the calendar event
                            if (request.status?.ToLower() == "cancelled" && !string.IsNullOrEmpty(job.googleCalendarEventId))
                            {
                                Console.WriteLine($"[JobController] Deleting calendar event for cancelled job {job.id}");
                                await _calendarService.DeleteCalendarEventAsync(userId.Value, job.googleCalendarEventId);
                                job.googleCalendarEventId = null;
                                await _ctx.SaveChangesAsync();
                                Console.WriteLine($"[JobController] Calendar event deleted for cancelled job {job.id}");
                            }
                            else if (!string.IsNullOrEmpty(job.googleCalendarEventId))
                            {
                                // Update existing event
                                await _calendarService.UpdateCalendarEventAsync(userId.Value, job.googleCalendarEventId, job);
                                Console.WriteLine($"[JobController] Calendar event updated for job {job.id}");
                            }
                            else if (request.status?.ToLower() != "cancelled")
                            {
                                // Create new event if none exists (and not cancelled)
                                Console.WriteLine($"[JobController] Creating new calendar event for job {job.id}");
                                var eventId = await _calendarService.CreateCalendarEventAsync(userId.Value, job);
                                if (!string.IsNullOrEmpty(eventId))
                                {
                                    job.googleCalendarEventId = eventId;
                                    await _ctx.SaveChangesAsync();
                                    Console.WriteLine($"[JobController] Calendar event created for job {job.id}: {eventId}");
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[JobController] No calendar action needed for job {job.id} (status: {request.status}, eventId: {job.googleCalendarEventId ?? "NULL"})");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[JobController] Calendar not linked for user {userId.Value}, skipping calendar sync");
                    }
                }
                catch (Exception ex)
                {
                    // Don't fail the job update if calendar sync fails
                    Console.WriteLine($"[JobController] Calendar sync failed (non-blocking): {ex.Message}");
                }
            }

            return Ok(result);
        }

        /// <summary>
        /// Find a job linked to an estimate or any of its version siblings.
        /// Used to detect if a job already exists when converting a new estimate version.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("by-estimate/{estimateId}")]
        public async Task<ActionResult> GetJobByEstimateId(
            Guid estimateId,
            [FromServices] JobHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // First, get the estimate to find version family
            var estimate = await _ctx.Estimates
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.id == estimateId && e.companyId == companyId);

            if (estimate == null)
                return NotFound(new { Message = "Estimate not found" });

            // Get the root estimate id (parent or self)
            var rootId = estimate.parentEstimateId ?? estimate.id;

            // Find all estimate ids in this version family
            var familyIds = await _ctx.Estimates
                .AsNoTracking()
                .Where(e => e.id == rootId || e.parentEstimateId == rootId)
                .Select(e => e.id)
                .ToListAsync();

            // Find a job linked to any estimate in this family
            var job = await _ctx.Job
                .Include(j => j.client)
                .Include(j => j.location)
                .AsNoTracking()
                .Where(j => j.companyId == companyId && j.estimateId.HasValue && familyIds.Contains(j.estimateId.Value))
                .OrderByDescending(j => j.createDatetime)
                .FirstOrDefaultAsync();

            if (job == null)
                return Ok(new { found = false });

            var result = await handler.GetJobByIdAsync(job.id, companyId);
            return Ok(new { found = true, job = result });
        }

        /// <summary>
        /// Get the expanded occurrences for a recurring job.
        /// All recurrence logic is computed server-side using Ical.Net.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}/occurrences")]
        public async Task<ActionResult> GetJobOccurrences(
            Guid id,
            [FromServices] JobHandler handler,
            [FromServices] RecurrenceService recurrenceService,
            [FromQuery] int maxOccurrences = 52)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var job = await handler.GetJobByIdAsync(id, companyId);
            if (job == null)
                return NotFound(new { Message = "Job not found" });

            if (!job.isRecurring || string.IsNullOrEmpty(job.recurrenceRule))
                return Ok(new { occurrences = new List<DateTime>(), isRecurring = false });

            var startDate = job.scheduledStartDate ?? DateTime.UtcNow;
            var occurrences = recurrenceService.GetOccurrences(
                startDate,
                job.recurrenceRule,
                job.recurrenceEndDate,
                job.timeZone,
                maxOccurrences);

            return Ok(new
            {
                jobId = job.id,
                isRecurring = true,
                recurrenceRule = job.recurrenceRule,
                timeZone = job.timeZone,
                occurrences = occurrences
            });
        }

        /// <summary>
        /// Set recurrence on an existing job. Validates RRULE server-side.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("{id}/recurrence")]
        public async Task<ActionResult<JobRM>> SetJobRecurrence(
            Guid id,
            [FromBody] SetRecurrenceVM request,
            [FromServices] JobHandler handler,
            [FromServices] RecurrenceService recurrenceService)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Validate RRULE if provided
            if (!string.IsNullOrEmpty(request.recurrenceRule) && !recurrenceService.ValidateRRule(request.recurrenceRule))
            {
                return BadRequest(new { Message = "Invalid recurrence rule (RRULE)" });
            }

            var updateVM = new UpdateJobVM
            {
                id = id,
                isRecurring = !string.IsNullOrEmpty(request.recurrenceRule),
                recurrenceRule = request.recurrenceRule,
                recurrenceEndDate = request.recurrenceEndDate,
                timeZone = request.timeZone
            };

            var result = await handler.UpdateJobAsync(updateVM, companyId);
            if (result == null)
                return NotFound(new { Message = "Job not found" });

            return Ok(result);
        }

        /// <summary>
        /// Skip a visit (child job) from a recurring parent job.
        /// Sets the child job status to "skipped".
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("{id}/skip")]
        public async Task<ActionResult<JobRM>> SkipVisit(
            Guid id,
            [FromServices] JobHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var job = await handler.GetJobByIdAsync(id, companyId);
            if (job == null)
                return NotFound(new { Message = "Job not found" });

            // Only child jobs (visits) can be skipped
            if (job.parentJobId == null)
                return BadRequest(new { Message = "Only child jobs (visits) can be skipped. This is a parent job." });

            // Don't skip already completed or cancelled jobs
            if (job.status == "completed" || job.status == "cancelled")
                return BadRequest(new { Message = $"Cannot skip a job with status '{job.status}'" });

            var result = await handler.UpdateJobAsync(new UpdateJobVM
            {
                id = id,
                status = "skipped"
            }, companyId);

            if (result == null)
                return NotFound(new { Message = "Job not found" });

            return Ok(result);
        }

        /// <summary>
        /// Unskip a visit (child job) - restore it to scheduled status.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("{id}/unskip")]
        public async Task<ActionResult<JobRM>> UnskipVisit(
            Guid id,
            [FromServices] JobHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var job = await handler.GetJobByIdAsync(id, companyId);
            if (job == null)
                return NotFound(new { Message = "Job not found" });

            if (job.status != "skipped")
                return BadRequest(new { Message = "Only skipped jobs can be unskipped" });

            var result = await handler.UpdateJobAsync(new UpdateJobVM
            {
                id = id,
                status = "scheduled"
            }, companyId);

            if (result == null)
                return NotFound(new { Message = "Job not found" });

            return Ok(result);
        }

        /// <summary>
        /// Get all child jobs created from a recurring parent job.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}/children")]
        public async Task<ActionResult> GetChildJobs(
            Guid id,
            [FromServices] JobHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var parentJob = await handler.GetJobByIdAsync(id, companyId);
            if (parentJob == null)
                return NotFound(new { Message = "Job not found" });

            var children = await handler.GetChildJobsAsync(id, companyId);
            return Ok(new
            {
                parentJobId = id,
                parentJobTitle = parentJob.title,
                isRecurring = parentJob.isRecurring,
                childJobs = children
            });
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteJob(
            Guid id,
            [FromServices] JobHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Get userId from header for calendar sync
            Guid? userId = null;
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) &&
                Guid.TryParse(userIdHeader, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            // NMJ-40: Get the job's calendar event ID BEFORE deleting
            string? eventIdToDelete = null;
            var jobToDelete = await _ctx.Job.FindAsync(id);
            if (jobToDelete != null)
            {
                eventIdToDelete = jobToDelete.googleCalendarEventId;
            }

            var result = await handler.DeleteJobAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Job not found" });

            // NMJ-40: Delete from Google Calendar if event exists
            if (userId.HasValue && !string.IsNullOrEmpty(eventIdToDelete))
            {
                try
                {
                    await _calendarService.DeleteCalendarEventAsync(userId.Value, eventIdToDelete);
                    Console.WriteLine($"[JobController] Calendar event deleted for job {id}: {eventIdToDelete}");
                }
                catch (Exception ex)
                {
                    // Don't fail the job deletion if calendar sync fails
                    Console.WriteLine($"[JobController] Calendar delete failed (non-blocking): {ex.Message}");
                }
            }

            return Ok(new { Message = "Job deleted successfully" });
        }
    }
}

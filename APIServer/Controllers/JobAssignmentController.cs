using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobAssignmentController : ControllerBase
    {
        private Guid GetRequestingUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token");
            }
            return userId;
        }

        private Guid? GetCompanyId()
        {
            if (Request.Headers.TryGetValue("X-Company-Id", out var headerValue) && Guid.TryParse(headerValue, out var companyId))
            {
                return companyId;
            }
            return null;
        }

        /// <summary>
        /// Assign a team member to a job (Admin/Leader only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("assign")]
        public async Task<ActionResult<JobAssignmentRM>> AssignTeamMember(
            [FromBody] AssignTeamMemberVM request,
            [FromServices] JobAssignmentHandler handler,
            [FromServices] NotificationService notificationService)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var assignedByUserId = GetRequestingUserId();
            var result = await handler.AssignTeamMemberAsync(request, companyId.Value, assignedByUserId);

            if (result == null)
            {
                return NotFound(new { message = "Job or team member not found" });
            }

            // Fire notification to the assigned user
            _ = Task.Run(async () =>
            {
                try
                {
                    await notificationService.TriggerJobAssignment(
                        request.userId, companyId.Value, request.jobId, result.jobTitle ?? "a job");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[JobAssignment] Notification failed: {ex.Message}");
                }
            });

            return Ok(result);
        }

        /// <summary>
        /// Unassign a team member from a job (Admin/Leader only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("unassign")]
        public async Task<ActionResult> UnassignTeamMember(
            [FromBody] UnassignTeamMemberVM request,
            [FromServices] JobAssignmentHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var success = await handler.UnassignTeamMemberAsync(request, companyId.Value);

            if (!success)
            {
                return NotFound(new { message = "Assignment not found" });
            }

            return Ok(new { message = "Team member unassigned successfully" });
        }

        /// <summary>
        /// Get all team members assigned to a specific job (Admin/Leader only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("job/{jobId}")]
        public async Task<ActionResult<List<AssignedTeamMemberRM>>> GetJobAssignments(
            Guid jobId,
            [FromServices] JobAssignmentHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobAssignmentsAsync(jobId, companyId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Get jobs assigned to the current user (for non-admin users)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("my-jobs")]
        public async Task<ActionResult<PagedResponse<JobListItem>>> GetMyAssignedJobs(
            [FromBody] UserJobsRequestVM request,
            [FromServices] JobAssignmentHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var userId = GetRequestingUserId();
            var result = await handler.GetUserAssignedJobsAsync(request, userId, companyId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Get all assignments with optional filters (Admin/Leader only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<JobAssignmentListItemRM>>> GetAssignments(
            [FromBody] JobAssignmentListRequestVM request,
            [FromServices] JobAssignmentHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var result = await handler.GetAssignmentsAsync(request, companyId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Check if current user is assigned to a job
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("check/{jobId}")]
        public async Task<ActionResult<object>> CheckAssignment(
            Guid jobId,
            [FromServices] JobAssignmentHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var userId = GetRequestingUserId();
            var isAssigned = await handler.IsUserAssignedToJobAsync(userId, jobId, companyId.Value);
            return Ok(new { isAssigned });
        }

        /// <summary>
        /// Get job details for an assigned job (Crew can only see jobs they are assigned to)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("my-job/{jobId}")]
        public async Task<ActionResult<JobRM>> GetMyAssignedJobDetails(
            Guid jobId,
            [FromServices] JobAssignmentHandler assignmentHandler,
            [FromServices] JobHandler jobHandler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var userId = GetRequestingUserId();
            
            // Check if user is assigned to this job
            var isAssigned = await assignmentHandler.IsUserAssignedToJobAsync(userId, jobId, companyId.Value);
            if (!isAssigned)
            {
                return Forbid();
            }

            // Get job details
            var result = await jobHandler.GetJobByIdAsync(jobId, companyId.Value);
            if (result == null)
            {
                return NotFound(new { message = "Job not found" });
            }

            return Ok(result);
        }
    }
}

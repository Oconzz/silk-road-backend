using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers;

[ApiController]
[Route("[controller]")]
public class GoalController : ControllerBase
{
    private readonly IConfiguration _config;

    public GoalController(IConfiguration config)
    {
        _config = config;
    }

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
    /// Create a new goal for a job (Admin/Leader only)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
    [HttpPost("create")]
    public async Task<ActionResult<GoalRM>> CreateGoal(
        [FromBody] CreateGoalVM request,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.CreateGoalAsync(request, companyId.Value, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing goal (Admin/Leader only)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
    [HttpPut("{goalId}")]
    public async Task<ActionResult<GoalRM>> UpdateGoal(
        Guid goalId,
        [FromBody] UpdateGoalVM request,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            var result = await handler.UpdateGoalAsync(goalId, request, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complete a goal (any team member can complete goals assigned to them)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpPost("{goalId}/complete")]
    public async Task<ActionResult<GoalRM>> CompleteGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.CompleteGoalAsync(goalId, companyId.Value, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reopen a completed goal (Admin/Leader only)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
    [HttpPost("{goalId}/reopen")]
    public async Task<ActionResult<GoalRM>> ReopenGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            var result = await handler.ReopenGoalAsync(goalId, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a goal (Admin/Leader only)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
    [HttpDelete("{goalId}")]
    public async Task<ActionResult> DeleteGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            await handler.DeleteGoalAsync(goalId, companyId.Value);
            return Ok(new { message = "Goal deleted successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific goal by ID
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpGet("{goalId}")]
    public async Task<ActionResult<GoalRM>> GetGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            var result = await handler.GetGoalByIdAsync(goalId, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all goals for a job
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpGet("job/{jobId}")]
    public async Task<ActionResult<List<GoalRM>>> GetJobGoals(
        Guid jobId,
        [FromQuery] string? status,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            var result = await handler.GetGoalsByJobAsync(jobId, companyId.Value, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get goals summary for a job
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpGet("job/{jobId}/summary")]
    public async Task<ActionResult<JobGoalsSummaryRM>> GetJobGoalsSummary(
        Guid jobId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        try
        {
            var result = await handler.GetJobGoalsSummaryAsync(jobId, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get goals assigned to the current user (My Goals)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpGet("my-goals")]
    public async Task<ActionResult<List<GoalRM>>> GetMyGoals(
        [FromQuery] string? status,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.GetMyGoalsAsync(userId, companyId.Value, status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get goals summary for the current user
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpGet("my-goals/summary")]
    public async Task<ActionResult<UserGoalsSummaryRM>> GetMyGoalsSummary(
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.GetUserGoalsSummaryAsync(userId, companyId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Bulk create goals for a job (Admin/Leader only)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
    [HttpPost("job/{jobId}/bulk")]
    public async Task<ActionResult<List<GoalRM>>> BulkCreateGoals(
        Guid jobId,
        [FromBody] List<CreateGoalVM> goals,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.BulkCreateGoalsAsync(jobId, goals, companyId.Value, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Vote on a team goal (any team member assigned to the job or admin)
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
    [HttpPost("{goalId}/vote")]
    public async Task<ActionResult<GoalRM>> VoteGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.VoteGoalAsync(goalId, companyId.Value, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Force complete a goal (Admin only) - bypasses voting requirement
    /// </summary>
    [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
    [HttpPost("{goalId}/force-complete")]
    public async Task<ActionResult<GoalRM>> ForceCompleteGoal(
        Guid goalId,
        [FromServices] GoalHandler handler)
    {
        var companyId = GetCompanyId();
        if (!companyId.HasValue)
        {
            return BadRequest(new { message = "X-Company-Id header is required" });
        }

        var userId = GetRequestingUserId();

        try
        {
            var result = await handler.ForceCompleteGoalAsync(goalId, companyId.Value, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

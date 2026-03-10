using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EstimatesController : ControllerBase
    {
        public EstimatesController() { }

        /// <summary>
        /// Get paginated list of estimates
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<EstimateListItem>>> GetEstimateList(
            [FromBody] EstimateListRequestVM request,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetEstimateListAsync(request, companyId);
            return Ok(result);
        }

        /// <summary>
        /// Get estimate by ID
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<EstimateRM>> GetEstimateById(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetEstimateByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Estimate not found" });

            return Ok(result);
        }

        /// <summary>
        /// Create a new estimate
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<EstimateRM>> CreateEstimate(
            [FromBody] CreateEstimateVM request,
            [FromServices] EstimateHandler handler,
            [FromServices] FreemiumLimitService freemiumService)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Freemium limit check
            var limitCheck = await freemiumService.CheckLimit(companyId, FreemiumResource.Estimates);
            if (!limitCheck.Allowed)
            {
                return BadRequest(new { Message = limitCheck.Message, code = "FREEMIUM_LIMIT", resource = "estimates", currentCount = limitCheck.CurrentCount, limit = limitCheck.Limit });
            }

            try
            {
                var result = await handler.CreateEstimateAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing estimate
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<EstimateRM>> UpdateEstimate(
            [FromBody] UpdateEstimateVM request,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateEstimateAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Estimate not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete an estimate
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEstimate(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteEstimateAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Estimate not found" });

            return Ok(new { Message = "Estimate deleted successfully" });
        }

        /// <summary>
        /// Create a new version of an estimate. Original stays as baseline.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("{id}/create-version")]
        public async Task<ActionResult<EstimateRM>> CreateVersion(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateNewVersionAsync(id, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Get version history for an estimate chain.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}/versions")]
        public async Task<ActionResult> GetVersionHistory(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.GetVersionHistoryAsync(id, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Check inventory overallocation before converting estimate to job.
        /// Returns list of items that are also allocated to other active jobs.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}/check-overallocation")]
        public async Task<ActionResult> CheckOverallocation(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CheckOverallocationAsync(id, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Convert an estimate to a job
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("convert-to-job")]
        public async Task<ActionResult<JobRM>> ConvertEstimateToJob(
            [FromBody] ConvertEstimateToJobVM request,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.ConvertEstimateToJobAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Generate a public share link for an estimate
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("{id}/generate-share-link")]
        public async Task<ActionResult<EstimateRM>> GenerateShareLink(
            Guid id,
            [FromServices] EstimateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.GenerateShareLinkAsync(id, companyId);
                if (result == null)
                    return NotFound(new { Message = "Estimate not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Get estimate by public share ID (no authentication required)
        /// </summary>
        [HttpGet("public/{shareId}")]
        public async Task<ActionResult<EstimateRM>> GetEstimateByShareId(
            string shareId,
            [FromServices] EstimateHandler handler,
            [FromServices] NotificationService notificationService)
        {
            try
            {
                var result = await handler.GetEstimateByShareIdAsync(shareId);
                if (result == null)
                    return NotFound(new { Message = "Estimate not found or link has expired" });

                // Fire estimate viewed notification (non-blocking)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await notificationService.TriggerEstimateViewed(
                            result.companyId, result.id, result.name ?? "", result.client?.contactName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[EstimateViewed] Notification failed: {ex.Message}");
                    }
                });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// View estimate as HTML page (no authentication required)
        /// </summary>
        [HttpGet("view/{shareId}")]
        public async Task<IActionResult> ViewEstimateHtml(
            string shareId,
            [FromServices] EstimateHandler handler)
        {
            try
            {
                var html = await handler.GetEstimateHtmlByShareIdAsync(shareId);
                if (html == null)
                    return NotFound("Estimate not found or link has expired");

                // Note: TriggerEstimateViewed is already called via GetEstimateByShareId
                // The HTML view also increments viewCount in the handler
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Approve estimate via public share link (no authentication required)
        /// Only changes status to approved, does NOT convert to job.
        /// </summary>
        [HttpPost("public/{shareId}/approve")]
        public async Task<ActionResult> ApproveEstimateByShareId(
            string shareId,
            [FromServices] EstimateHandler handler,
            [FromServices] NotificationService notificationService)
        {
            try
            {
                // Get estimate data before approval for notification
                var estimate = await handler.GetEstimateByShareIdAsync(shareId);
                
                var result = await handler.ApproveEstimateByShareIdAsync(shareId);
                if (!result)
                    return NotFound(new { Message = "Estimate not found" });

                // Fire estimate approved notification (non-blocking)
                if (estimate != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await notificationService.TriggerEstimateApproved(
                                estimate.companyId, estimate.id, estimate.name ?? "", estimate.client?.contactName);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[EstimateApproved] Notification failed: {ex.Message}");
                        }
                    });
                }

                return Ok(new { Message = "Estimate approved successfully", approved = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

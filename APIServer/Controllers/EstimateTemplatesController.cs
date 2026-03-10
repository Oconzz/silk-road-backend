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
    public class EstimateTemplatesController : ControllerBase
    {
        public EstimateTemplatesController() { }

        /// <summary>
        /// Get paginated list of estimate templates
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<EstimateTemplateListItem>>> GetEstimateTemplateList(
            [FromBody] EstimateTemplateListRequestVM request,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetEstimateTemplateListAsync(request, companyId);
            return Ok(result);
        }

        /// <summary>
        /// Get estimate template by ID
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<EstimateTemplateRM>> GetEstimateTemplateById(
            Guid id,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetEstimateTemplateByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Estimate template not found" });

            return Ok(result);
        }

        /// <summary>
        /// Create a new estimate template
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<EstimateTemplateRM>> CreateEstimateTemplate(
            [FromBody] CreateEstimateTemplateVM request,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateEstimateTemplateAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing estimate template
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<EstimateTemplateRM>> UpdateEstimateTemplate(
            [FromBody] UpdateEstimateTemplateVM request,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateEstimateTemplateAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Estimate template not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete an estimate template
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteEstimateTemplate(
            Guid id,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteEstimateTemplateAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Estimate template not found" });

            return Ok(new { Message = "Estimate template deleted successfully" });
        }

        /// <summary>
        /// Create a template from an existing estimate
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("createFromEstimate")]
        public async Task<ActionResult<EstimateTemplateRM>> CreateFromEstimate(
            [FromBody] CreateTemplateFromEstimateVM request,
            [FromServices] EstimateTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateFromEstimateAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

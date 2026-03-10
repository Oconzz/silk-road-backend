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
    public class CompanyConfigController : ControllerBase
    {
        public CompanyConfigController() { }

        /// <summary>
        /// Get company configuration for the current company
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet]
        public async Task<ActionResult<CompanyConfigRM>> GetCompanyConfig(
            [FromServices] CompanyConfigHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetCompanyConfigAsync(companyId);
            if (result == null)
                return NotFound(new { Message = "Company configuration not found" });

            return Ok(result);
        }

        /// <summary>
        /// Create company configuration
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<CompanyConfigRM>> CreateCompanyConfig(
            [FromBody] CreateCompanyConfigVM request,
            [FromServices] CompanyConfigHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateCompanyConfigAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update company configuration
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<CompanyConfigRM>> UpdateCompanyConfig(
            [FromBody] UpdateCompanyConfigVM request,
            [FromServices] CompanyConfigHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.UpdateCompanyConfigAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Company configuration not found" });

            return Ok(result);
        }

        /// <summary>
        /// Delete company configuration
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpDelete]
        public async Task<ActionResult> DeleteCompanyConfig(
            [FromServices] CompanyConfigHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteCompanyConfigAsync(companyId);
            if (!result)
                return NotFound(new { Message = "Company configuration not found" });

            return Ok(new { Message = "Company configuration deleted successfully" });
        }
    }
}

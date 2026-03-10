using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for Quick Start Templates - global read-only templates for quick estimate creation
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class QuickStartTemplatesController : ControllerBase
    {
        public QuickStartTemplatesController() { }

        /// <summary>
        /// Get all templates for a specific category
        /// </summary>
        /// <param name="category">Business category (e.g., "Painting", "Roofing")</param>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("list")]
        public async Task<ActionResult<List<QuickStartTemplateRM>>> GetTemplatesByCategory(
            [FromQuery] string category,
            [FromServices] QuickStartTemplateHandler handler)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                return BadRequest(new { Message = "Category parameter is required" });
            }

            var result = await handler.GetTemplatesByCategoryAsync(category);
            return Ok(result);
        }

        /// <summary>
        /// Copy a Quick Start Template to create a new draft estimate
        /// </summary>
        /// <param name="id">Template ID to copy</param>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("copy/{id}")]
        public async Task<ActionResult<EstimateRM>> CopyTemplateToDraft(
            Guid id,
            [FromServices] QuickStartTemplateHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.CopyTemplateToDraftAsync(id, companyId);
            
            if (result == null)
            {
                return NotFound(new { Message = "Template not found or inactive" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get a specific template by ID
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<QuickStartTemplateRM>> GetTemplateById(
            Guid id,
            [FromServices] QuickStartTemplateHandler handler)
        {
            var result = await handler.GetTemplateByIdAsync(id);
            
            if (result == null)
            {
                return NotFound(new { Message = "Template not found" });
            }

            return Ok(result);
        }
    }
}


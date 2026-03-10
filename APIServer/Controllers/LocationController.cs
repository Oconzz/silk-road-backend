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
    public class LocationController : ControllerBase
    {
        public LocationController()
        {
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<LocationListItem>>> GetLocationList(
            [FromBody] LocationListRequestVM request,
            [FromServices] LocationHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetLocationListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationRM>> GetLocationById(
            Guid id,
            [FromServices] LocationHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetLocationByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Location not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<LocationRM>> CreateLocation(
            [FromBody] CreateLocationVM request,
            [FromServices] LocationHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.CreateLocationAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<LocationRM>> UpdateLocation(
            [FromBody] UpdateLocationVM request,
            [FromServices] LocationHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.UpdateLocationAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Location not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteLocation(
            Guid id,
            [FromServices] LocationHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteLocationAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Location not found" });

            return Ok(new { Message = "Location deleted successfully" });
        }
    }
}

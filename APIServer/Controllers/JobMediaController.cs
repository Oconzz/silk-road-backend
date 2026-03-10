using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobMediaController : ControllerBase
    {
        public JobMediaController() { }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<List<JobMediaRM>>> GetJobMediaList(
            [FromBody] JobMediaListRequestVM request,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobMediaListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<JobMediaRM>> CreateJobMedia(
            [FromBody] CreateJobMediaVM request,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateJobMediaAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<JobMediaRM>> UpdateJobMedia(
            [FromBody] UpdateJobMediaVM request,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateJobMediaAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Job media not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteJobMedia(
            Guid id,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteJobMediaAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Job media not found" });

            return Ok(new { Message = "Job media deleted successfully" });
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("albums/{jobId}")]
        public async Task<ActionResult<List<JobAlbumRM>>> GetJobAlbums(
            Guid jobId,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetJobAlbumsAsync(jobId, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("albums/create")]
        public async Task<ActionResult<JobAlbumRM>> CreateJobAlbum(
            [FromBody] CreateJobAlbumVM request,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateJobAlbumAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpDelete("albums/{id}")]
        public async Task<ActionResult> DeleteJobAlbum(
            Guid id,
            [FromServices] JobMediaHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteJobAlbumAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Job album not found" });

            return Ok(new { Message = "Job album deleted successfully" });
        }
    }
}

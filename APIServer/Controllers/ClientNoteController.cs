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
    public class ClientNoteController : ControllerBase
    {
        public ClientNoteController() { }

        /// <summary>
        /// Get paginated list of client notes
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<ClientNoteListItem>>> GetClientNoteList(
            [FromBody] ClientNoteListRequestVM request,
            [FromServices] ClientNoteHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetClientNoteListAsync(request, companyId);
            return Ok(result);
        }

        /// <summary>
        /// Get client note by ID
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientNoteRM>> GetClientNoteById(
            Guid id,
            [FromServices] ClientNoteHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetClientNoteByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Client note not found" });

            return Ok(result);
        }

        /// <summary>
        /// Create a new client note
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<ClientNoteRM>> CreateClientNote(
            [FromBody] CreateClientNoteVM request,
            [FromServices] ClientNoteHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (!Request.Headers.TryGetValue("X-User-Id", out var userIdHeader) ||
                !Guid.TryParse(userIdHeader, out var userId))
            {
                return BadRequest(new { Message = "X-User-Id header is required" });
            }

            try
            {
                var result = await handler.CreateClientNoteAsync(request, companyId, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing client note
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<ClientNoteRM>> UpdateClientNote(
            [FromBody] UpdateClientNoteVM request,
            [FromServices] ClientNoteHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateClientNoteAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Client note not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a client note
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteClientNote(
            Guid id,
            [FromServices] ClientNoteHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteClientNoteAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Client note not found" });

            return Ok(new { Message = "Client note deleted successfully" });
        }
    }
}

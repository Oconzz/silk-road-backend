using APIServer.AuthAttributes;
using APIServer.Services;
using AutoMapper;
using Business.Data;
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
    public class ClientController : ControllerBase
    {
        public ClientController()
        {
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<ClientListItem>>> GetClientList(
            [FromBody] ClientListRequestVM request,
            [FromServices] ClientHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetClientListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<ClientRM>> GetClientById(
            Guid id,
            [FromServices] ClientHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetClientByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Client not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<ClientRM>> CreateClient(
            [FromBody] CreateClientVM request,
            [FromServices] ClientHandler handler,
            [FromServices] FreemiumLimitService freemiumService)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Freemium limit check
            var limitCheck = await freemiumService.CheckLimit(companyId, FreemiumResource.Clients);
            if (!limitCheck.Allowed)
            {
                return BadRequest(new { Message = limitCheck.Message, code = "FREEMIUM_LIMIT", resource = "clients", currentCount = limitCheck.CurrentCount, limit = limitCheck.Limit });
            }

            var result = await handler.CreateClientAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<ClientRM>> UpdateClient(
            [FromBody] UpdateClientVM request,
            [FromServices] ClientHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.UpdateClientAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Client not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteClient(
            Guid id,
            [FromServices] ClientHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteClientAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Client not found" });

            return Ok(new { Message = "Client deleted successfully" });
        }

/// <summary>
        /// Bulk create clients from imported contacts
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("bulk")]
        public async Task<ActionResult<BulkCreateClientResultVM>> BulkCreateClients(
            [FromBody] BulkCreateClientVM request,
            [FromServices] ClientHandler handler,
            [FromServices] FreemiumLimitService freemiumService)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Validate request
            if (request?.clients == null || request.clients.Count == 0)
            {
                return BadRequest(new { Message = "At least one client is required" });
            }

            // Limit batch size to prevent abuse
            if (request.clients.Count > 100)
            {
                return BadRequest(new { Message = "Maximum 100 clients can be imported at once" });
            }

            // Freemium limit check (check if adding these would exceed limit)
            var limitCheck = await freemiumService.CheckLimit(companyId, FreemiumResource.Clients);
            if (!limitCheck.Allowed)
            {
                return BadRequest(new { Message = limitCheck.Message, code = "FREEMIUM_LIMIT", resource = "clients", currentCount = limitCheck.CurrentCount, limit = limitCheck.Limit });
            }

            var result = await handler.BulkCreateClientsAsync(request, companyId);
            return Ok(result);
        }

        // Location endpoints for specific client
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("{clientId}/locations")]
        public async Task<ActionResult<List<LocationRM>>> GetClientLocations(
            Guid clientId,
            [FromServices] LocationHandler locationHandler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var locations = await locationHandler.GetLocationsByClientIdAsync(clientId, companyId);
            return Ok(locations);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("{clientId}/locations")]
        public async Task<ActionResult<LocationRM>> CreateLocationForClient(
            Guid clientId,
            [FromBody] CreateLocationVM request,
            [FromServices] LocationHandler locationHandler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Set clientId in the request
            request.clientId = clientId;
            var result = await locationHandler.CreateLocationAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPut("locations/{id}")]
        public async Task<ActionResult<LocationRM>> UpdateClientLocation(
            Guid id,
            [FromBody] UpdateLocationVM request,
            [FromServices] LocationHandler locationHandler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            request.id = id;
            var result = await locationHandler.UpdateLocationAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Location not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("locations/{id}")]
        public async Task<ActionResult> DeleteClientLocation(
            Guid id,
            [FromServices] LocationHandler locationHandler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await locationHandler.DeleteLocationAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Location not found" });

            return Ok(new { Message = "Location deleted successfully" });
        }
    }
}

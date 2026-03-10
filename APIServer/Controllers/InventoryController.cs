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
    public class InventoryController : ControllerBase
    {
        public InventoryController() { }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<InventoryListItem>>> GetInventoryList(
            [FromBody] InventoryListRequestVM request,
            [FromServices] InventoryHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetInventoryListAsync(request, companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("stats")]
        public async Task<ActionResult<InventoryStatsRM>> GetInventoryStats(
            [FromServices] InventoryHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetInventoryStatsAsync(companyId);
            return Ok(result);
        }

        /// <summary>
        /// Get inventory items that are allocated to active jobs.
        /// Shows which items are reserved and by which jobs.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("allocated")]
        public async Task<ActionResult> GetAllocatedInventory(
            [FromServices] InventoryHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetAllocatedInventoryAsync(companyId);
            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<InventoryRM>> GetInventoryById(
            Guid id,
            [FromServices] InventoryHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetInventoryByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Inventory not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<InventoryRM>> CreateInventory(
            [FromBody] CreateInventoryVM request,
            [FromServices] InventoryHandler handler,
            [FromServices] FreemiumLimitService freemiumService)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            // Freemium limit check
            var limitCheck = await freemiumService.CheckLimit(companyId, FreemiumResource.Inventory);
            if (!limitCheck.Allowed)
            {
                return BadRequest(new { Message = limitCheck.Message, code = "FREEMIUM_LIMIT", resource = "inventory", currentCount = limitCheck.CurrentCount, limit = limitCheck.Limit });
            }

            try
            {
                var result = await handler.CreateInventoryAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<InventoryRM>> UpdateInventory(
            [FromBody] UpdateInventoryVM request,
            [FromServices] InventoryHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.UpdateInventoryAsync(request, companyId);
            if (result == null)
                return NotFound(new { Message = "Inventory not found" });

            return Ok(result);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInventory(
            Guid id,
            [FromServices] InventoryHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteInventoryAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Inventory not found" });

            return Ok(new { Message = "Inventory deleted successfully" });
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("move")]
        public async Task<ActionResult<InventoryRM>> MoveInventory(
            [FromBody] MoveInventoryVM request,
            [FromServices] InventoryHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.MoveInventoryAsync(request, companyId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("add-quantity")]
        public async Task<ActionResult<InventoryRM>> AddQuantity(
            [FromBody] AddQuantityVM request,
            [FromServices] InventoryHandler handler)
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
                var result = await handler.AddQuantityAsync(request, companyId, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("usage")]
        public async Task<ActionResult<InventoryUsageHistoryRM>> RecordInventoryUsage(
            [FromBody] RecordInventoryUsageVM request,
            [FromServices] InventoryHandler handler)
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
                var result = await handler.RecordInventoryUsageAsync(request, companyId, userId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<InventoryUsageHistoryRM>>> GetInventoryUsageHistory(
            Guid id,
            [FromServices] InventoryHandler handler)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetInventoryUsageHistoryAsync(id, companyId);
            return Ok(result);
        }
    }
}

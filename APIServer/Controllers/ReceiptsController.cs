using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReceiptsController : ControllerBase
    {
        public ReceiptsController() { }

        /// <summary>
        /// Get paginated list of receipts with optional search and date filtering
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<ReceiptListItem>>> GetReceiptList(
            [FromBody] ReceiptListRequestVM request,
            [FromServices] ReceiptHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetReceiptListAsync(request, companyId);
            return Ok(result);
        }

        /// <summary>
        /// Get receipt by ID
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet("{id}")]
        public async Task<ActionResult<ReceiptRM>> GetReceiptById(
            Guid id,
            [FromServices] ReceiptHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.GetReceiptByIdAsync(id, companyId);
            if (result == null)
                return NotFound(new { Message = "Receipt not found" });

            return Ok(result);
        }

        /// <summary>
        /// Upload a new receipt image
        /// Accepts base64 encoded image (JPEG, PNG, etc.) up to 10MB
        /// Uploads to public S3 bucket and stores metadata in database
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("create")]
        public async Task<ActionResult<ReceiptRM>> CreateReceipt(
            [FromBody] CreateReceiptVM request,
            [FromServices] ReceiptHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.CreateReceiptAsync(request, companyId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while uploading the receipt", Details = ex.Message });
            }
        }

        /// <summary>
        /// Update receipt metadata and/or replace the image
        /// If new image provided, old image is deleted from S3
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPut("update")]
        public async Task<ActionResult<ReceiptRM>> UpdateReceipt(
            [FromBody] UpdateReceiptVM request,
            [FromServices] ReceiptHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            try
            {
                var result = await handler.UpdateReceiptAsync(request, companyId);
                if (result == null)
                    return NotFound(new { Message = "Receipt not found" });

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while updating the receipt", Details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a receipt
        /// Removes both the database record and the file from S3
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteReceipt(
            Guid id,
            [FromServices] ReceiptHandler handler)
        {
            // Get companyId from header
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var result = await handler.DeleteReceiptAsync(id, companyId);
            if (!result)
                return NotFound(new { Message = "Receipt not found" });

            return Ok(new { Message = "Receipt deleted successfully" });
        }

    }
}

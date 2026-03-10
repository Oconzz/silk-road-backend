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
    public class ProductsController : ControllerBase
    {
        public ProductsController() { }

        /// <summary>
        /// Get paginated list of products (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, false })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<ProductListItem>>> GetProductList(
            [FromBody] ProductListRequestVM request,
            [FromServices] ProductHandler handler)
        {
            var result = await handler.GetProductListAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Get product by ID (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, false })]
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductRM>> GetProductById(
            Guid id,
            [FromServices] ProductHandler handler)
        {
            var result = await handler.GetProductByIdAsync(id);
            if (result == null)
                return NotFound(new { Message = "Product not found" });

            return Ok(result);
        }

        /// <summary>
        /// Get product by barcode (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, false })]
        [HttpGet("barcode/{barcode}")]
        public async Task<ActionResult<ProductRM>> GetProductByBarcode(
            string barcode,
            [FromServices] ProductHandler handler)
        {
            var result = await handler.GetProductByBarcodeAsync(barcode);
            if (result == null)
                return NotFound(new { Message = "Product not found" });

            return Ok(result);
        }

        /// <summary>
        /// Create a new product (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPost("create")]
        public async Task<ActionResult<ProductRM>> CreateProduct(
            [FromBody] CreateProductVM request,
            [FromServices] ProductHandler handler)
        {
            try
            {
                var result = await handler.CreateProductAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing product (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPut("update")]
        public async Task<ActionResult<ProductRM>> UpdateProduct(
            [FromBody] UpdateProductVM request,
            [FromServices] ProductHandler handler)
        {
            try
            {
                var result = await handler.UpdateProductAsync(request);
                if (result == null)
                    return NotFound(new { Message = "Product not found" });

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        /// <summary>
        /// Delete a product (Global - no companyId required)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteProduct(
            Guid id,
            [FromServices] ProductHandler handler)
        {
            try
            {
                var result = await handler.DeleteProductAsync(id);
                if (!result)
                    return NotFound(new { Message = "Product not found" });

                return Ok(new { Message = "Product deleted successfully" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}

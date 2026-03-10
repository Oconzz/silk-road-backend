using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Business.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryCategoryController : ControllerBase
    {
        private readonly DBContext _db;

        public InventoryCategoryController(DBContext db)
        {
            _db = db;
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpGet]
        public async Task<ActionResult> GetCategories()
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var categories = await _db.InventoryCategories
                .Where(c => c.companyId == companyId)
                .OrderBy(c => c.name)
                .Select(c => new { c.id, c.name, c.color })
                .ToListAsync();

            return Ok(categories);
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost]
        public async Task<ActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { Message = "Category name is required" });
            }

            // Check for duplicate name
            var exists = await _db.InventoryCategories
                .AnyAsync(c => c.companyId == companyId && c.name == request.Name.Trim());
            if (exists)
            {
                return Conflict(new { Message = "A category with this name already exists" });
            }

            var category = new InventoryCategory
            {
                companyId = companyId,
                name = request.Name.Trim(),
                color = request.Color ?? "#3B82F6",
                createDatetime = DateTime.UtcNow,
                updateDatetime = DateTime.UtcNow,
            };

            _db.InventoryCategories.Add(category);
            await _db.SaveChangesAsync();

            return Ok(new { category.id, category.name, category.color });
        }

        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCategory(Guid id)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var category = await _db.InventoryCategories
                .FirstOrDefaultAsync(c => c.id == id && c.companyId == companyId);

            if (category == null)
            {
                return NotFound();
            }

            _db.InventoryCategories.Remove(category);
            await _db.SaveChangesAsync();

            return Ok();
        }
    }

    public class CreateCategoryRequest
    {
        public string Name { get; set; }
        public string? Color { get; set; }
    }
}

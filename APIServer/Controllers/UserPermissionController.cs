using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserPermissionController : ControllerBase
    {
        private Guid GetRequestingUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token");
            }
            return userId;
        }

        private Guid? GetCompanyId()
        {
            if (Request.Headers.TryGetValue("X-Company-Id", out var headerValue) && Guid.TryParse(headerValue, out var companyId))
            {
                return companyId;
            }
            return null;
        }

        /// <summary>
        /// Get all available permission definitions
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("available")]
        public ActionResult<AvailablePermissionsResponseVM> GetAvailablePermissions(
            [FromServices] UserPermissionHandler handler)
        {
            var result = handler.GetAvailablePermissions();
            return Ok(result);
        }

        /// <summary>
        /// Get all permissions for a specific user (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserPermissionsResponseVM>> GetUserPermissions(
            Guid userId,
            [FromServices] UserPermissionHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var result = await handler.GetUserPermissionsAsync(userId, companyId.Value);
            if (result == null)
            {
                return NotFound(new { message = "User not found in this company" });
            }

            return Ok(result);
        }

        /// <summary>
        /// Get my own permissions (any authenticated user)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("my-permissions")]
        public async Task<ActionResult<List<string>>> GetMyPermissions(
            [FromServices] UserPermissionHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var userId = GetRequestingUserId();
            var result = await handler.GetUserGrantedPermissionsAsync(userId, companyId.Value);
            return Ok(result);
        }

        /// <summary>
        /// Update a single permission for a user (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("update")]
        public async Task<ActionResult> UpdatePermission(
            [FromBody] UpdatePermissionVM request,
            [FromServices] UserPermissionHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var grantedByUserId = GetRequestingUserId();
            var success = await handler.UpdatePermissionAsync(
                request.userId, 
                companyId.Value, 
                request.permissionKey, 
                request.granted, 
                grantedByUserId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to update permission. Invalid permission key or user not found." });
            }

            return Ok(new { message = "Permission updated successfully" });
        }

        /// <summary>
        /// Update multiple permissions for a user (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("update-multiple")]
        public async Task<ActionResult> UpdateMultiplePermissions(
            [FromBody] UpdatePermissionsVM request,
            [FromServices] UserPermissionHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var grantedByUserId = GetRequestingUserId();
            var success = await handler.UpdatePermissionsAsync(
                request.userId, 
                companyId.Value, 
                request.permissions, 
                grantedByUserId);

            if (!success)
            {
                return BadRequest(new { message = "Failed to update permissions" });
            }

            return Ok(new { message = "Permissions updated successfully" });
        }

        /// <summary>
        /// Reset all permissions to role defaults (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("reset/{userId}")]
        public async Task<ActionResult> ResetPermissions(
            Guid userId,
            [FromServices] UserPermissionHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var success = await handler.ResetPermissionsAsync(userId, companyId.Value);

            if (!success)
            {
                return BadRequest(new { message = "Failed to reset permissions" });
            }

            return Ok(new { message = "Permissions reset to role defaults" });
        }
    }
}

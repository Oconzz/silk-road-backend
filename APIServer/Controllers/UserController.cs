using APIServer.AuthAttributes;
using APIServer.Services;
using AutoMapper;
using Business.Data;
using Business.Handlers;
using Business.Model;
using Business.Response;
using Business.Services;
using Business.Utilities;
using Business.ViewModel;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using NgrokExtensions;
using System.Security.Claims;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserController : ControllerBase
    {
        public UserController()
        {
        }

        // Helper to get requesting user ID from JWT token
        private Guid GetRequestingUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid or missing user ID in token");
            }
            return userId;
        }

        // SuperAdmin can see all users, Admin can see users in their companies
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPost("userList")]
        public async Task<ActionResult<PagedResponse<UserListItem>>> GetUserList([FromBody] UserListRequestVM request,
            [FromServices] DBContext _ctx,
            [FromServices] IMapper _mapper,
            [FromServices] UserHandler handler)
        {
            var requestingUserId = GetRequestingUserId();
            
            // Get requesting user's role - if Admin (not SuperAdmin), filter by their company
            var requestingUserCompanies = await _ctx.UserCompany
                .AsNoTracking()
                .Include(uc => uc.company)
                .Where(uc => uc.userId == requestingUserId && uc.company.active)
                .ToListAsync();

            var isSuperAdmin = requestingUserCompanies.Any(uc => 
                string.Equals(uc.role, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase));

            Guid? companyFilter = null;
            if (!isSuperAdmin)
            {
                // Admin - get their company ID from header or use first admin company
                if (Request.Headers.TryGetValue("X-Company-Id", out var headerValue) && Guid.TryParse(headerValue, out var companyId))
                {
                    companyFilter = companyId;
                }
                else
                {
                    var adminCompanyId = requestingUserCompanies
                        .Where(uc => string.Equals(uc.role, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
                        .Select(uc => uc.companyId)
                        .FirstOrDefault();
                    
                    companyFilter = adminCompanyId;
                }
            }

            return await handler.GetUserListAsync(request, companyFilter);
        }

        // Crew and above can update their own profile, Admin can update users in their company, SuperAdmin can update anyone
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, false })]
        [HttpPost("userUpdate")]
        public async Task<ActionResult<UsersOutVM>> UpdateUser([FromBody] UserUpdateVM request,
            [FromServices] DBContext _ctx,
            [FromServices] IMapper _mapper,
            [FromServices] UserHandler handler)
        {
            var requestingUserId = GetRequestingUserId();
            
            try
            {
                return await handler.UpdateUserAsync(request, requestingUserId);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
        }

        // Only SuperAdmin and Admin can create users
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPost("userCreate")]
        public async Task<ActionResult<UsersOutVM>> CreateUser([FromBody] CreateUserVM request,
            [FromServices] UserHandler handler)
        {
            var result = await handler.CreateUserAsync(request);
            return result;
        }

        // Assign user to company - Admin can assign to their company, SuperAdmin to any company
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPost("assignUserToCompany")]
        public async Task<ActionResult<bool>> AssignUserToCompany([FromBody] AssignUserToCompanyVM request,
            [FromServices] UserHandler handler)
        {
            var requestingUserId = GetRequestingUserId();
            
            try
            {
                var result = await handler.AssignUserToCompanyAsync(requestingUserId, request.userId, request.companyId, request.role);
                return Ok(new { success = result, message = result ? "User assigned successfully" : "Failed to assign user" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
        }

        // Remove user from company - Admin can remove from their company, SuperAdmin from any company
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, false })]
        [HttpPost("removeUserFromCompany")]
        public async Task<ActionResult<bool>> RemoveUserFromCompany([FromBody] UpdateUserCompanyRoleVM request,
            [FromServices] UserHandler handler)
        {
            var requestingUserId = GetRequestingUserId();
            
            try
            {
                var result = await handler.RemoveUserFromCompanyAsync(requestingUserId, request.userId, request.companyId);
                return Ok(new { success = result, message = result ? "User removed successfully" : "Failed to remove user" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
        }

        // Get user's own info - any authenticated user
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, false })]
        [HttpGet("me")]
        public async Task<ActionResult<UsersOutVM>> GetMyProfile(
            [FromServices] DBContext _ctx,
            [FromServices] IMapper _mapper,
            [FromServices] UserHandler handler)
        {
            var requestingUserId = GetRequestingUserId();
            var user = await handler.GetUserAsync(requestingUserId);
            
            if (user == null)
                return NotFound(new { message = "User not found" });

            return _mapper.Map<User, UsersOutVM>(user);
        }
    }
}

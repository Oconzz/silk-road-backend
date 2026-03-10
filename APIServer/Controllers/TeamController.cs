using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Handlers;
using Business.Response;
using Business.Services;
using Business.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly IConfiguration _config;

        public TeamController(IConfiguration config)
        {
            _config = config;
        }

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
        /// Get list of team members for the current company (Admin/Leader)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Leader, 0, true })]
        [HttpPost("list")]
        public async Task<ActionResult<PagedResponse<TeamMemberRM>>> GetTeamMembers(
            [FromBody] TeamMemberListRequestVM request,
            [FromServices] UserHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            // Exclude the requesting user from the list
            var requestingUserId = GetRequestingUserId();
            var result = await handler.GetTeamMembersAsync(request, companyId.Value, requestingUserId);
            return Ok(result);
        }

        /// <summary>
        /// Get a single team member's details (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("{userId}")]
        public async Task<ActionResult<TeamMemberRM>> GetTeamMember(
            Guid userId,
            [FromServices] UserHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var member = await handler.GetTeamMemberAsync(userId, companyId.Value);
            if (member == null)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(member);
        }

        /// <summary>
        /// Invite a new team member to the company (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("invite")]
        public async Task<ActionResult<InviteUserResponseVM>> InviteTeamMember(
            [FromBody] InviteUserVM request,
            [FromServices] UserHandler handler,
            [FromServices] Business.Data.DBContext ctx,
            [FromServices] Business.Services.IEmailService emailService)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var requestingUserId = GetRequestingUserId();

            // Validate request
            if (string.IsNullOrWhiteSpace(request.email))
            {
                return BadRequest(new { message = "Email is required" });
            }
            if (string.IsNullOrWhiteSpace(request.fullName))
            {
                return BadRequest(new { message = "Full name is required" });
            }

            // Validate seat limit before inviting
            var company = await ctx.Company.FindAsync(companyId.Value);
            if (company != null)
            {
                var totalSeats = company.subscriptionActive ? 1 + (company.subscriptionUserPacks * 10) : 1;
                var currentMembers = await ctx.UserCompany.CountAsync(uc => uc.companyId == companyId.Value);

                if (currentMembers >= totalSeats)
                {
                    var packsNeeded = (int)Math.Ceiling((currentMembers + 1 - totalSeats) / 10.0);
                    return BadRequest(new
                    {
                        message = $"Seat limit reached ({currentMembers}/{totalSeats}). Add {packsNeeded} more seat pack(s) to invite new members.",
                        code = "SEAT_LIMIT_EXCEEDED",
                        totalSeats,
                        usedSeats = currentMembers,
                        packsNeeded
                    });
                }
            }

            var result = await handler.InviteUserAsync(request, companyId.Value, requestingUserId);

            if (!result.success)
            {
                // If it's an existing user added, return success
                if (result.message == "Existing user added to your company")
                {
                    return Ok(new InviteUserResponseVM
                    {
                        success = true,
                        message = "Team member added successfully (existing user)",
                        userId = result.userId,
                        email = result.email,
                        fullName = request.fullName,
                        isNewUser = false
                    });
                }
                return BadRequest(new { message = result.message });
            }

            // Get requesting user's name for the invite email
            var requestingUser = await ctx.User.FindAsync(requestingUserId);
            var invitedByName = requestingUser?.fullName ?? "Your Admin";
            var companyName = result.companyName ?? "Your Company";
            
            // Send invite email using MailGun
            var emailSent = await emailService.SendTeamMemberInviteEmailAsync(
                result.email!,
                result.inviteToken!,
                request.fullName,
                companyName,
                invitedByName
            );

            // Always return credentials so admin can share manually if needed
            return Ok(new InviteUserResponseVM
            {
                success = true,
                message = emailSent 
                    ? "Team member invited successfully. Welcome email sent." 
                    : "Team member created. Please share the credentials below with the team member.",
                userId = result.userId,
                email = result.email,
                fullName = request.fullName,
                temporaryPassword = result.tempPassword,
                isNewUser = true
            });
        }

        /// <summary>
        /// Update a team member's role (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPut("role")]
        public async Task<ActionResult> UpdateTeamMemberRole(
            [FromBody] UpdateTeamMemberRoleVM request,
            [FromServices] UserHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            try
            {
                var success = await handler.UpdateTeamMemberRoleAsync(request.userId, companyId.Value, request.role);
                if (!success)
                {
                    return NotFound(new { message = "Team member not found" });
                }
                return Ok(new { message = "Role updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Remove a team member from the company (Admin only)
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpDelete("{userId}")]
        public async Task<ActionResult> RemoveTeamMember(
            Guid userId,
            [FromServices] UserHandler handler)
        {
            var companyId = GetCompanyId();
            if (!companyId.HasValue)
            {
                return BadRequest(new { message = "X-Company-Id header is required" });
            }

            var success = await handler.DeactivateTeamMemberAsync(userId, companyId.Value);
            if (!success)
            {
                return NotFound(new { message = "Team member not found" });
            }

            return Ok(new { message = "Team member removed successfully" });
        }

        /// <summary>
        /// Send invite email using OneSignal
        /// </summary>
        private async Task<bool> SendInviteEmailAsync(string name, string email, string password, string companyName)
        {
            try
            {
                var oneSignalService = new OneSignalService(_config);
                var htmlBody = OneSignalService.GenerateInviteEmailHtml(name, email, password, companyName);
                var subject = $"Welcome to {companyName} on Silk Road!";
                var preheader = $"You've been invited to join {companyName}";
                
                return await oneSignalService.SendEmailAsync(email, name, subject, htmlBody, preheader);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamController] Error sending invite email: {ex.Message}");
                return false;
            }
        }
    }
}

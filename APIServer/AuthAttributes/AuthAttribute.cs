using System.Net.Http;
using System.Security;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Business.Data;
using Business.Middleware;
using Business.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;


namespace APIServer.AuthAttributes
{
    /// <summary>
    /// Roles enum for authorization
    /// </summary>
    public enum Roles
    {
        SuperAdmin,
        Admin,
        Leader,
        Crew,
        Client
    }

    /// <summary>
    /// Custom authorization attribute for controlling access to API endpoints based on company-scoped roles.
    /// Validates that the user is assigned to the company specified in the request header or route.
    /// The role check is performed against the user's role for that specific company.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizationAttribute : Attribute, IAuthorizationFilter
    {
        public Roles role;
        public int limit;
        public bool requireCompanyContext;
        private readonly IServiceProvider _serviceProvider;

        public AuthorizationAttribute(IServiceProvider serviceProvider, Roles _role, int _limit, bool _requireCompanyContext = true)
        {
            _serviceProvider = serviceProvider;
            role = _role;
            limit = _limit;
            requireCompanyContext = _requireCompanyContext;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var principal = context.HttpContext.User;
            var requestPath = context.HttpContext.Request.Path;
            
            Console.WriteLine($"[AUTH DEBUG] Request: {requestPath}");
            Console.WriteLine($"[AUTH DEBUG] Required Role: {role}");
            Console.WriteLine($"[AUTH DEBUG] IsAuthenticated: {principal?.Identity?.IsAuthenticated}");
            
            if (principal?.Identity?.IsAuthenticated != true)
            {
                Console.WriteLine($"[AUTH DEBUG] REJECTED: Not authenticated");
                context.Result = new JsonResult(new { Message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            var userIdClaim = principal.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
            Console.WriteLine($"[AUTH DEBUG] UserIdClaim: {userIdClaim}");
            
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                Console.WriteLine($"[AUTH DEBUG] REJECTED: Invalid user ID claim");
                context.Result = new JsonResult(new { Message = "Unauthorized" }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }

            // Extract companyId from header or route
            Guid? companyId = null;
            
            // First try to get from custom header
            if (context.HttpContext.Request.Headers.TryGetValue("X-Company-Id", out var headerValue))
            {
                if (Guid.TryParse(headerValue, out var parsedCompanyId))
                {
                    companyId = parsedCompanyId;
                }
            }
            
            // If not in header, try route value
            if (!companyId.HasValue && TryGetCompanyIdFromRoute(context.HttpContext, out var routeCompanyId))
            {
                companyId = routeCompanyId;
            }

            // Check if company context is required
            if (requireCompanyContext && !companyId.HasValue)
            {
                context.Result = new JsonResult(new { Message = "X-Company-Id header is required for this endpoint" }) 
                    { StatusCode = StatusCodes.Status400BadRequest };
                return;
            }

            // For routes that require company context, validate the assignment
            if (companyId.HasValue)
            {
                Console.WriteLine($"[AUTH DEBUG] CompanyId from header: {companyId.Value}");
                
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DBContext>();

                // Check if user is assigned to this company and get their role
                var userCompany = db.UserCompany
                    .AsNoTracking()
                    .Include(uc => uc.company)
                    .FirstOrDefault(uc => uc.userId == userId && uc.companyId == companyId.Value);

                if (userCompany == null)
                {
                    Console.WriteLine($"[AUTH DEBUG] REJECTED: UserCompany not found for userId={userId}, companyId={companyId.Value}");
                    context.Result = new JsonResult(new { Message = "User is not assigned to this company" }) 
                        { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
                
                if (!userCompany.company.active)
                {
                    Console.WriteLine($"[AUTH DEBUG] REJECTED: Company is not active");
                    context.Result = new JsonResult(new { Message = "Company is not active" }) 
                        { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }

                // Check if user's role for this company meets the required role
                var userRole = userCompany.role;
                Console.WriteLine($"[AUTH DEBUG] User Role: '{userRole}', Required Role: {role}");
                
                var isAllowed = ValidateRole(userRole, role);
                Console.WriteLine($"[AUTH DEBUG] IsAllowed: {isAllowed}");

                if (!isAllowed)
                {
                    Console.WriteLine($"[AUTH DEBUG] REJECTED: Insufficient permissions. User role '{userRole}' < required '{role}'");
                    context.Result = new JsonResult(new { Message = $"Insufficient permissions. Your role is '{userRole}', required: '{role}'" }) 
                        { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
                
                Console.WriteLine($"[AUTH DEBUG] AUTHORIZED: User {userId} with role '{userRole}' accessing {requestPath}");
            }
            else if (!requireCompanyContext)
            {
                // No company context but it's optional - validate user has ANY company with sufficient role
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DBContext>();

                var hasAnyCompanyWithRole = db.UserCompany
                    .AsNoTracking()
                    .Include(uc => uc.company)
                    .Any(uc => uc.userId == userId && uc.company.active && ValidateRole(uc.role, role));

                if (!hasAnyCompanyWithRole)
                {
                    context.Result = new JsonResult(new { Message = "User does not have sufficient permissions in any company" }) 
                        { StatusCode = StatusCodes.Status403Forbidden };
                    return;
                }
            }

            // Store userId (and companyId if available) in HttpContext.Items for controllers
            context.HttpContext.Items["UserId"] = userId;
            if (companyId.HasValue)
            {
                context.HttpContext.Items["CompanyId"] = companyId.Value;
            }

            // Optional: handle trial limits
            if (limit > 0)
            {
                var trialVal = principal.Claims.FirstOrDefault(x => x.Type == "trial")?.Value ?? string.Empty;
                var trial = trialVal.Equals("true", StringComparison.InvariantCultureIgnoreCase);
                if (trial)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var _ctx = scope.ServiceProvider.GetRequiredService<DBContext>();
                    // Currently no specific per-action limit enforcement active
                }
            }
        }

        private static bool ValidateRole(string userRole, Roles requiredRole)
        {
            // Role hierarchy: SuperAdmin > Admin > Leader > Crew > Client
            var roleHierarchy = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "SuperAdmin", 5 },
                { "Admin", 4 },
                { "Leader", 3 },
                { "Crew", 2 },
                { "Client", 1 }
            };

            var requiredRoleHierarchy = new Dictionary<Roles, int>
            {
                { Roles.SuperAdmin, 5 },
                { Roles.Admin, 4 },
                { Roles.Leader, 3 },
                { Roles.Crew, 2 },
                { Roles.Client, 1 }
            };

            if (!roleHierarchy.TryGetValue(userRole, out var userLevel))
                return false;

            if (!requiredRoleHierarchy.TryGetValue(requiredRole, out var requiredLevel))
                return false;

            return userLevel >= requiredLevel;
        }

        // Try to get company id from route value named "companyId"
        private static bool TryGetCompanyIdFromRoute(HttpContext http, out Guid companyId)
        {
            companyId = Guid.Empty;
            if (http.Request.RouteValues.TryGetValue("companyId", out var routeVal) && routeVal != null && Guid.TryParse(routeVal.ToString(), out companyId))
                return true;
            return false;
        }
    }
}

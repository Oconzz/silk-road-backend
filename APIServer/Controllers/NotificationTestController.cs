using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Controllers
{
    /// <summary>
    /// DEBUG-only controller for manually triggering notifications.
    /// Remove or disable in production.
    /// </summary>
    [ApiController]
    [Route("api/notifications/test")]
    public class NotificationTestController : ControllerBase
    {
        private readonly DBContext _ctx;
        private readonly NotificationService _notificationService;

        public NotificationTestController(DBContext ctx, NotificationService notificationService)
        {
            _ctx = ctx;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Fire all notification types for a given user (for testing).
        /// Requires the user's email and company context.
        /// </summary>
        [HttpPost("fire-all")]
        public async Task<ActionResult> FireAllNotifications([FromBody] FireAllVM request)
        {
            // Find user by email
            var user = await _ctx.User.FirstOrDefaultAsync(u => u.email == request.Email);
            if (user == null)
                return NotFound(new { message = $"User with email '{request.Email}' not found" });

            // Find user's company
            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.company)
                .FirstOrDefaultAsync(uc => uc.userId == user.id);

            if (userCompany?.company == null)
                return NotFound(new { message = "User has no company" });

            var userId = user.id;
            var companyId = userCompany.companyId;
            var results = new List<string>();

            // 1. Job Assignment
            try
            {
                await _notificationService.TriggerJobAssignment(
                    userId, companyId, Guid.Empty, "Test Job - Kitchen Remodel");
                results.Add("✅ job_assignment: sent");
            }
            catch (Exception ex) { results.Add($"❌ job_assignment: {ex.Message}"); }

            // 2. Material Return Warning
            try
            {
                await _notificationService.SendToUser(
                    userId, "material_return",
                    "Material Return Reminder",
                    "Materials for \"Test Job\" are due for return in 3 days.");
                results.Add("✅ material_return: sent");
            }
            catch (Exception ex) { results.Add($"❌ material_return: {ex.Message}"); }

            // 3. Timecard Reminder
            try
            {
                await _notificationService.SendToUser(
                    userId, "timecard_reminder",
                    "Clock In Reminder",
                    "Don't forget to clock in for today's work.");
                results.Add("✅ timecard_reminder: sent");
            }
            catch (Exception ex) { results.Add($"❌ timecard_reminder: {ex.Message}"); }

            // 4. Goal/Reward Reminder
            try
            {
                await _notificationService.SendToUser(
                    userId, "reward_goal",
                    "Goal Deadline Approaching",
                    "Goal \"Install countertops\" for \"Test Job\" is due soon!");
                results.Add("✅ reward_goal: sent");
            }
            catch (Exception ex) { results.Add($"❌ reward_goal: {ex.Message}"); }

            // 5. Review Prompt
            try
            {
                await _notificationService.SendToUser(
                    userId, "review_prompt",
                    "Enjoying SilkRoad?",
                    "You've completed multiple jobs! Take a moment to rate your experience.");
                results.Add("✅ review_prompt: sent");
            }
            catch (Exception ex) { results.Add($"❌ review_prompt: {ex.Message}"); }

            // 6. Low Stock Alert
            try
            {
                await _notificationService.SendToUser(
                    userId, "low_stock",
                    "Low Stock Alert",
                    "3 item(s) running low: Drywall Sheets, Paint Primer, Caulk");
                results.Add("✅ low_stock: sent");
            }
            catch (Exception ex) { results.Add($"❌ low_stock: {ex.Message}"); }

            // 7. Estimate Viewed
            try
            {
                await _notificationService.TriggerEstimateViewed(
                    companyId, Guid.Empty, "EST-TEST-001", "John Smith");
                results.Add("✅ estimate_viewed: sent");
            }
            catch (Exception ex) { results.Add($"❌ estimate_viewed: {ex.Message}"); }

            // 8. Invoice Paid
            try
            {
                await _notificationService.TriggerInvoicePaid(
                    companyId, Guid.Empty, "INV-TEST-001", 2499.99m, "Jane Doe");
                results.Add("✅ invoice_paid: sent");
            }
            catch (Exception ex) { results.Add($"❌ invoice_paid: {ex.Message}"); }

            return Ok(new
            {
                message = $"Fired 8 notification types for {request.Email}",
                userId = userId.ToString(),
                companyId = companyId.ToString(),
                results
            });
        }

        /// <summary>
        /// Fire a single notification type for testing.
        /// </summary>
        [HttpPost("fire")]
        public async Task<ActionResult> FireSingleNotification([FromBody] FireSingleVM request)
        {
            var user = await _ctx.User.FirstOrDefaultAsync(u => u.email == request.Email);
            if (user == null)
                return NotFound(new { message = $"User with email '{request.Email}' not found" });

            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.company)
                .FirstOrDefaultAsync(uc => uc.userId == user.id);

            if (userCompany?.company == null)
                return NotFound(new { message = "User has no company" });

            await _notificationService.SendToUser(
                user.id,
                request.Topic ?? "general",
                request.Title ?? "SilkRoad Test",
                request.Body ?? "This is a test notification."
            );

            return Ok(new
            {
                message = "Notification sent",
                userId = user.id.ToString(),
                topic = request.Topic,
                title = request.Title
            });
        }
    }

    public class FireAllVM
    {
        public string Email { get; set; } = string.Empty;
    }

    public class FireSingleVM
    {
        public string Email { get; set; } = string.Empty;
        public string? Topic { get; set; }
        public string? Title { get; set; }
        public string? Body { get; set; }
    }
}

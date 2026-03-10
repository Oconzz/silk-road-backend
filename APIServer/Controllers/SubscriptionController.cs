using APIServer.AuthAttributes;
using APIServer.Services;
using Business.Data;
using Business.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace APIServer.Controllers
{
    /// <summary>
    /// Controller for subscription management via RevenueCat
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly DBContext _ctx;
        private readonly IConfiguration _config;

        public SubscriptionController(DBContext ctx, IConfiguration config)
        {
            _ctx = ctx;
            _config = config;
        }

        /// <summary>
        /// Get subscription status for the current company
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Crew, 0, true })]
        [HttpGet("status")]
        public async Task<ActionResult<SubscriptionStatusRM>> GetStatus()
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var company = await _ctx.Company.FindAsync(companyId);
            if (company == null)
            {
                return NotFound(new { Message = "Company not found" });
            }

            return Ok(new SubscriptionStatusRM
            {
                IsActive = company.subscriptionActive,
                Plan = company.subscriptionPlan,
                UserPacks = company.subscriptionUserPacks,
                ExpirationDate = company.subscriptionExpirationDate,
                LastVerified = company.subscriptionLastVerified
            });
        }

        /// <summary>
        /// Sync subscription status from the mobile app (after RevenueCat purchase)
        /// Called by the mobile app to update the backend with the current subscription state
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpPost("sync")]
        public async Task<ActionResult<SubscriptionStatusRM>> SyncSubscription(
            [FromBody] SubscriptionSyncVM request)
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var company = await _ctx.Company.FindAsync(companyId);
            if (company == null)
            {
                return NotFound(new { Message = "Company not found" });
            }

            company.subscriptionActive = request.IsActive;
            company.subscriptionPlan = request.Plan;
            company.subscriptionUserPacks = request.UserPacks;
            company.subscriptionExpirationDate = request.ExpirationDate;
            company.subscriptionLastVerified = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(request.RevenueCatCustomerId))
            {
                company.revenueCatCustomerId = request.RevenueCatCustomerId;
            }

            company.updateDatetime = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            Console.WriteLine($"[Subscription] Synced for company {companyId}: active={request.IsActive}, plan={request.Plan}");

            return Ok(new SubscriptionStatusRM
            {
                IsActive = company.subscriptionActive,
                Plan = company.subscriptionPlan,
                UserPacks = company.subscriptionUserPacks,
                ExpirationDate = company.subscriptionExpirationDate,
                LastVerified = company.subscriptionLastVerified
            });
        }

        /// <summary>
        /// RevenueCat Webhook endpoint - receives subscription events.
        /// Configured in RevenueCat dashboard → Webhooks.
        /// Authorization: Bearer {WebhookAuthSecret} from appsettings.
        /// </summary>
        [HttpPost("webhook")]
        public async Task<ActionResult> RevenueCatWebhook([FromBody] RevenueCatWebhookVM payload)
        {
            // ── Auth check ──────────────────────────────────────────
            var expectedSecret = _config["RevenueCat:WebhookAuthSecret"];
            if (!string.IsNullOrEmpty(expectedSecret))
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrEmpty(authHeader) || authHeader != $"Bearer {expectedSecret}")
                {
                    Console.WriteLine("[Webhook] Unauthorized request rejected");
                    return Unauthorized();
                }
            }

            var evt = payload.Event;
            Console.WriteLine($"[Webhook] Event: {evt?.Type}, AppUserId: {evt?.AppUserId}, Product: {evt?.ProductId}, Env: {evt?.Environment}");

            // ── TEST event — just acknowledge ───────────────────────
            if (evt?.Type?.Equals("TEST", StringComparison.OrdinalIgnoreCase) == true)
            {
                Console.WriteLine("[Webhook] TEST event received — OK");
                return Ok();
            }

            if (evt == null || string.IsNullOrEmpty(evt.AppUserId))
            {
                return Ok(); // Acknowledge but ignore invalid payloads
            }

            // The AppUserId from RevenueCat is the userId we set during Purchases.logIn()
            if (!Guid.TryParse(evt.AppUserId, out var userId))
            {
                Console.WriteLine($"[Webhook] Invalid AppUserId (not a GUID): {evt.AppUserId}");
                return Ok();
            }

            var userCompany = await _ctx.UserCompany
                .Include(uc => uc.company)
                .Where(uc => uc.userId == userId && uc.role == "Admin")
                .FirstOrDefaultAsync();

            if (userCompany?.company == null)
            {
                Console.WriteLine($"[Webhook] No admin company found for user: {userId}");
                return Ok();
            }

            var company = userCompany.company;
            var eventType = (evt.Type ?? "").ToUpperInvariant();

            switch (eventType)
            {
                case "INITIAL_PURCHASE":
                case "RENEWAL":
                case "PRODUCT_CHANGE":
                case "UNCANCELLATION":
                    company.subscriptionActive = true;
                    company.subscriptionPlan = evt.ProductId;
                    company.subscriptionUserPacks = ParsePacksFromProductId(evt.ProductId);
                    company.subscriptionExpirationDate = evt.ExpirationAtMs.HasValue
                        ? DateTimeOffset.FromUnixTimeMilliseconds(evt.ExpirationAtMs.Value).UtcDateTime
                        : null;
                    company.revenueCatCustomerId = evt.AppUserId;
                    break;

                case "CANCELLATION":
                    // Cancellation means auto-renew is off; user still has access until expiration
                    Console.WriteLine($"[Webhook] Cancellation for company {company.id} — access until {company.subscriptionExpirationDate}");
                    break;

                case "EXPIRATION":
                    company.subscriptionActive = false;
                    break;

                case "BILLING_ISSUE":
                    Console.WriteLine($"[Webhook] Billing issue for company {company.id} — may lose access soon");
                    break;

                case "SUBSCRIBER_ALIAS":
                case "TRANSFER":
                    Console.WriteLine($"[Webhook] {eventType} for user {userId} — no action needed");
                    break;

                default:
                    Console.WriteLine($"[Webhook] Unhandled event type: {eventType}");
                    break;
            }

            company.subscriptionLastVerified = DateTime.UtcNow;
            company.updateDatetime = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            Console.WriteLine($"[Webhook] Updated company {company.id}: event={eventType}, active={company.subscriptionActive}, plan={company.subscriptionPlan}, packs={company.subscriptionUserPacks}");
            return Ok();
        }

        /// <summary>
        /// Get the seat limit for a company (used by TeamController for validation)
        /// Base subscription includes 1 seat. Each pack adds 10 seats.
        /// </summary>
        [TypeFilter(typeof(ServiceProviderFilterFactory), Arguments = new object[] { Roles.Admin, 0, true })]
        [HttpGet("seat-limit")]
        public async Task<ActionResult> GetSeatLimit()
        {
            if (!Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader) ||
                !Guid.TryParse(companyIdHeader, out var companyId))
            {
                return BadRequest(new { Message = "X-Company-Id header is required" });
            }

            var company = await _ctx.Company.FindAsync(companyId);
            if (company == null)
            {
                return NotFound(new { Message = "Company not found" });
            }

            var totalSeats = company.subscriptionActive ? 1 + (company.subscriptionUserPacks * 10) : 1;
            var currentMembers = await _ctx.UserCompany.CountAsync(uc => uc.companyId == companyId);

            return Ok(new
            {
                TotalSeats = totalSeats,
                UsedSeats = currentMembers,
                AvailableSeats = Math.Max(0, totalSeats - currentMembers),
                UserPacks = company.subscriptionUserPacks,
                IsSubscribed = company.subscriptionActive
            });
        }

        /// <summary>
        /// Parse the number of seat packs from a RevenueCat product identifier.
        /// Matches pattern like "1pack", "2packs", "3packs" etc.
        /// </summary>
        private static int ParsePacksFromProductId(string? productId)
        {
            if (string.IsNullOrEmpty(productId)) return 0;
            var match = System.Text.RegularExpressions.Regex.Match(productId, @"(\d+)pack");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }
    }

    #region ViewModels

    public class SubscriptionStatusRM
    {
        public bool IsActive { get; set; }
        public string? Plan { get; set; }
        public int UserPacks { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? LastVerified { get; set; }
    }

    public class SubscriptionSyncVM
    {
        public bool IsActive { get; set; }
        public string? Plan { get; set; }
        public int UserPacks { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? RevenueCatCustomerId { get; set; }
    }

    public class RevenueCatWebhookVM
    {
        [JsonPropertyName("api_version")]
        public string? ApiVersion { get; set; }

        [JsonPropertyName("event")]
        public RevenueCatEvent? Event { get; set; }
    }

    public class RevenueCatEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("app_user_id")]
        public string? AppUserId { get; set; }

        [JsonPropertyName("original_app_user_id")]
        public string? OriginalAppUserId { get; set; }

        [JsonPropertyName("product_id")]
        public string? ProductId { get; set; }

        [JsonPropertyName("expiration_at_ms")]
        public long? ExpirationAtMs { get; set; }

        [JsonPropertyName("purchased_at_ms")]
        public long? PurchasedAtMs { get; set; }

        [JsonPropertyName("event_timestamp_ms")]
        public long? EventTimestampMs { get; set; }

        [JsonPropertyName("environment")]
        public string? Environment { get; set; }

        [JsonPropertyName("store")]
        public string? Store { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("entitlement_ids")]
        public List<string>? EntitlementIds { get; set; }

        [JsonPropertyName("period_type")]
        public string? PeriodType { get; set; }

        [JsonPropertyName("presented_offering_id")]
        public string? PresentedOfferingId { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    #endregion
}

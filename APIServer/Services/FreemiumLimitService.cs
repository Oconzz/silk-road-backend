using Business.Data;
using Microsoft.EntityFrameworkCore;

namespace APIServer.Services
{
    /// <summary>
    /// Server-side enforcement of freemium limits.
    /// Mirrors the mobile useFreemiumLimits hook to prevent API-level bypass.
    /// </summary>
    public class FreemiumLimitService
    {
        private readonly DBContext _ctx;

        // Freemium limits — must match mobile FREEMIUM_LIMITS in types/subscription.ts
        public const int MaxJobs = 3;
        public const int MaxEstimates = 3;
        public const int MaxClients = 3;
        public const int MaxInventoryItems = 10;
        public const int MaxTeamMembers = 0; // enforced separately via seat logic

        public FreemiumLimitService(DBContext ctx)
        {
            _ctx = ctx;
        }

        /// <summary>
        /// Check if a company can create a resource of the given type.
        /// Returns (allowed, currentCount, limit).
        /// Subscribed companies have no limits.
        /// </summary>
        public async Task<FreemiumCheckResult> CheckLimit(Guid companyId, FreemiumResource resource)
        {
            var company = await _ctx.Company.AsNoTracking().FirstOrDefaultAsync(c => c.id == companyId);
            if (company == null)
                return new FreemiumCheckResult(false, 0, 0, "Company not found");

            // Subscribed users have unlimited access
            if (company.subscriptionActive)
                return new FreemiumCheckResult(true, 0, int.MaxValue, null);

            var (count, limit) = await GetCountAndLimit(companyId, resource);

            if (count >= limit)
            {
                var label = GetResourceLabel(resource);
                return new FreemiumCheckResult(
                    false, count, limit,
                    $"Free plan limit reached: {count}/{limit} {label}. Subscribe to unlock unlimited access."
                );
            }

            return new FreemiumCheckResult(true, count, limit, null);
        }

        private async Task<(int count, int limit)> GetCountAndLimit(Guid companyId, FreemiumResource resource)
        {
            return resource switch
            {
                FreemiumResource.Jobs => (
                    await _ctx.Job.CountAsync(j => j.companyId == companyId && j.status != "cancelled"),
                    MaxJobs
                ),
                FreemiumResource.Estimates => (
                    await _ctx.Estimates.CountAsync(e => e.companyId == companyId),
                    MaxEstimates
                ),
                FreemiumResource.Clients => (
                    await _ctx.Client.CountAsync(c => c.companyId == companyId),
                    MaxClients
                ),
                FreemiumResource.Inventory => (
                    await _ctx.Inventory.CountAsync(i => i.companyId == companyId),
                    MaxInventoryItems
                ),
                _ => (0, 0)
            };
        }

        private static string GetResourceLabel(FreemiumResource resource)
        {
            return resource switch
            {
                FreemiumResource.Jobs => "jobs",
                FreemiumResource.Estimates => "estimates",
                FreemiumResource.Clients => "clients",
                FreemiumResource.Inventory => "inventory items",
                _ => "resources"
            };
        }
    }

    public enum FreemiumResource
    {
        Jobs,
        Estimates,
        Clients,
        Inventory
    }

    public record FreemiumCheckResult(
        bool Allowed,
        int CurrentCount,
        int Limit,
        string? Message
    );
}

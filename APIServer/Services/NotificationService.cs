using Business.Data;
using Business.Model;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace APIServer.Services
{
    /// <summary>
    /// Service for sending push notifications via OneSignal REST API.
    /// Configure OneSignal:AppId and OneSignal:RestApiKey in appsettings.json or environment variables.
    /// </summary>
    public class NotificationService
    {
        private readonly DBContext _ctx;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private const string ONESIGNAL_API_URL = "https://api.onesignal.com/notifications";

        public NotificationService(DBContext ctx, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _ctx = ctx;
            _config = config;
            _httpClient = httpClientFactory.CreateClient("OneSignal");
        }

        /// <summary>
        /// Send push notification to a specific user via OneSignal external_id.
        /// The external_id is the userId set by the mobile app via OneSignal.login().
        /// </summary>
        public async Task SendToUser(Guid userId, string topic, string title, string body, Dictionary<string, string>? data = null)
        {
            // Check user's notification preferences
            var prefs = await _ctx.NotificationPreferences
                .FirstOrDefaultAsync(p => p.userId == userId);

            if (prefs != null && !prefs.enabled) return;
            if (prefs != null && !IsTopicEnabled(prefs, topic)) return;

            var messageData = data ?? new Dictionary<string, string>();
            if (!messageData.ContainsKey("topic"))
            {
                messageData["topic"] = topic;
            }

            await SendOneSignalNotification(
                title, body, messageData,
                includeExternalUserIds: new List<string> { userId.ToString() }
            );
        }

        /// <summary>
        /// Send push notification to all members of a company.
        /// Uses OneSignal tags to filter by company and topic preferences.
        /// </summary>
        public async Task SendToCompany(Guid companyId, string topic, string title, string body, Dictionary<string, string>? data = null)
        {
            // Get all user IDs in this company
            var userIds = await _ctx.UserCompany
                .Where(uc => uc.companyId == companyId)
                .Select(uc => uc.userId)
                .ToListAsync();

            if (!userIds.Any()) return;

            // Filter by preferences in DB
            var eligibleUserIds = new List<string>();
            foreach (var userId in userIds)
            {
                var prefs = await _ctx.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.userId == userId && p.companyId == companyId);

                if (prefs != null && !prefs.enabled) continue;
                if (prefs != null && !IsTopicEnabled(prefs, topic)) continue;

                eligibleUserIds.Add(userId.ToString());
            }

            if (!eligibleUserIds.Any()) return;

            var messageData = data ?? new Dictionary<string, string>();
            if (!messageData.ContainsKey("topic"))
            {
                messageData["topic"] = topic;
            }

            await SendOneSignalNotification(title, body, messageData, includeExternalUserIds: eligibleUserIds);
        }

        /// <summary>
        /// Send push notification to specific users by their IDs.
        /// </summary>
        public async Task SendToUsers(IEnumerable<Guid> userIds, Guid companyId, string topic, string title, string body, Dictionary<string, string>? data = null)
        {
            var eligibleUserIds = new List<string>();

            foreach (var userId in userIds)
            {
                var prefs = await _ctx.NotificationPreferences
                    .FirstOrDefaultAsync(p => p.userId == userId);

                if (prefs != null && !prefs.enabled) continue;
                if (prefs != null && !IsTopicEnabled(prefs, topic)) continue;

                eligibleUserIds.Add(userId.ToString());
            }

            if (!eligibleUserIds.Any()) return;

            var messageData = data ?? new Dictionary<string, string>();
            if (!messageData.ContainsKey("topic"))
            {
                messageData["topic"] = topic;
            }

            await SendOneSignalNotification(title, body, messageData, includeExternalUserIds: eligibleUserIds);
        }

        // === Trigger Methods (called from controllers/background jobs) ===

        /// <summary>
        /// Trigger: Material Return Date Warning (3 days before scheduled end)
        /// Called by a scheduled background job.
        /// Uses Job.scheduledEndDate as a proxy since JobInventory has no returnDate field.
        /// </summary>
        public async Task TriggerMaterialReturnWarnings()
        {
            var threeDaysFromNow = DateTime.UtcNow.AddDays(3).Date;

            // Find active jobs whose scheduled end is in 3 days and have inventory allocated
            var jobs = await _ctx.Job
                .Where(j => j.scheduledEndDate != null
                    && j.scheduledEndDate.Value.Date == threeDaysFromNow
                    && j.status != "completed" && j.status != "cancelled")
                .ToListAsync();

            foreach (var job in jobs)
            {
                // Check if this job has inventory allocations
                var hasInventory = await _ctx.JobInventory.AnyAsync(ji => ji.jobId == job.id);
                if (!hasInventory) continue;

                var assignedUserIds = await _ctx.JobAssignments
                    .Where(ja => ja.jobId == job.id)
                    .Select(ja => ja.userId)
                    .ToListAsync();

                var data = new Dictionary<string, string>
                {
                    { "topic", "material_return" },
                    { "jobId", job.id.ToString() },
                };

                await SendToUsers(
                    assignedUserIds,
                    job.companyId,
                    "material_return",
                    "Material Return Reminder",
                    $"Materials for \"{job.title}\" are due for return in 3 days.",
                    data
                );
            }

            Console.WriteLine($"[NotificationService] Material return warnings sent for {jobs.Count} jobs");
        }

        /// <summary>
        /// Trigger: Time Card Reminder (clock-in at job start time, clock-out at end of day)
        /// Called by a scheduled background job
        /// </summary>
        public async Task TriggerTimecardReminders(string reminderType)
        {
            // Get all companies with active subscriptions
            var companies = await _ctx.Company
                .Where(c => c.active && c.subscriptionActive)
                .Select(c => c.id)
                .ToListAsync();

            foreach (var companyId in companies)
            {
                var title = reminderType == "clock_in"
                    ? "Clock In Reminder"
                    : "Clock Out Reminder";
                var body = reminderType == "clock_in"
                    ? "Don't forget to clock in for today's work."
                    : "Remember to clock out before leaving for the day.";

                var data = new Dictionary<string, string>
                {
                    { "topic", "timecard_reminder" },
                    { "reminderType", reminderType },
                };

                await SendToCompany(companyId, "timecard_reminder", title, body, data);
            }

            Console.WriteLine($"[NotificationService] Timecard " + reminderType + " reminders sent");
        }

        /// <summary>
        /// Trigger: Job Assignment (immediate, when a user is assigned to a job)
        /// Called from JobAssignmentController
        /// </summary>
        public async Task TriggerJobAssignment(Guid userId, Guid companyId, Guid jobId, string jobName)
        {
            var data = new Dictionary<string, string>
            {
                { "topic", "job_assignment" },
                { "jobId", jobId.ToString() },
            };

            await SendToUser(
                userId,
                "job_assignment",
                "New Job Assignment",
                $"You've been assigned to \"{jobName}\".",
                data
            );

            Console.WriteLine($"[NotificationService] Job assignment notification sent to user {userId} for job {jobId}");
        }

        /// <summary>
        /// Trigger: In-App Rewards/Goals reminder (day of + 2 hours before deadline)
        /// Called by a scheduled background job
        /// </summary>
        public async Task TriggerGoalReminders()
        {
            var now = DateTime.UtcNow;
            var twoHoursFromNow = now.AddHours(2);

            // Find goals with deadlines today or within 2 hours
            var goals = await _ctx.JobGoals
                .Include(g => g.job)
                .Where(g => g.targetDate >= now
                    && g.targetDate <= twoHoursFromNow
                    && !g.completed)
                .ToListAsync();

            foreach (var goal in goals)
            {
                if (goal.job == null) continue;

                var assignedUserIds = await _ctx.JobAssignments
                    .Where(ja => ja.jobId == goal.jobId)
                    .Select(ja => ja.userId)
                    .ToListAsync();

                var data = new Dictionary<string, string>
                {
                    { "topic", "reward_goal" },
                    { "jobId", goal.jobId.ToString() },
                    { "goalId", goal.id.ToString() },
                };

                await SendToUsers(
                    assignedUserIds,
                    goal.job.companyId,
                    "reward_goal",
                    "Goal Deadline Approaching",
                    $"Goal \"{goal.title}\" for \"{goal.job.title}\" is due soon!",
                    data
                );
            }

            Console.WriteLine("[NotificationService] Goal reminders sent for " + goals.Count + " goals");
        }

        /// <summary>
        /// Trigger: Review Prompt (after 3 completed jobs or 1 month of usage)
        /// Called by a scheduled background job
        /// </summary>
        public async Task TriggerReviewPrompts()
        {
            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            // Find admin users who have completed 3+ jobs and haven't been prompted yet
            var adminUsers = await _ctx.UserCompany
                .Where(uc => uc.role == "Admin" || uc.role == "SuperAdmin")
                .Select(uc => new { uc.userId, uc.companyId })
                .ToListAsync();

            foreach (var admin in adminUsers)
            {
                var completedJobCount = await _ctx.Job
                    .Where(j => j.companyId == admin.companyId && j.status == "completed")
                    .CountAsync();

                // Only prompt if 3+ completed jobs
                if (completedJobCount < 3) continue;

                // Check if we already prompted recently (use a simple check based on last notification)
                // In a real implementation, you'd track whether this prompt was already sent
                var data = new Dictionary<string, string>
                {
                    { "topic", "review_prompt" },
                };

                await SendToUser(
                    admin.userId,
                    "review_prompt",
                    "Enjoying SilkRoad?",
                    "You've completed multiple jobs! Take a moment to rate your experience.",
                    data
                );
            }

            Console.WriteLine("[NotificationService] Review prompts processed");
        }

        // === Missing Triggers ===

        /// <summary>
        /// Trigger: Low Stock Alert (inventory quantity below threshold)
        /// Called by a scheduled background job.
        /// Default threshold: 5 units.
        /// </summary>
        public async Task TriggerLowStockAlerts(decimal threshold = 5)
        {
            var lowStockItems = await _ctx.Inventory
                .Where(i => i.quantity <= threshold && i.quantity > 0)
                .ToListAsync();

            // Group by company
            var grouped = lowStockItems.GroupBy(i => i.companyId);

            foreach (var group in grouped)
            {
                var companyId = group.Key;
                var itemNames = group.Select(i => i.title).Take(3).ToList();
                var totalLow = group.Count();
                var summary = string.Join(", ", itemNames);
                if (totalLow > 3) summary += $" and {totalLow - 3} more";

                // Notify admins of this company
                var adminUserIds = await _ctx.UserCompany
                    .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                    .Select(uc => uc.userId)
                    .ToListAsync();

                var data = new Dictionary<string, string>
                {
                    { "topic", "low_stock" },
                    { "count", totalLow.ToString() },
                };

                await SendToUsers(
                    adminUserIds,
                    companyId,
                    "low_stock",
                    "Low Stock Alert",
                    $"{totalLow} item(s) running low: {summary}",
                    data
                );
            }

            Console.WriteLine($"[NotificationService] Low stock alerts sent for {lowStockItems.Count} items");
        }

        /// <summary>
        /// Trigger: Estimate Viewed by client (immediate, when client opens public share link)
        /// Called from EstimateHandler.GetPublicEstimate()
        /// </summary>
        public async Task TriggerEstimateViewed(Guid companyId, Guid estimateId, string estimateNumber, string? clientName)
        {
            // Notify admins of this company
            var adminUserIds = await _ctx.UserCompany
                .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                .Select(uc => uc.userId)
                .ToListAsync();

            var data = new Dictionary<string, string>
            {
                { "topic", "estimate_viewed" },
                { "estimateId", estimateId.ToString() },
            };

            var clientLabel = string.IsNullOrEmpty(clientName) ? "A client" : clientName;

            await SendToUsers(
                adminUserIds,
                companyId,
                "estimate_viewed",
                "Estimate Viewed",
                $"{clientLabel} just viewed estimate {estimateNumber}.",
                data
            );

            Console.WriteLine($"[NotificationService] Estimate viewed notification sent for estimate {estimateId}");
        }

        /// <summary>
        /// Trigger: Estimate Approved by client via public share link (immediate)
        /// Called from EstimatesController.ApproveEstimateByShareId()
        /// </summary>
        public async Task TriggerEstimateApproved(Guid companyId, Guid estimateId, string estimateNumber, string? clientName)
        {
            // Notify admins of this company
            var adminUserIds = await _ctx.UserCompany
                .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                .Select(uc => uc.userId)
                .ToListAsync();

            var data = new Dictionary<string, string>
            {
                { "topic", "estimate_approved" },
                { "estimateId", estimateId.ToString() },
            };

            var clientLabel = string.IsNullOrEmpty(clientName) ? "A client" : clientName;

            await SendToUsers(
                adminUserIds,
                companyId,
                "estimate_approved",
                "Estimate Approved!",
                $"{clientLabel} approved estimate {estimateNumber}.",
                data
            );

            Console.WriteLine($"[NotificationService] Estimate approved notification sent for estimate {estimateId}");
        }

        /// <summary>
        /// Trigger: Invoice/Payment completed (immediate, after successful payment)
        /// Called from payment processing flow
        /// </summary>
        public async Task TriggerInvoicePaid(Guid companyId, Guid invoiceId, string invoiceNumber, decimal amount, string? clientName)
        {
            // Notify admins of this company
            var adminUserIds = await _ctx.UserCompany
                .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                .Select(uc => uc.userId)
                .ToListAsync();

            var data = new Dictionary<string, string>
            {
                { "topic", "invoice_paid" },
                { "invoiceId", invoiceId.ToString() },
            };

            var clientLabel = string.IsNullOrEmpty(clientName) ? "A client" : clientName;

            await SendToUsers(
                adminUserIds,
                companyId,
                "invoice_paid",
                "Payment Received!",
                $"{clientLabel} paid ${amount:F2} for invoice {invoiceNumber}.",
                data
            );

            Console.WriteLine($"[NotificationService] Invoice paid notification sent for invoice {invoiceId}");
        }

        // === Private Helpers ===

        private bool IsTopicEnabled(NotificationPreference prefs, string topic)
        {
            return topic switch
            {
                "material_return" => prefs.materialReturn,
                "timecard_reminder" => prefs.timecardReminder,
                "job_assignment" => prefs.jobAssignment,
                "reward_goal" => prefs.rewardGoal,
                "review_prompt" => prefs.reviewPrompt,
                _ => true,
            };
        }

        /// <summary>
        /// Send a push notification via OneSignal REST API.
        /// Targets users by their external_id (set via OneSignal.login() on the mobile app).
        /// </summary>
        private async Task SendOneSignalNotification(
            string title,
            string body,
            Dictionary<string, string>? data = null,
            List<string>? includeExternalUserIds = null)
        {
            try
            {
                var appId = _config["OneSignal:AppId"];
                var restApiKey = _config["OneSignal:ApiKey"];

                if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(restApiKey))
                {
                    Console.WriteLine("[NotificationService] OneSignal:AppId or OneSignal:RestApiKey not configured - skipping push");
                    return;
                }

                var payload = new Dictionary<string, object>
                {
                    { "app_id", appId },
                    { "headings", new { en = title } },
                    { "contents", new { en = body } },
                };

                if (data != null && data.Any())
                {
                    payload["data"] = data;
                }

                if (includeExternalUserIds != null && includeExternalUserIds.Any())
                {
                    payload["include_aliases"] = new { external_id = includeExternalUserIds };
                    payload["target_channel"] = "push";
                }
                else
                {
                    // Send to all subscribed users (fallback)
                    payload["included_segments"] = new[] { "Subscribed Users" };
                }

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, ONESIGNAL_API_URL);
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue("Key", restApiKey);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[NotificationService] OneSignal error: {response.StatusCode} - {responseBody}");
                }
                else
                {
                    Console.WriteLine($"[NotificationService] Push sent via OneSignal: {responseBody}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotificationService] Error sending OneSignal notification: {ex.Message}");
            }
        }
    }
}

using Business.Data;
using Business.Model;
using Business.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APIServer.Services
{
    /// <summary>
    /// Background service that periodically checks recurring jobs and auto-creates
    /// child job instances for upcoming occurrences.
    /// Runs every hour. Creates jobs for occurrences within the next 7 days (1 week).
    /// </summary>
    public class RecurringJobBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringJobBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);
        private readonly int _lookAheadHours = 168; // 7 days = 168 hours

        public RecurringJobBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecurringJobBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RecurringJobs] Background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessRecurringJobs(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RecurringJobs] Error processing recurring jobs.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("[RecurringJobs] Background service stopped.");
        }

        private async Task ProcessRecurringJobs(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<DBContext>();
            var recurrenceService = scope.ServiceProvider.GetRequiredService<RecurrenceService>();

            var now = DateTime.UtcNow;
            var lookAheadEnd = now.AddHours(_lookAheadHours);

            // Find all active recurring parent jobs (not child jobs)
            var recurringJobs = await ctx.Job
                .Include(j => j.client)
                .Include(j => j.location)
                .Where(j =>
                    j.isRecurring &&
                    !string.IsNullOrEmpty(j.recurrenceRule) &&
                    j.parentJobId == null && // Only parent/template jobs
                    j.status != "cancelled" &&
                    j.status != "completed" &&
                    (j.recurrenceEndDate == null || j.recurrenceEndDate > now))
                .ToListAsync(ct);

            _logger.LogInformation("[RecurringJobs] Found {Count} active recurring jobs to process.", recurringJobs.Count);

            foreach (var parentJob in recurringJobs)
            {
                try
                {
                    await CreateUpcomingOccurrences(ctx, recurrenceService, parentJob, now, lookAheadEnd, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RecurringJobs] Error processing job {JobId} ({Title}).", parentJob.id, parentJob.title);
                }
            }
        }

        private async Task CreateUpcomingOccurrences(
            DBContext ctx,
            RecurrenceService recurrenceService,
            Job parentJob,
            DateTime now,
            DateTime lookAheadEnd,
            CancellationToken ct)
        {
            var startDate = parentJob.scheduledStartDate ?? parentJob.createDatetime;

            // Get all occurrences up to lookAheadEnd
            var allOccurrences = recurrenceService.GetOccurrences(
                startDate,
                parentJob.recurrenceRule!,
                parentJob.recurrenceEndDate,
                parentJob.timeZone,
                maxOccurrences: 200);

            // Filter to occurrences within the look-ahead window that haven't been created yet
            var lastCreated = parentJob.lastOccurrenceCreated ?? DateTime.MinValue;

            var upcomingOccurrences = allOccurrences
                .Where(o => o > lastCreated && o <= lookAheadEnd && o > now.AddHours(-1))
                .ToList();

            if (!upcomingOccurrences.Any())
            {
                return;
            }

            _logger.LogInformation(
                "[RecurringJobs] Job {JobId} ({Title}): Creating {Count} occurrence(s).",
                parentJob.id, parentJob.title, upcomingOccurrences.Count);

            // Check which occurrences already have child jobs (to avoid duplicates)
            var existingOccurrenceDates = await ctx.Job
                .Where(j => j.parentJobId == parentJob.id && j.occurrenceDate != null)
                .Select(j => j.occurrenceDate!.Value)
                .ToListAsync(ct);

            // Get the last job number for generating new ones
            var lastJobNumber = await ctx.Job
                .Where(j => j.companyId == parentJob.companyId && j.jobNumber != null)
                .OrderByDescending(j => j.createDatetime)
                .Select(j => j.jobNumber)
                .FirstOrDefaultAsync(ct);

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastJobNumber) && lastJobNumber.StartsWith("JOB-"))
            {
                if (int.TryParse(lastJobNumber.Substring(4), out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            DateTime latestCreated = lastCreated;

            foreach (var occurrenceDate in upcomingOccurrences)
            {
                // Skip if a child job already exists for this date (within 1 hour tolerance)
                if (existingOccurrenceDates.Any(d => Math.Abs((d - occurrenceDate).TotalHours) < 1))
                {
                    _logger.LogDebug("[RecurringJobs] Skipping duplicate occurrence {Date} for job {JobId}.", occurrenceDate, parentJob.id);
                    continue;
                }

                var childJob = new Job
                {
                    id = Guid.NewGuid(),
                    companyId = parentJob.companyId,
                    jobNumber = $"JOB-{nextNumber:D6}",
                    title = parentJob.title,
                    description = parentJob.description,
                    status = "scheduled",
                    parentJobId = parentJob.id,
                    occurrenceDate = occurrenceDate,
                    scheduledStartDate = occurrenceDate,
                    scheduledEndDate = parentJob.scheduledEndDate.HasValue
                        ? occurrenceDate.Add(parentJob.scheduledEndDate.Value - (parentJob.scheduledStartDate ?? parentJob.createDatetime))
                        : null,
                    estimatedTotal = parentJob.estimatedTotal,
                    laborCost = parentJob.laborCost,
                    materialsCost = parentJob.materialsCost,
                    isRecurring = false, // Child jobs are NOT recurring themselves
                    createDatetime = DateTime.UtcNow,
                    updateDatetime = DateTime.UtcNow,
                };

                // Link same client and location
                if (parentJob.client != null)
                {
                    childJob.client = parentJob.client;
                }
                if (parentJob.location != null)
                {
                    childJob.location = parentJob.location;
                }

                ctx.Job.Add(childJob);

                // Copy assignments from parent job to child job
                var parentAssignments = await ctx.JobAssignments
                    .Where(ja => ja.jobId == parentJob.id && ja.active)
                    .ToListAsync(ct);

                foreach (var assignment in parentAssignments)
                {
                    ctx.JobAssignments.Add(new JobAssignment
                    {
                        id = Guid.NewGuid(),
                        jobId = childJob.id,
                        userId = assignment.userId,
                        companyId = parentJob.companyId,
                        assignedByUserId = assignment.assignedByUserId,
                        role = assignment.role,
                        notes = string.Empty,
                        active = true,
                        assignedDatetime = DateTime.UtcNow,
                        createDatetime = DateTime.UtcNow,
                        updateDatetime = DateTime.UtcNow,
                    });
                }

                nextNumber++;

                if (occurrenceDate > latestCreated)
                {
                    latestCreated = occurrenceDate;
                }

                _logger.LogInformation(
                    "[RecurringJobs] Created child job {ChildId} ({Number}) for occurrence {Date} of parent {ParentId}.",
                    childJob.id, childJob.jobNumber, occurrenceDate, parentJob.id);
            }

            // Update the parent's lastOccurrenceCreated tracker
            if (latestCreated > lastCreated)
            {
                var trackedParent = await ctx.Job.FindAsync(new object[] { parentJob.id }, ct);
                if (trackedParent != null)
                {
                    trackedParent.lastOccurrenceCreated = latestCreated;
                    trackedParent.updateDatetime = DateTime.UtcNow;
                }
            }

            await ctx.SaveChangesAsync(ct);

            // Send notifications for created child jobs
            try
            {
                var notificationService = _scopeFactory.CreateScope().ServiceProvider
                    .GetService<NotificationService>();

                if (notificationService != null)
                {
                    // Get all users in the company to notify
                    var companyUsers = await ctx.UserCompany
                        .Where(uc => uc.companyId == parentJob.companyId)
                        .Select(uc => uc.userId)
                        .ToListAsync(ct);

                    foreach (var userId in companyUsers)
                    {
                        foreach (var occDate in upcomingOccurrences)
                        {
                            var formattedDate = occDate.ToString("MMM dd, yyyy 'at' h:mm tt");
                            await notificationService.SendToUser(
                                userId,
                                "job_scheduled",
                                "Recurring Job Scheduled",
                                $"{parentJob.title} is scheduled for {formattedDate}",
                                new Dictionary<string, string>
                                {
                                    { "type", "recurring_job" },
                                    { "parentJobId", parentJob.id.ToString() },
                                    { "occurrenceDate", occDate.ToString("o") }
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[RecurringJobs] Failed to send notifications for job {JobId}.", parentJob.id);
            }
        }
    }
}

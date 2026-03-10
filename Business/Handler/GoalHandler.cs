using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers;

public class GoalHandler
{
    private readonly DBContext _ctx;
    private readonly IMapper _mapper;

    public GoalHandler(DBContext ctx, IMapper mapper)
    {
        _ctx = ctx;
        _mapper = mapper;
    }

    /// <summary>
    /// Create a new goal for a job
    /// </summary>
    public async Task<GoalRM> CreateGoalAsync(CreateGoalVM vm, Guid companyId, Guid createdByUserId)
    {
        var now = DateTime.UtcNow;

        // Verify job exists and belongs to company
        var job = await _ctx.Job.FirstOrDefaultAsync(j => j.id == vm.jobId && j.companyId == companyId);
        if (job == null)
        {
            throw new Exception("Job not found or access denied");
        }

        // If assigned to user, verify user is in company
        if (vm.assignedToUserId.HasValue)
        {
            var userInCompany = await _ctx.UserCompany
                .AnyAsync(uc => uc.userId == vm.assignedToUserId.Value && uc.companyId == companyId);
            if (!userInCompany)
            {
                throw new Exception("Assigned user is not a member of this company");
            }
        }

        // Get max sort order for this job
        var maxSortOrder = await _ctx.JobGoals
            .Where(g => g.jobId == vm.jobId)
            .MaxAsync(g => (int?)g.sortOrder) ?? 0;

        var goal = new JobGoal
        {
            id = Guid.NewGuid(),
            jobId = vm.jobId,
            companyId = companyId,
            title = vm.title,
            description = vm.description ?? string.Empty,
            targetDate = vm.deadline,
            assignedToUserId = vm.assignedToUserId,
            createdByUserId = createdByUserId,
            bonusAmount = vm.bonusAmount,
            requiresVerification = vm.requiresVerification,
            status = "pending",
            completed = false,
            sortOrder = vm.sortOrder > 0 ? vm.sortOrder : maxSortOrder + 1,
            createDatetime = now,
            updateDatetime = now
        };

        _ctx.JobGoals.Add(goal);
        await _ctx.SaveChangesAsync();

        return await GetGoalByIdAsync(goal.id, companyId);
    }

    /// <summary>
    /// Update an existing goal
    /// </summary>
    public async Task<GoalRM> UpdateGoalAsync(Guid goalId, UpdateGoalVM vm, Guid companyId)
    {
        var goal = await _ctx.JobGoals.FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);
        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        if (goal.status == "completed")
        {
            throw new Exception("Cannot update a completed goal");
        }

        if (!string.IsNullOrWhiteSpace(vm.title))
            goal.title = vm.title;
        
        if (vm.description != null)
            goal.description = vm.description;
        
        if (vm.deadline.HasValue)
            goal.targetDate = vm.deadline.Value;
        
        if (vm.assignedToUserId.HasValue)
        {
            // Verify user is in company
            var userInCompany = await _ctx.UserCompany
                .AnyAsync(uc => uc.userId == vm.assignedToUserId.Value && uc.companyId == companyId);
            if (!userInCompany)
            {
                throw new Exception("Assigned user is not a member of this company");
            }
            goal.assignedToUserId = vm.assignedToUserId.Value;
        }
        
        if (vm.sortOrder.HasValue)
            goal.sortOrder = vm.sortOrder.Value;

        goal.updateDatetime = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();

        return await GetGoalByIdAsync(goal.id, companyId);
    }

    /// <summary>
    /// Complete a goal - tracks if completed on time or late
    /// </summary>
    public async Task<GoalRM> CompleteGoalAsync(Guid goalId, Guid companyId, Guid completedByUserId)
    {
        var goal = await _ctx.JobGoals.FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);
        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        if (goal.status == "completed")
        {
            throw new Exception("Goal is already completed");
        }

        var now = DateTime.UtcNow;
        goal.status = "completed";
        goal.completed = true;
        goal.completedDate = now;
        goal.completedBy = completedByUserId;
        goal.completedOnTime = now <= goal.targetDate; // true if completed before or on deadline
        goal.updateDatetime = now;

        await _ctx.SaveChangesAsync();

        Console.WriteLine($"[Goal] Completed goal '{goal.title}' - On time: {goal.completedOnTime}");

        return await GetGoalByIdAsync(goal.id, companyId);
    }

    /// <summary>
    /// Reopen a completed goal
    /// </summary>
    public async Task<GoalRM> ReopenGoalAsync(Guid goalId, Guid companyId)
    {
        var goal = await _ctx.JobGoals.FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);
        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        goal.status = goal.targetDate < DateTime.UtcNow ? "overdue" : "pending";
        goal.completed = false;
        goal.completedDate = null;
        goal.completedBy = null;
        goal.completedOnTime = null;
        goal.updateDatetime = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();

        return await GetGoalByIdAsync(goal.id, companyId);
    }

    /// <summary>
    /// Delete a goal
    /// </summary>
    public async Task DeleteGoalAsync(Guid goalId, Guid companyId)
    {
        var goal = await _ctx.JobGoals.FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);
        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        _ctx.JobGoals.Remove(goal);
        await _ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Get a goal by ID
    /// </summary>
    public async Task<GoalRM> GetGoalByIdAsync(Guid goalId, Guid companyId)
    {
        var goal = await _ctx.JobGoals
            .Include(g => g.assignedToUser)
            .Include(g => g.createdByUser)
            .Include(g => g.completedByUser)
            .FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);

        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        return MapToGoalRM(goal);
    }

    /// <summary>
    /// Get goals for a job
    /// </summary>
    public async Task<List<GoalRM>> GetGoalsByJobAsync(Guid jobId, Guid companyId, string? status = null)
    {
        var query = _ctx.JobGoals
            .Include(g => g.assignedToUser)
            .Include(g => g.createdByUser)
            .Include(g => g.completedByUser)
            .Include(g => g.votes)
                .ThenInclude(v => v.user)
            .Where(g => g.jobId == jobId && g.companyId == companyId);

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (status == "overdue")
            {
                query = query.Where(g => g.status == "pending" && g.targetDate < DateTime.UtcNow);
            }
            else
            {
                query = query.Where(g => g.status == status);
            }
        }

        // Get job assignments for vote counting
        var jobAssignments = await _ctx.JobAssignments
            .Where(ja => ja.jobId == jobId && ja.companyId == companyId && ja.active)
            .Select(ja => ja.userId)
            .ToListAsync();

        // Get admins from the company (they count as eligible voters even if not explicitly assigned)
        var adminUserIds = await _ctx.UserCompany
            .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
            .Select(uc => uc.userId)
            .ToListAsync();

        // Combine job assignments with admins (admins always count as eligible voters)
        var eligibleVoters = jobAssignments.Union(adminUserIds).ToList();

        var goals = await query
            .OrderBy(g => g.sortOrder)
            .ThenBy(g => g.targetDate)
            .ToListAsync();

        // Update overdue status for pending goals
        var now = DateTime.UtcNow;
        foreach (var goal in goals.Where(g => g.status == "pending" && g.targetDate < now))
        {
            goal.status = "overdue";
        }

        return goals.Select(g => MapToGoalRM(g, eligibleVoters)).ToList();
    }

    /// <summary>
    /// Get goals assigned to a specific user (My Goals)
    /// </summary>
    public async Task<List<GoalRM>> GetMyGoalsAsync(Guid userId, Guid companyId, string? status = null)
    {
        var query = _ctx.JobGoals
            .Include(g => g.assignedToUser)
            .Include(g => g.createdByUser)
            .Include(g => g.completedByUser)
            .Include(g => g.job)
            .Where(g => g.companyId == companyId && 
                       (g.assignedToUserId == userId || g.assignedToUserId == null)); // User's goals OR team goals

        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (status == "overdue")
            {
                query = query.Where(g => g.status == "pending" && g.targetDate < DateTime.UtcNow);
            }
            else
            {
                query = query.Where(g => g.status == status);
            }
        }

        var goals = await query
            .OrderBy(g => g.targetDate)
            .ThenBy(g => g.sortOrder)
            .ToListAsync();

        // Update overdue status
        var now = DateTime.UtcNow;
        foreach (var goal in goals.Where(g => g.status == "pending" && g.targetDate < now))
        {
            goal.status = "overdue";
        }

        return goals.Select(g => MapToGoalRM(g, null)).ToList();
    }

    /// <summary>
    /// Get goals summary for a job
    /// </summary>
    public async Task<JobGoalsSummaryRM> GetJobGoalsSummaryAsync(Guid jobId, Guid companyId)
    {
        var goals = await _ctx.JobGoals
            .Where(g => g.jobId == jobId && g.companyId == companyId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var completed = goals.Where(g => g.status == "completed").ToList();
        var pending = goals.Where(g => g.status != "completed" && g.targetDate >= now).ToList();
        var overdue = goals.Where(g => g.status != "completed" && g.targetDate < now).ToList();

        return new JobGoalsSummaryRM
        {
            jobId = jobId,
            totalGoals = goals.Count,
            completedGoals = completed.Count,
            pendingGoals = pending.Count,
            overdueGoals = overdue.Count,
            completedOnTime = completed.Count(g => g.completedOnTime == true),
            completedLate = completed.Count(g => g.completedOnTime == false),
            completionRate = goals.Count > 0 ? Math.Round((double)completed.Count / goals.Count * 100, 1) : 0,
            onTimeRate = completed.Count > 0 ? Math.Round((double)completed.Count(g => g.completedOnTime == true) / completed.Count * 100, 1) : 0
        };
    }

    /// <summary>
    /// Get goals summary for a user
    /// </summary>
    public async Task<UserGoalsSummaryRM> GetUserGoalsSummaryAsync(Guid userId, Guid companyId)
    {
        var now = DateTime.UtcNow;

        var goals = await _ctx.JobGoals
            .Include(g => g.assignedToUser)
            .Include(g => g.createdByUser)
            .Include(g => g.job)
            .Where(g => g.companyId == companyId && 
                       (g.assignedToUserId == userId || g.assignedToUserId == null))
            .ToListAsync();

        var completed = goals.Where(g => g.status == "completed").ToList();
        var pending = goals.Where(g => g.status != "completed" && g.targetDate >= now).ToList();
        var overdue = goals.Where(g => g.status != "completed" && g.targetDate < now).ToList();

        return new UserGoalsSummaryRM
        {
            userId = userId,
            totalAssigned = goals.Count,
            completed = completed.Count,
            pending = pending.Count,
            overdue = overdue.Count,
            completedOnTime = completed.Count(g => g.completedOnTime == true),
            completedLate = completed.Count(g => g.completedOnTime == false),
            completionRate = goals.Count > 0 ? Math.Round((double)completed.Count / goals.Count * 100, 1) : 0,
            onTimeRate = completed.Count > 0 ? Math.Round((double)completed.Count(g => g.completedOnTime == true) / completed.Count * 100, 1) : 0,
            upcomingGoals = pending
                .OrderBy(g => g.targetDate)
                .Take(5)
                .Select(g => MapToGoalRM(g, null))
                .ToList(),
            overdueGoals = overdue
                .OrderBy(g => g.targetDate)
                .Take(5)
                .Select(g => MapToGoalRM(g, null))
                .ToList()
        };
    }

    /// <summary>
    /// Vote on a team goal (goals without assignedToUserId)
    /// </summary>
    public async Task<GoalRM> VoteGoalAsync(Guid goalId, Guid companyId, Guid userId, bool voteComplete = true)
    {
        var goal = await _ctx.JobGoals
            .Include(g => g.votes)
            .FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);

        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        if (goal.status == "completed")
        {
            throw new Exception("Cannot vote on a completed goal");
        }

        if (goal.assignedToUserId.HasValue)
        {
            throw new Exception("Voting is only allowed on team goals (goals without specific assignment)");
        }

        // Get job assignments
        var jobAssignments = await _ctx.JobAssignments
            .Where(ja => ja.jobId == goal.jobId && ja.companyId == companyId && ja.active)
            .Select(ja => ja.userId)
            .ToListAsync();

        // Get admins from the company
        var adminUserIds = await _ctx.UserCompany
            .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
            .Select(uc => uc.userId)
            .ToListAsync();

        // Combine job assignments with admins
        var eligibleVoters = jobAssignments.Union(adminUserIds).ToList();

        if (!eligibleVoters.Contains(userId))
        {
            throw new Exception("You must be assigned to this job or be an admin to vote on goals");
        }

        // Check if user already voted
        var existingVote = goal.votes?.FirstOrDefault(v => v.userId == userId);
        
        if (existingVote != null)
        {
            existingVote.voteComplete = voteComplete;
            existingVote.voteDate = DateTime.UtcNow;
        }
        else
        {
            var vote = new JobGoalVote
            {
                id = Guid.NewGuid(),
                goalId = goal.id,
                userId = userId,
                voteComplete = voteComplete,
                voteDate = DateTime.UtcNow,
                createDatetime = DateTime.UtcNow
            };
            _ctx.JobGoalVotes.Add(vote);
        }

        await _ctx.SaveChangesAsync();

        // Check if all eligible voters have voted complete
        var updatedGoal = await _ctx.JobGoals
            .Include(g => g.votes)
            .FirstOrDefaultAsync(g => g.id == goalId);

        if (updatedGoal != null)
        {
            var votesComplete = updatedGoal.votes?.Count(v => v.voteComplete && eligibleVoters.Contains(v.userId)) ?? 0;
            
            if (votesComplete >= eligibleVoters.Count && eligibleVoters.Count > 0)
            {
                updatedGoal.status = "completed";
                updatedGoal.completed = true;
                updatedGoal.completedDate = DateTime.UtcNow;
                updatedGoal.completedBy = userId;
                updatedGoal.completedOnTime = DateTime.UtcNow <= updatedGoal.targetDate;
                updatedGoal.updateDatetime = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
            }
        }

        return await GetGoalByIdWithVotersAsync(goalId, companyId);
    }

    /// <summary>
    /// Force complete a goal (Admin only) - bypasses voting requirement
    /// </summary>
    public async Task<GoalRM> ForceCompleteGoalAsync(Guid goalId, Guid companyId, Guid userId)
    {
        // Verify user is admin
        var isAdmin = await _ctx.UserCompany
            .AnyAsync(uc => uc.userId == userId && uc.companyId == companyId && 
                          (uc.role == "Admin" || uc.role == "SuperAdmin"));

        if (!isAdmin)
        {
            throw new Exception("Only admins can force complete goals");
        }

        var goal = await _ctx.JobGoals.FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);
        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        if (goal.status == "completed")
        {
            throw new Exception("Goal is already completed");
        }

        var now = DateTime.UtcNow;
        goal.status = "completed";
        goal.completed = true;
        goal.completedDate = now;
        goal.completedBy = userId;
        goal.completedOnTime = now <= goal.targetDate;
        goal.updateDatetime = now;

        await _ctx.SaveChangesAsync();

        return await GetGoalByIdWithVotersAsync(goalId, companyId);
    }

    /// <summary>
    /// Get goal by ID with eligible voters calculated
    /// </summary>
    private async Task<GoalRM> GetGoalByIdWithVotersAsync(Guid goalId, Guid companyId)
    {
        var goal = await _ctx.JobGoals
            .Include(g => g.assignedToUser)
            .Include(g => g.createdByUser)
            .Include(g => g.completedByUser)
            .Include(g => g.votes)
                .ThenInclude(v => v.user)
            .FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);

        if (goal == null)
        {
            throw new Exception("Goal not found or access denied");
        }

        // Get eligible voters for team goals
        List<Guid>? eligibleVoters = null;
        if (!goal.assignedToUserId.HasValue)
        {
            var jobAssignments = await _ctx.JobAssignments
                .Where(ja => ja.jobId == goal.jobId && ja.companyId == companyId && ja.active)
                .Select(ja => ja.userId)
                .ToListAsync();

            var adminUserIds = await _ctx.UserCompany
                .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                .Select(uc => uc.userId)
                .ToListAsync();

            eligibleVoters = jobAssignments.Union(adminUserIds).ToList();
        }

        return MapToGoalRM(goal, eligibleVoters);
    }

    /// <summary>
    /// Bulk create goals for a job
    /// </summary>
    public async Task<List<GoalRM>> BulkCreateGoalsAsync(Guid jobId, List<CreateGoalVM> goals, Guid companyId, Guid createdByUserId)
    {
        var createdGoals = new List<GoalRM>();

        foreach (var vm in goals)
        {
            vm.jobId = jobId;
            var goal = await CreateGoalAsync(vm, companyId, createdByUserId);
            createdGoals.Add(goal);
        }

        return createdGoals;
    }

    private GoalRM MapToGoalRM(JobGoal goal, List<Guid>? eligibleVoters = null)
    {
        var now = DateTime.UtcNow;
        var daysUntilDeadline = (int)(goal.targetDate - now).TotalDays;

        // Calculate votes for team goals
        var totalVotesNeeded = 0;
        var votesReceived = 0;
        var votes = new List<GoalVoteRM>();

        if (!goal.assignedToUserId.HasValue && eligibleVoters != null && eligibleVoters.Count > 0) // Team goal
        {
            totalVotesNeeded = eligibleVoters.Count;
            votesReceived = goal.votes?.Count(v => v.voteComplete && eligibleVoters.Contains(v.userId)) ?? 0;
            votes = goal.votes?.Select(v => new GoalVoteRM
            {
                id = v.id,
                goalId = v.goalId,
                userId = v.userId,
                userName = v.user?.fullName ?? string.Empty,
                voteComplete = v.voteComplete,
                voteDate = v.voteDate
            }).ToList() ?? new List<GoalVoteRM>();
        }

        return new GoalRM
        {
            id = goal.id,
            jobId = goal.jobId,
            companyId = goal.companyId,
            title = goal.title,
            description = goal.description,
            deadline = goal.targetDate,
            isOverdue = goal.status != "completed" && goal.targetDate < now,
            daysUntilDeadline = daysUntilDeadline,
            assignedToUserId = goal.assignedToUserId,
            assignedToUserName = goal.assignedToUser?.fullName,
            assignedToUserPhoto = goal.assignedToUser?.personalPhotoThumbnail,
            isTeamGoal = !goal.assignedToUserId.HasValue,
            createdByUserId = goal.createdByUserId ?? Guid.Empty,
            createdByUserName = goal.createdByUser?.fullName,
            status = goal.status == "pending" && goal.targetDate < now ? "overdue" : goal.status,
            completedAt = goal.completedDate,
            completedOnTime = goal.completedOnTime,
            completedByUserId = goal.completedBy,
            completedByUserName = goal.completedByUser?.fullName,
            votes = votes,
            totalVotesNeeded = totalVotesNeeded,
            votesReceived = votesReceived,
            sortOrder = goal.sortOrder,
            bonusAmount = goal.bonusAmount,
            createDatetime = goal.createDatetime,
            updateDatetime = goal.updateDatetime
        };
    }
}

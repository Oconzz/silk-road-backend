using Business.Data;
using Business.Model;
using Business.Response;
using Business.Services;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers;

public class JobGoalHandler
{
    private readonly DBContext _context;
    private readonly IPhotoService _photoService;

    public JobGoalHandler(DBContext context, IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
    }

    public async Task<List<JobGoalRM>> GetJobGoalListAsync(
        JobGoalListRequestVM request, 
        Guid companyId)
    {
        var query = _context.JobGoals
            .Where(g => g.companyId == companyId && g.jobId == request.jobId)
            .AsQueryable();

        if (request.completed.HasValue)
        {
            query = query.Where(g => g.completed == request.completed.Value);
        }

        if (request.verified.HasValue)
        {
            query = query.Where(g => g.verified == request.verified.Value);
        }

        // Get team members assigned to this job for vote counting
        var jobAssignments = await _context.JobAssignments
            .Where(ja => ja.jobId == request.jobId)
            .Select(ja => ja.userId)
            .ToListAsync();

        // Get admins from the company (they count as voters even if not explicitly assigned)
        var adminUserIds = await _context.UserCompany
            .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
            .Select(uc => uc.userId)
            .ToListAsync();

        // Combine job assignments with admins (admins always count as eligible voters)
        var allEligibleVoters = jobAssignments.Union(adminUserIds).ToList();

        var goals = await query
            .OrderBy(g => g.targetDate)
            .Include(g => g.completedByUser)
            .Include(g => g.verifiedByUser)
            .Include(g => g.assignedToUser)
            .Include(g => g.media)
            .Include(g => g.votes)
                .ThenInclude(v => v.user)
            .ToListAsync();

        return goals.Select(g => {
            // Calculate votes for team goals (admins count as team members for voting)
            var totalVotesNeeded = g.assignedToUserId == null ? allEligibleVoters.Count : 0;
            var votesReceived = g.assignedToUserId == null
                ? g.votes.Count(v => v.voteComplete && allEligibleVoters.Contains(v.userId))
                : 0;

            return new JobGoalRM
            {
                id = g.id,
                jobId = g.jobId,
                companyId = g.companyId,
                title = g.title,
                description = g.description,
                bonusAmount = g.bonusAmount,
                completionBonus = g.completionBonus,
                assignedToUserId = g.assignedToUserId,
                assignedToUserName = g.assignedToUser?.fullName,
                targetDate = g.targetDate,
                completed = g.completed,
                completedDate = g.completedDate,
                completedBy = g.completedBy,
                completedByName = g.completedByUser?.fullName,
                requiresVerification = g.requiresVerification,
                verified = g.verified,
                verifiedBy = g.verifiedBy,
                verifiedByName = g.verifiedByUser?.fullName,
                verifiedDate = g.verifiedDate,
                media = g.media.Select(m => new JobGoalMediaRM
                {
                    id = m.id,
                    goalId = m.goalId,
                    webUrl = m.webUrl,
                    thumbUrl = m.thumbUrl,
                    createDatetime = m.createDatetime
                }).ToList(),
                votes = g.votes.Select(v => new JobGoalVoteRM
                {
                    id = v.id,
                    goalId = v.goalId,
                    userId = v.userId,
                    userName = v.user?.fullName ?? string.Empty,
                    voteComplete = v.voteComplete,
                    voteDate = v.voteDate
                }).ToList(),
                totalVotesNeeded = totalVotesNeeded,
                votesReceived = votesReceived,
                createDatetime = g.createDatetime,
                updateDatetime = g.updateDatetime
            };
        }).ToList();
    }

    public async Task<JobGoalRM?> GetJobGoalByIdAsync(Guid id, Guid companyId)
    {
        var goal = await _context.JobGoals
            .Where(g => g.id == id && g.companyId == companyId)
            .Include(g => g.completedByUser)
            .Include(g => g.verifiedByUser)
            .Include(g => g.assignedToUser)
            .Include(g => g.media)
            .Include(g => g.votes)
                .ThenInclude(v => v.user)
            .FirstOrDefaultAsync();

        if (goal == null) return null;

        // Calculate votes needed (team members assigned to job + admins) for team goals
        var totalVotesNeeded = 0;
        var votesReceived = 0;

        if (goal.assignedToUserId == null) // Team goal
        {
            var jobAssignments = await _context.JobAssignments
                .Where(ja => ja.jobId == goal.jobId)
                .Select(ja => ja.userId)
                .ToListAsync();

            // Get admins from the company (they count as voters even if not explicitly assigned)
            var adminUserIds = await _context.UserCompany
                .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
                .Select(uc => uc.userId)
                .ToListAsync();

            // Combine job assignments with admins (admins always count as eligible voters)
            var allEligibleVoters = jobAssignments.Union(adminUserIds).ToList();

            totalVotesNeeded = allEligibleVoters.Count;
            votesReceived = goal.votes.Count(v => v.voteComplete && allEligibleVoters.Contains(v.userId));
        }

        return new JobGoalRM
        {
            id = goal.id,
            jobId = goal.jobId,
            companyId = goal.companyId,
            title = goal.title,
            description = goal.description,
            bonusAmount = goal.bonusAmount,
            completionBonus = goal.completionBonus,
            assignedToUserId = goal.assignedToUserId,
            assignedToUserName = goal.assignedToUser?.fullName,
            targetDate = goal.targetDate,
            completed = goal.completed,
            completedDate = goal.completedDate,
            completedBy = goal.completedBy,
            completedByName = goal.completedByUser?.fullName,
            requiresVerification = goal.requiresVerification,
            verified = goal.verified,
            verifiedBy = goal.verifiedBy,
            verifiedByName = goal.verifiedByUser?.fullName,
            verifiedDate = goal.verifiedDate,
            media = goal.media.Select(m => new JobGoalMediaRM
            {
                id = m.id,
                goalId = m.goalId,
                webUrl = m.webUrl,
                thumbUrl = m.thumbUrl,
                createDatetime = m.createDatetime
            }).ToList(),
            votes = goal.votes.Select(v => new JobGoalVoteRM
            {
                id = v.id,
                goalId = v.goalId,
                userId = v.userId,
                userName = v.user?.fullName ?? string.Empty,
                voteComplete = v.voteComplete,
                voteDate = v.voteDate
            }).ToList(),
            totalVotesNeeded = totalVotesNeeded,
            votesReceived = votesReceived,
            createDatetime = goal.createDatetime,
            updateDatetime = goal.updateDatetime
        };
    }

    public async Task<JobGoalRM> CreateJobGoalAsync(CreateJobGoalVM request, Guid companyId)
    {
        var goal = new JobGoal
        {
            id = Guid.NewGuid(),
            jobId = request.jobId,
            companyId = companyId,
            title = request.title,
            description = request.description,
            bonusAmount = request.bonusAmount,
            targetDate = request.targetDate,
            completed = false,
            requiresVerification = request.requiresVerification,
            verified = false,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.JobGoals.Add(goal);
        await _context.SaveChangesAsync();

        return await GetJobGoalByIdAsync(goal.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created goal");
    }

    public async Task<JobGoalRM?> UpdateJobGoalAsync(UpdateJobGoalVM request, Guid companyId)
    {
        var goal = await _context.JobGoals
            .FirstOrDefaultAsync(g => g.id == request.id && g.companyId == companyId);

        if (goal == null) return null;

        if (!string.IsNullOrEmpty(request.title))
            goal.title = request.title;

        if (request.description != null)
            goal.description = request.description;

        if (request.bonusAmount.HasValue)
            goal.bonusAmount = request.bonusAmount.Value;

        if (request.targetDate.HasValue)
            goal.targetDate = request.targetDate.Value;

        if (request.requiresVerification.HasValue)
            goal.requiresVerification = request.requiresVerification.Value;

        goal.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobGoalByIdAsync(goal.id, companyId);
    }

    public async Task<JobGoalRM?> CompleteJobGoalAsync(
        CompleteJobGoalVM request, 
        Guid companyId, 
        Guid userId)
    {
        var goal = await _context.JobGoals
            .FirstOrDefaultAsync(g => g.id == request.id && g.jobId == request.jobId && g.companyId == companyId);

        if (goal == null) return null;

        // Check if this is a team goal (assignedToUserId == null)
        // Team goals require voting from all team members to complete
        if (goal.assignedToUserId == null)
        {
            var jobAssignments = await _context.JobAssignments
                .Where(ja => ja.jobId == request.jobId)
                .Select(ja => ja.userId)
                .ToListAsync();

            if (jobAssignments.Count > 0)
            {
                throw new InvalidOperationException(
                    "Team goals require all team members to vote for completion. Use the /vote endpoint instead.");
            }
        }

        goal.completed = true;
        goal.completedDate = DateTime.UtcNow;
        goal.completedBy = userId;
        goal.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobGoalByIdAsync(goal.id, companyId);
    }

    public async Task<JobGoalRM?> VerifyJobGoalAsync(
        VerifyJobGoalVM request, 
        Guid companyId, 
        Guid userId)
    {
        var goal = await _context.JobGoals
            .FirstOrDefaultAsync(g => g.id == request.id && g.jobId == request.jobId && g.companyId == companyId);

        if (goal == null) return null;

        if (!goal.completed)
        {
            throw new InvalidOperationException("Goal must be completed before verification");
        }

        goal.verified = request.approved;
        goal.verifiedBy = userId;
        goal.verifiedDate = DateTime.UtcNow;
        goal.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobGoalByIdAsync(goal.id, companyId);
    }

    public async Task<JobGoalMediaRM> AddJobGoalMediaAsync(
        AddJobGoalMediaVM request, 
        Guid companyId)
    {
        var keyPrefix = $"goals/{request.goalId}";
        var (webUrl, thumbUrl) = await _photoService.ProcessAndUploadAsync(request.base64Image, keyPrefix);

        if (string.IsNullOrEmpty(webUrl) || string.IsNullOrEmpty(thumbUrl))
        {
            throw new InvalidOperationException("Failed to upload image");
        }

        var media = new JobGoalMedia
        {
            id = Guid.NewGuid(),
            goalId = request.goalId,
            webUrl = webUrl,
            thumbUrl = thumbUrl,
            createDatetime = DateTime.UtcNow
        };

        _context.JobGoalMedia.Add(media);
        await _context.SaveChangesAsync();

        return new JobGoalMediaRM
        {
            id = media.id,
            goalId = media.goalId,
            webUrl = media.webUrl,
            thumbUrl = media.thumbUrl,
            createDatetime = media.createDatetime
        };
    }

    public async Task<bool> DeleteJobGoalAsync(Guid id, Guid companyId)
    {
        var goal = await _context.JobGoals
            .FirstOrDefaultAsync(g => g.id == id && g.companyId == companyId);

        if (goal == null) return false;

        _context.JobGoals.Remove(goal);
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Cast a vote on a team goal (goals with assignedToUserId == null).
    /// When all assigned team members vote complete, the goal is marked as completed.
    /// </summary>
    public async Task<JobGoalRM?> CastGoalVoteAsync(CastGoalVoteVM request, Guid companyId, Guid userId)
    {
        var goal = await _context.JobGoals
            .Include(g => g.votes)
            .FirstOrDefaultAsync(g => g.id == request.goalId && g.jobId == request.jobId && g.companyId == companyId);

        if (goal == null) return null;

        // Only allow voting on team goals (assignedToUserId == null)
        if (goal.assignedToUserId != null)
        {
            throw new InvalidOperationException("Voting is only allowed on team goals (goals assigned to everyone)");
        }

        // Get team members assigned to this job
        var jobAssignments = await _context.JobAssignments
            .Where(ja => ja.jobId == request.jobId)
            .Select(ja => ja.userId)
            .ToListAsync();

        // Get admins from the company (they can vote even if not explicitly assigned)
        var adminUserIds = await _context.UserCompany
            .Where(uc => uc.companyId == companyId && (uc.role == "Admin" || uc.role == "SuperAdmin"))
            .Select(uc => uc.userId)
            .ToListAsync();

        // Combine job assignments with admins (admins always count as eligible voters)
        var allEligibleVoters = jobAssignments.Union(adminUserIds).ToList();

        if (!allEligibleVoters.Contains(userId))
        {
            throw new InvalidOperationException("You must be assigned to this job or be an admin to vote on goals");
        }

        // Check if user already voted
        var existingVote = goal.votes.FirstOrDefault(v => v.userId == userId);
        
        if (existingVote != null)
        {
            // Update existing vote
            existingVote.voteComplete = request.voteComplete;
            existingVote.voteDate = DateTime.UtcNow;
        }
        else
        {
            // Create new vote
            var vote = new JobGoalVote
            {
                id = Guid.NewGuid(),
                goalId = goal.id,
                userId = userId,
                voteComplete = request.voteComplete,
                voteDate = DateTime.UtcNow,
                createDatetime = DateTime.UtcNow
            };
            _context.JobGoalVotes.Add(vote);
        }

        await _context.SaveChangesAsync();

        // Check if all eligible voters (team members + admins) have voted complete
        var allVotesComplete = await CheckAndCompleteGoalByVotes(goal.id, allEligibleVoters, companyId, userId);

        return await GetJobGoalByIdAsync(goal.id, companyId);
    }

    /// <summary>
    /// Check if all eligible voters (team members + admins) voted complete and mark goal as completed if so
    /// </summary>
    private async Task<bool> CheckAndCompleteGoalByVotes(Guid goalId, List<Guid> eligibleVoterIds, Guid companyId, Guid userId)
    {
        var goal = await _context.JobGoals
            .Include(g => g.votes)
            .FirstOrDefaultAsync(g => g.id == goalId && g.companyId == companyId);

        if (goal == null || goal.completed) return false;

        var totalVotesNeeded = eligibleVoterIds.Count;
        var votesComplete = goal.votes.Count(v => v.voteComplete && eligibleVoterIds.Contains(v.userId));

        if (votesComplete >= totalVotesNeeded && totalVotesNeeded > 0)
        {
            goal.completed = true;
            goal.completedDate = DateTime.UtcNow;
            goal.completedBy = userId; // Last voter to complete
            goal.status = "completed";
            goal.updateDatetime = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }
}

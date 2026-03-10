using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Business.Handlers;

public class JobAssignmentHandler
{
    private readonly DBContext _ctx;

    public JobAssignmentHandler(DBContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>
    /// Assign a team member to a job
    /// </summary>
    public async Task<JobAssignmentRM?> AssignTeamMemberAsync(AssignTeamMemberVM request, Guid companyId, Guid assignedByUserId)
    {
        // Check if job exists and belongs to company
        var job = await _ctx.Job.FirstOrDefaultAsync(j => j.id == request.jobId && j.companyId == companyId);
        if (job == null) return null;

        // Check if user is a team member of the company
        var userCompany = await _ctx.UserCompany
            .Include(uc => uc.user)
            .FirstOrDefaultAsync(uc => uc.userId == request.userId && uc.companyId == companyId);
        if (userCompany == null) return null;

        // Check if already assigned (active)
        var existingAssignment = await _ctx.JobAssignments
            .FirstOrDefaultAsync(ja => ja.jobId == request.jobId && ja.userId == request.userId && ja.active);
        
        if (existingAssignment != null)
        {
            // Already assigned, return existing
            return await GetAssignmentByIdAsync(existingAssignment.id, companyId);
        }

        // Create new assignment
        var assignment = new JobAssignment
        {
            jobId = request.jobId,
            userId = request.userId,
            companyId = companyId,
            assignedByUserId = assignedByUserId,
            role = userCompany.role,
            notes = request.notes ?? string.Empty,
            active = true,
            assignedDatetime = DateTime.UtcNow,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _ctx.JobAssignments.Add(assignment);
        await _ctx.SaveChangesAsync();

        return await GetAssignmentByIdAsync(assignment.id, companyId);
    }

    /// <summary>
    /// Unassign a team member from a job
    /// </summary>
    public async Task<bool> UnassignTeamMemberAsync(UnassignTeamMemberVM request, Guid companyId)
    {
        var assignment = await _ctx.JobAssignments
            .FirstOrDefaultAsync(ja => ja.jobId == request.jobId && ja.userId == request.userId && ja.companyId == companyId && ja.active);

        if (assignment == null) return false;

        assignment.active = false;
        assignment.unassignedDatetime = DateTime.UtcNow;
        assignment.updateDatetime = DateTime.UtcNow;

        await _ctx.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get assignment by ID
    /// </summary>
    public async Task<JobAssignmentRM?> GetAssignmentByIdAsync(Guid assignmentId, Guid companyId)
    {
        return await _ctx.JobAssignments
            .Include(ja => ja.job)
            .Include(ja => ja.user)
            .Include(ja => ja.assignedByUser)
            .Where(ja => ja.id == assignmentId && ja.companyId == companyId)
            .Select(ja => new JobAssignmentRM
            {
                id = ja.id,
                jobId = ja.jobId,
                jobTitle = ja.job != null ? ja.job.title : string.Empty,
                jobStatus = ja.job != null ? ja.job.status : string.Empty,
                userId = ja.userId,
                userName = ja.user != null ? ja.user.fullName : string.Empty,
                userEmail = ja.user != null ? ja.user.email : string.Empty,
                userPhoto = ja.user != null ? ja.user.personalPhotoThumbnail : null,
                role = ja.role,
                notes = ja.notes,
                active = ja.active,
                assignedDatetime = ja.assignedDatetime,
                unassignedDatetime = ja.unassignedDatetime,
                assignedByUserName = ja.assignedByUser != null ? ja.assignedByUser.fullName : string.Empty
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Get assignments for a job
    /// </summary>
    public async Task<List<AssignedTeamMemberRM>> GetJobAssignmentsAsync(Guid jobId, Guid companyId)
    {
        return await _ctx.JobAssignments
            .Include(ja => ja.user)
            .Where(ja => ja.jobId == jobId && ja.companyId == companyId && ja.active)
            .OrderBy(ja => ja.assignedDatetime)
            .Select(ja => new AssignedTeamMemberRM
            {
                userId = ja.userId,
                fullName = ja.user != null ? ja.user.fullName : string.Empty,
                email = ja.user != null ? ja.user.email : string.Empty,
                photoThumbnail = ja.user != null ? ja.user.personalPhotoThumbnail : null,
                role = ja.role,
                assignedDatetime = ja.assignedDatetime
            })
            .ToListAsync();
    }

    /// <summary>
    /// Get jobs assigned to a specific user (for non-admin filtering)
    /// </summary>
    public async Task<PagedResponse<JobListItem>> GetUserAssignedJobsAsync(UserJobsRequestVM request, Guid userId, Guid companyId)
    {
        if (request.pageNumber <= 0) request.pageNumber = 1;
        if (request.pageSize <= 0 || request.pageSize > 100) request.pageSize = 50;

        var query = _ctx.JobAssignments
            .Include(ja => ja.job)
                .ThenInclude(j => j!.client)
            .Include(ja => ja.job)
                .ThenInclude(j => j!.location)
            .Where(ja => ja.userId == userId && ja.companyId == companyId && ja.active && ja.job != null);

        // Filter by status
        if (!string.IsNullOrWhiteSpace(request.status))
        {
            query = query.Where(ja => ja.job!.status == request.status);
        }

        // Search by title
        if (!string.IsNullOrWhiteSpace(request.searchTerm))
        {
            var term = request.searchTerm.ToLower();
            query = query.Where(ja => 
                ja.job!.title.ToLower().Contains(term) || 
                (ja.job.client != null && ja.job.client.companyName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(ja => ja.job!.createDatetime)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(ja => new JobListItem
            {
                id = ja.job!.id,
                companyId = ja.job.companyId,
                title = ja.job.title,
                status = ja.job.status,
                clientName = ja.job.client != null 
                    ? (!string.IsNullOrEmpty(ja.job.client.companyName) ? ja.job.client.companyName : ja.job.client.contactName)
                    : string.Empty,
                locationCity = ja.job.location != null ? ja.job.location.city : string.Empty,
                createDatetime = ja.job.createDatetime
            })
            .ToListAsync();

        return new PagedResponse<JobListItem>
        {
            Items = items,
            PageNumber = request.pageNumber,
            PageSize = request.pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.pageSize)
        };
    }

    /// <summary>
    /// Check if a user is assigned to a job
    /// </summary>
    public async Task<bool> IsUserAssignedToJobAsync(Guid userId, Guid jobId, Guid companyId)
    {
        return await _ctx.JobAssignments
            .AnyAsync(ja => ja.userId == userId && ja.jobId == jobId && ja.companyId == companyId && ja.active);
    }

    /// <summary>
    /// Get all assignments with pagination
    /// </summary>
    public async Task<PagedResponse<JobAssignmentListItemRM>> GetAssignmentsAsync(JobAssignmentListRequestVM request, Guid companyId)
    {
        if (request.pageNumber <= 0) request.pageNumber = 1;
        if (request.pageSize <= 0 || request.pageSize > 100) request.pageSize = 50;

        var query = _ctx.JobAssignments
            .Include(ja => ja.job)
            .Include(ja => ja.user)
            .Where(ja => ja.companyId == companyId);

        if (request.jobId.HasValue)
        {
            query = query.Where(ja => ja.jobId == request.jobId.Value);
        }

        if (request.userId.HasValue)
        {
            query = query.Where(ja => ja.userId == request.userId.Value);
        }

        if (request.active.HasValue)
        {
            query = query.Where(ja => ja.active == request.active.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(ja => ja.assignedDatetime)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(ja => new JobAssignmentListItemRM
            {
                id = ja.id,
                jobId = ja.jobId,
                jobTitle = ja.job != null ? ja.job.title : string.Empty,
                userId = ja.userId,
                userName = ja.user != null ? ja.user.fullName : string.Empty,
                userPhotoThumbnail = ja.user != null ? ja.user.personalPhotoThumbnail : null,
                role = ja.role,
                active = ja.active,
                assignedDatetime = ja.assignedDatetime
            })
            .ToListAsync();

        return new PagedResponse<JobAssignmentListItemRM>
        {
            Items = items,
            PageNumber = request.pageNumber,
            PageSize = request.pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.pageSize)
        };
    }
}

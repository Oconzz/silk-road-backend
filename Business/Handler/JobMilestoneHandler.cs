using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers;

public class JobMilestoneHandler
{
    private readonly DBContext _context;

    public JobMilestoneHandler(DBContext context)
    {
        _context = context;
    }

    public async Task<List<JobMilestoneRM>> GetJobMilestoneListAsync(
        JobMilestoneListRequestVM request, 
        Guid companyId)
    {
        var query = _context.JobMilestones
            .Where(m => m.companyId == companyId && m.jobId == request.jobId)
            .AsQueryable();

        if (request.completed.HasValue)
        {
            query = query.Where(m => m.completed == request.completed.Value);
        }

        var milestones = await query
            .OrderBy(m => m.orderIndex)
            .ThenBy(m => m.createDatetime)
            .Include(m => m.completedByUser)
            .Select(m => new JobMilestoneRM
            {
                id = m.id,
                jobId = m.jobId,
                companyId = m.companyId,
                title = m.title,
                description = m.description,
                completed = m.completed,
                completedDate = m.completedDate,
                completedBy = m.completedBy,
                completedByName = m.completedByUser != null ? m.completedByUser.fullName : null,
                orderIndex = m.orderIndex,
                createDatetime = m.createDatetime,
                updateDatetime = m.updateDatetime
            })
            .ToListAsync();

        return milestones;
    }

    public async Task<JobMilestoneRM?> GetJobMilestoneByIdAsync(Guid id, Guid companyId)
    {
        var milestone = await _context.JobMilestones
            .Where(m => m.id == id && m.companyId == companyId)
            .Include(m => m.completedByUser)
            .FirstOrDefaultAsync();

        if (milestone == null) return null;

        return new JobMilestoneRM
        {
            id = milestone.id,
            jobId = milestone.jobId,
            companyId = milestone.companyId,
            title = milestone.title,
            description = milestone.description,
            completed = milestone.completed,
            completedDate = milestone.completedDate,
            completedBy = milestone.completedBy,
            completedByName = milestone.completedByUser?.fullName,
            orderIndex = milestone.orderIndex,
            createDatetime = milestone.createDatetime,
            updateDatetime = milestone.updateDatetime
        };
    }

    public async Task<JobMilestoneRM> CreateJobMilestoneAsync(
        CreateJobMilestoneVM request, 
        Guid companyId)
    {
        var milestone = new JobMilestone
        {
            id = Guid.NewGuid(),
            jobId = request.jobId,
            companyId = companyId,
            title = request.title,
            description = request.description,
            completed = false,
            orderIndex = request.orderIndex,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.JobMilestones.Add(milestone);
        await _context.SaveChangesAsync();

        return await GetJobMilestoneByIdAsync(milestone.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created milestone");
    }

    public async Task<JobMilestoneRM?> UpdateJobMilestoneAsync(
        UpdateJobMilestoneVM request, 
        Guid companyId)
    {
        var milestone = await _context.JobMilestones
            .FirstOrDefaultAsync(m => m.id == request.id && m.companyId == companyId);

        if (milestone == null) return null;

        if (!string.IsNullOrEmpty(request.title))
            milestone.title = request.title;

        if (request.description != null)
            milestone.description = request.description;

        if (request.completed.HasValue)
            milestone.completed = request.completed.Value;

        if (request.orderIndex.HasValue)
            milestone.orderIndex = request.orderIndex.Value;

        milestone.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobMilestoneByIdAsync(milestone.id, companyId);
    }

    public async Task<JobMilestoneRM?> CompleteJobMilestoneAsync(
        CompleteJobMilestoneVM request, 
        Guid companyId, 
        Guid userId)
    {
        var milestone = await _context.JobMilestones
            .FirstOrDefaultAsync(m => m.id == request.id && m.jobId == request.jobId && m.companyId == companyId);

        if (milestone == null) return null;

        milestone.completed = true;
        milestone.completedDate = DateTime.UtcNow;
        milestone.completedBy = userId;
        milestone.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobMilestoneByIdAsync(milestone.id, companyId);
    }

    public async Task<bool> DeleteJobMilestoneAsync(Guid id, Guid companyId)
    {
        var milestone = await _context.JobMilestones
            .FirstOrDefaultAsync(m => m.id == id && m.companyId == companyId);

        if (milestone == null) return false;

        _context.JobMilestones.Remove(milestone);
        await _context.SaveChangesAsync();

        return true;
    }
}

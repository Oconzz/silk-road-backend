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

public class JobNoteHandler
{
    private readonly DBContext _context;

    public JobNoteHandler(DBContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<JobNoteListItem>> GetJobNoteListAsync(
        JobNoteListRequestVM request, 
        Guid companyId)
    {
        var query = _context.JobNotes
            .Where(n => n.companyId == companyId && n.jobId == request.jobId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.type))
        {
            query = query.Where(n => n.type == request.type);
        }

        var totalCount = await query.CountAsync();

        var notes = await query
            .OrderByDescending(n => n.createDatetime)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Include(n => n.creator)
            .Select(n => new JobNoteListItem
            {
                id = n.id,
                jobId = n.jobId,
                createdByName = n.creator != null ? n.creator.fullName : "Unknown",
                content = n.content,
                type = n.type,
                createDatetime = n.createDatetime
            })
            .ToListAsync();

        return new PagedResponse<JobNoteListItem>
        {
            Items = notes,
            TotalCount = totalCount,
            PageNumber = request.pageNumber,
            PageSize = request.pageSize
        };
    }

    public async Task<JobNoteRM?> GetJobNoteByIdAsync(Guid id, Guid companyId)
    {
        var note = await _context.JobNotes
            .Where(n => n.id == id && n.companyId == companyId)
            .Include(n => n.creator)
            .FirstOrDefaultAsync();

        if (note == null) return null;

        return new JobNoteRM
        {
            id = note.id,
            jobId = note.jobId,
            companyId = note.companyId,
            createdBy = note.createdBy,
            createdByName = note.creator?.fullName ?? "Unknown",
            content = note.content,
            type = note.type,
            createDatetime = note.createDatetime,
            updateDatetime = note.updateDatetime
        };
    }

    public async Task<JobNoteRM> CreateJobNoteAsync(
        CreateJobNoteVM request, 
        Guid companyId, 
        Guid userId)
    {
        var note = new JobNote
        {
            id = Guid.NewGuid(),
            jobId = request.jobId,
            companyId = companyId,
            createdBy = userId,
            content = request.content,
            type = request.type,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.JobNotes.Add(note);
        await _context.SaveChangesAsync();

        return await GetJobNoteByIdAsync(note.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created note");
    }

    public async Task<JobNoteRM?> UpdateJobNoteAsync(
        UpdateJobNoteVM request, 
        Guid companyId)
    {
        var note = await _context.JobNotes
            .FirstOrDefaultAsync(n => n.id == request.id && n.companyId == companyId);

        if (note == null) return null;

        note.content = request.content;
        note.type = request.type;
        note.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobNoteByIdAsync(note.id, companyId);
    }

    public async Task<bool> DeleteJobNoteAsync(Guid id, Guid companyId)
    {
        var note = await _context.JobNotes
            .FirstOrDefaultAsync(n => n.id == id && n.companyId == companyId);

        if (note == null) return false;

        _context.JobNotes.Remove(note);
        await _context.SaveChangesAsync();

        return true;
    }
}

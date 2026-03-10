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

public class ClientNoteHandler
{
    private readonly DBContext _context;

    public ClientNoteHandler(DBContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ClientNoteListItem>> GetClientNoteListAsync(
        ClientNoteListRequestVM request, 
        Guid companyId)
    {
        var query = _context.ClientNotes
            .Where(n => n.companyId == companyId && n.clientId == request.clientId)
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
            .Select(n => new ClientNoteListItem
            {
                id = n.id,
                clientId = n.clientId,
                createdByName = n.creator != null ? n.creator.fullName : "Unknown",
                content = n.content,
                type = n.type,
                createDatetime = n.createDatetime
            })
            .ToListAsync();

        return new PagedResponse<ClientNoteListItem>
        {
            Items = notes,
            TotalCount = totalCount,
            PageNumber = request.pageNumber,
            PageSize = request.pageSize
        };
    }

    public async Task<ClientNoteRM?> GetClientNoteByIdAsync(Guid id, Guid companyId)
    {
        var note = await _context.ClientNotes
            .Where(n => n.id == id && n.companyId == companyId)
            .Include(n => n.creator)
            .FirstOrDefaultAsync();

        if (note == null) return null;

        return new ClientNoteRM
        {
            id = note.id,
            clientId = note.clientId,
            companyId = note.companyId,
            createdBy = note.createdBy,
            createdByName = note.creator?.fullName ?? "Unknown",
            content = note.content,
            type = note.type,
            createDatetime = note.createDatetime,
            updateDatetime = note.updateDatetime
        };
    }

    public async Task<ClientNoteRM> CreateClientNoteAsync(
        CreateClientNoteVM request, 
        Guid companyId, 
        Guid userId)
    {
        var note = new ClientNote
        {
            id = Guid.NewGuid(),
            clientId = request.clientId,
            companyId = companyId,
            createdBy = userId,
            content = request.content,
            type = request.type,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.ClientNotes.Add(note);
        await _context.SaveChangesAsync();

        return await GetClientNoteByIdAsync(note.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created note");
    }

    public async Task<ClientNoteRM?> UpdateClientNoteAsync(
        UpdateClientNoteVM request, 
        Guid companyId)
    {
        var note = await _context.ClientNotes
            .FirstOrDefaultAsync(n => n.id == request.id && n.companyId == companyId);

        if (note == null) return null;

        note.content = request.content;
        note.type = request.type;
        note.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetClientNoteByIdAsync(note.id, companyId);
    }

    public async Task<bool> DeleteClientNoteAsync(Guid id, Guid companyId)
    {
        var note = await _context.ClientNotes
            .FirstOrDefaultAsync(n => n.id == id && n.companyId == companyId);

        if (note == null) return false;

        _context.ClientNotes.Remove(note);
        await _context.SaveChangesAsync();

        return true;
    }
}

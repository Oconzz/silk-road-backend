using Business.Data;
using Business.Model;
using Business.Response;
using Business.Services;
using Business.Utilities;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers;

public class JobMediaHandler
{
    private readonly DBContext _context;
    private readonly IPhotoService _photoService;
    private readonly AwsSettings _awsSettings;

    public JobMediaHandler(DBContext context, IPhotoService photoService, IOptions<AppSettings> appSettings)
    {
        _context = context;
        _photoService = photoService;
        _awsSettings = appSettings.Value.AwsSettings ?? new AwsSettings();
    }

    public async Task<List<JobMediaRM>> GetJobMediaListAsync(
        JobMediaListRequestVM request, 
        Guid companyId)
    {
        var query = _context.JobMedia
            .Where(m => m.companyId == companyId && m.jobId == request.jobId)
            .AsQueryable();

        if (request.albumId.HasValue)
        {
            query = query.Where(m => m.albumId == request.albumId.Value);
        }

        if (!string.IsNullOrEmpty(request.category))
        {
            query = query.Where(m => m.category == request.category);
        }

        var media = await query
            .OrderBy(m => m.orderIndex)
            .ThenBy(m => m.createDatetime)
            .Include(m => m.album)
            .Select(m => new JobMediaRM
            {
                id = m.id,
                jobId = m.jobId,
                companyId = m.companyId,
                albumId = m.albumId,
                albumName = m.album != null ? m.album.name : null,
                webUrl = m.webUrl,
                thumbUrl = m.thumbUrl,
                caption = m.caption,
                type = m.type,
                category = m.category,
                orderIndex = m.orderIndex,
                createDatetime = m.createDatetime,
                updateDatetime = m.updateDatetime
            })
            .ToListAsync();

        return media;
    }

    public async Task<JobMediaRM> CreateJobMediaAsync(
        CreateJobMediaVM request, 
        Guid companyId)
    {
        string webUrl, thumbUrl;

        if (!string.IsNullOrEmpty(request.s3Key))
        {
            // Direct S3 Upload flow
            var region = _awsSettings.S3Region ?? "us-east-2"; 
            var bucket = _awsSettings.S3PrivateBucket ?? "nmj-dev-gallery"; 
            
            webUrl = $"https://{bucket}.s3.{region}.amazonaws.com/{request.s3Key}";
            thumbUrl = webUrl; 
        }
        else
        {
            // Legacy Base64 Flow
            var keyPrefix = $"jobs/{request.jobId}";
            var result = await _photoService.ProcessAndUploadAsync(request.base64Image, keyPrefix);
            webUrl = result.webUrl!;
            thumbUrl = result.thumbUrl!;
        }

        if (string.IsNullOrEmpty(webUrl))
        {
            throw new InvalidOperationException("Failed to upload image or invalid S3 key");
        }

        var media = new JobMedia
        {
            id = Guid.NewGuid(),
            jobId = request.jobId,
            companyId = companyId,
            albumId = request.albumId,
            webUrl = webUrl,
            thumbUrl = thumbUrl,
            caption = request.caption,
            type = request.type,
            category = request.category,
            orderIndex = request.orderIndex,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.JobMedia.Add(media);
        await _context.SaveChangesAsync();

        return await GetJobMediaByIdAsync(media.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created media");
    }

    public async Task<JobMediaRM?> GetJobMediaByIdAsync(Guid id, Guid companyId)
    {
        var media = await _context.JobMedia
            .Where(m => m.id == id && m.companyId == companyId)
            .Include(m => m.album)
            .FirstOrDefaultAsync();

        if (media == null) return null;

        return new JobMediaRM
        {
            id = media.id,
            jobId = media.jobId,
            companyId = media.companyId,
            albumId = media.albumId,
            albumName = media.album?.name,
            webUrl = media.webUrl,
            thumbUrl = media.thumbUrl,
            caption = media.caption,
            type = media.type,
            category = media.category,
            orderIndex = media.orderIndex,
            createDatetime = media.createDatetime,
            updateDatetime = media.updateDatetime
        };
    }

    public async Task<JobMediaRM?> UpdateJobMediaAsync(
        UpdateJobMediaVM request, 
        Guid companyId)
    {
        var media = await _context.JobMedia
            .FirstOrDefaultAsync(m => m.id == request.id && m.companyId == companyId);

        if (media == null) return null;

        if (request.albumId.HasValue)
            media.albumId = request.albumId;

        if (request.caption != null)
            media.caption = request.caption;

        if (request.category != null)
            media.category = request.category;

        if (request.orderIndex.HasValue)
            media.orderIndex = request.orderIndex.Value;

        media.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetJobMediaByIdAsync(media.id, companyId);
    }

    public async Task<bool> DeleteJobMediaAsync(Guid id, Guid companyId)
    {
        var media = await _context.JobMedia
            .FirstOrDefaultAsync(m => m.id == id && m.companyId == companyId);

        if (media == null) return false;

        _context.JobMedia.Remove(media);
        await _context.SaveChangesAsync();

        return true;
    }

    // Album methods
    public async Task<List<JobAlbumRM>> GetJobAlbumsAsync(Guid jobId, Guid companyId)
    {
        var albums = await _context.JobAlbums
            .Where(a => a.jobId == jobId && a.companyId == companyId)
            .OrderBy(a => a.orderIndex)
            .Include(a => a.media)
            .Select(a => new JobAlbumRM
            {
                id = a.id,
                jobId = a.jobId,
                companyId = a.companyId,
                name = a.name,
                description = a.description,
                orderIndex = a.orderIndex,
                mediaCount = a.media.Count,
                media = a.media.Select(m => new JobMediaRM
                {
                    id = m.id,
                    jobId = m.jobId,
                    companyId = m.companyId,
                    albumId = m.albumId,
                    webUrl = m.webUrl,
                    thumbUrl = m.thumbUrl,
                    caption = m.caption,
                    type = m.type,
                    category = m.category,
                    orderIndex = m.orderIndex,
                    createDatetime = m.createDatetime,
                    updateDatetime = m.updateDatetime
                }).ToList(),
                createDatetime = a.createDatetime,
                updateDatetime = a.updateDatetime
            })
            .ToListAsync();

        return albums;
    }

    public async Task<JobAlbumRM> CreateJobAlbumAsync(
        CreateJobAlbumVM request, 
        Guid companyId)
    {
        var album = new JobAlbum
        {
            id = Guid.NewGuid(),
            jobId = request.jobId,
            companyId = companyId,
            name = request.name,
            description = request.description,
            orderIndex = request.orderIndex,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.JobAlbums.Add(album);
        await _context.SaveChangesAsync();

        return new JobAlbumRM
        {
            id = album.id,
            jobId = album.jobId,
            companyId = album.companyId,
            name = album.name,
            description = album.description,
            orderIndex = album.orderIndex,
            mediaCount = 0,
            media = new List<JobMediaRM>(),
            createDatetime = album.createDatetime,
            updateDatetime = album.updateDatetime
        };
    }

    public async Task<bool> DeleteJobAlbumAsync(Guid id, Guid companyId)
    {
        var album = await _context.JobAlbums
            .FirstOrDefaultAsync(a => a.id == id && a.companyId == companyId);

        if (album == null) return false;

        _context.JobAlbums.Remove(album);
        await _context.SaveChangesAsync();

        return true;
    }
}

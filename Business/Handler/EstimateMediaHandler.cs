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

public class EstimateMediaHandler
{
    private readonly DBContext _context;
    private readonly IPhotoService _photoService;
    private readonly AwsSettings _awsSettings;

    public EstimateMediaHandler(DBContext context, IPhotoService photoService, IOptions<AppSettings> appSettings)
    {
        _context = context;
        _photoService = photoService;
        _awsSettings = appSettings.Value.AwsSettings ?? new AwsSettings();
    }

    public async Task<List<EstimateMediaRM>> GetEstimateMediaListAsync(
        EstimateMediaListRequestVM request, 
        Guid companyId)
    {
        var query = _context.EstimateMedia
            .Where(m => m.companyId == companyId && m.estimateId == request.estimateId)
            .AsQueryable();

        if (request.albumId.HasValue)
        {
            query = query.Where(m => m.albumId == request.albumId.Value);
        }

        var media = await query
            .OrderBy(m => m.orderIndex)
            .ThenBy(m => m.createDatetime)
            .Include(m => m.album)
            .Select(m => new EstimateMediaRM
            {
                id = m.id,
                estimateId = m.estimateId,
                companyId = m.companyId,
                albumId = m.albumId,
                albumName = m.album != null ? m.album.name : null,
                estimateItemId = m.estimateItemId,
                estimateItemTitle = m.estimateItem != null ? m.estimateItem.title : null,
                webUrl = m.webUrl,
                thumbUrl = m.thumbUrl,
                caption = m.caption,
                type = m.type,
                orderIndex = m.orderIndex,
                createDatetime = m.createDatetime,
                updateDatetime = m.updateDatetime
            })
            .ToListAsync();

        return media;
    }

    public async Task<EstimateMediaRM> CreateEstimateMediaAsync(
        CreateEstimateMediaVM request, 
        Guid companyId)
    {
        string webUrl, thumbUrl;

        if (!string.IsNullOrEmpty(request.s3Key))
        {
            // Direct S3 Upload flow - construct URL from config
            var region = _awsSettings.S3Region ?? "us-east-2";
            var bucket = _awsSettings.S3PrivateBucket ?? "nmj-dev-gallery";
            
            webUrl = $"https://{bucket}.s3.{region}.amazonaws.com/{request.s3Key}";
            thumbUrl = webUrl; // Re-use full image as thumb for now if no separate thumb uploaded
        }
        else
        {
            // Legacy Base64 Flow
            var keyPrefix = $"estimates/{request.estimateId}";
            var result = await _photoService.ProcessAndUploadAsync(request.base64Image, keyPrefix);
            webUrl = result.webUrl!;
            thumbUrl = result.thumbUrl!;
        }

        if (string.IsNullOrEmpty(webUrl))
        {
            throw new InvalidOperationException("Failed to upload image or invalid S3 key");
        }

        var media = new EstimateMedia
        {
            id = Guid.NewGuid(),
            estimateId = request.estimateId,
            companyId = companyId,
            albumId = request.albumId,
            webUrl = webUrl,
            thumbUrl = thumbUrl,
            caption = request.caption,
            type = request.type,
            orderIndex = request.orderIndex,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.EstimateMedia.Add(media);
        await _context.SaveChangesAsync();

        return await GetEstimateMediaByIdAsync(media.id, companyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created media");
    }

    public async Task<EstimateMediaRM?> GetEstimateMediaByIdAsync(Guid id, Guid companyId)
    {
        var media = await _context.EstimateMedia
            .Where(m => m.id == id && m.companyId == companyId)
            .Include(m => m.album)
            .FirstOrDefaultAsync();

        if (media == null) return null;

        return new EstimateMediaRM
        {
            id = media.id,
            estimateId = media.estimateId,
            companyId = media.companyId,
            albumId = media.albumId,
            albumName = media.album?.name,
            estimateItemId = media.estimateItemId,
            estimateItemTitle = media.estimateItem?.title,
            webUrl = media.webUrl,
            thumbUrl = media.thumbUrl,
            caption = media.caption,
            type = media.type,
            orderIndex = media.orderIndex,
            createDatetime = media.createDatetime,
            updateDatetime = media.updateDatetime
        };
    }

    public async Task<EstimateMediaRM?> UpdateEstimateMediaAsync(
        UpdateEstimateMediaVM request, 
        Guid companyId)
    {
        var media = await _context.EstimateMedia
            .FirstOrDefaultAsync(m => m.id == request.id && m.companyId == companyId);

        if (media == null) return null;

        if (request.albumId.HasValue)
            media.albumId = request.albumId;

        if (request.caption != null)
            media.caption = request.caption;

        if (request.orderIndex.HasValue)
            media.orderIndex = request.orderIndex.Value;

        media.updateDatetime = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetEstimateMediaByIdAsync(media.id, companyId);
    }

    public async Task<bool> DeleteEstimateMediaAsync(Guid id, Guid companyId)
    {
        var media = await _context.EstimateMedia
            .FirstOrDefaultAsync(m => m.id == id && m.companyId == companyId);

        if (media == null) return false;

        _context.EstimateMedia.Remove(media);
        await _context.SaveChangesAsync();

        return true;
    }

    // Album methods
    public async Task<List<EstimateAlbumRM>> GetEstimateAlbumsAsync(Guid estimateId, Guid companyId)
    {
        var albums = await _context.EstimateAlbums
            .Where(a => a.estimateId == estimateId && a.companyId == companyId)
            .OrderBy(a => a.orderIndex)
            .Include(a => a.media)
            .Select(a => new EstimateAlbumRM
            {
                id = a.id,
                estimateId = a.estimateId,
                companyId = a.companyId,
                name = a.name,
                description = a.description,
                orderIndex = a.orderIndex,
                mediaCount = a.media.Count,
                media = a.media.Select(m => new EstimateMediaRM
                {
                    id = m.id,
                    estimateId = m.estimateId,
                    companyId = m.companyId,
                    albumId = m.albumId,
                    webUrl = m.webUrl,
                    thumbUrl = m.thumbUrl,
                    caption = m.caption,
                    type = m.type,
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

    public async Task<EstimateAlbumRM> CreateEstimateAlbumAsync(
        CreateEstimateAlbumVM request, 
        Guid companyId)
    {
        var album = new EstimateAlbum
        {
            id = Guid.NewGuid(),
            estimateId = request.estimateId,
            companyId = companyId,
            name = request.name,
            description = request.description,
            orderIndex = request.orderIndex,
            createDatetime = DateTime.UtcNow,
            updateDatetime = DateTime.UtcNow
        };

        _context.EstimateAlbums.Add(album);
        await _context.SaveChangesAsync();

        return new EstimateAlbumRM
        {
            id = album.id,
            estimateId = album.estimateId,
            companyId = album.companyId,
            name = album.name,
            description = album.description,
            orderIndex = album.orderIndex,
            mediaCount = 0,
            media = new List<EstimateMediaRM>(),
            createDatetime = album.createDatetime,
            updateDatetime = album.updateDatetime
        };
    }

    public async Task<bool> DeleteEstimateAlbumAsync(Guid id, Guid companyId)
    {
        var album = await _context.EstimateAlbums
            .FirstOrDefaultAsync(a => a.id == id && a.companyId == companyId);

        if (album == null) return false;

        _context.EstimateAlbums.Remove(album);
        await _context.SaveChangesAsync();

        return true;
    }
}

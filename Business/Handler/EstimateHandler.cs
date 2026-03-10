using AutoMapper;
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

namespace Business.Handlers
{
    public class EstimateHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;
        private readonly IS3PresignedService? _s3PresignedService;
        private readonly IGoogleCalendarService? _googleCalendarService;

        public EstimateHandler(DBContext ctx, IMapper mapper, IS3PresignedService? s3PresignedService = null, IGoogleCalendarService? googleCalendarService = null)
        {
            _ctx = ctx;
            _mapper = mapper;
            _s3PresignedService = s3PresignedService;
            _googleCalendarService = googleCalendarService;
        }

        public async Task<PagedResponse<EstimateListItem>> GetEstimateListAsync(EstimateListRequestVM request, Guid companyId)
        {
            if (request == null) request = new EstimateListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Estimates> query = _ctx.Estimates
                .AsNoTracking()
                .Include(e => e.client)
                .Include(e => e.location)
                .Where(e => e.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(e =>
                    (e.name != null && e.name.ToLower().Contains(term)) ||
                    (e.description != null && e.description.ToLower().Contains(term))
                );
            }

            // Filter by sent status
            if (request.sent.HasValue)
            {
                query = query.Where(e => e.sent == request.sent.Value);
            }

            // Filter by approved status
            if (request.approved.HasValue)
            {
                query = query.Where(e => e.approved == request.approved.Value);
            }

            // Filter by client
            if (request.clientId.HasValue)
            {
                query = query.Where(e => e.client != null && e.client.id == request.clientId.Value);
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(e => e.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(e => new EstimateListItem
                {
                    id = e.id,
                    companyId = e.companyId,
                    name = e.name,
                    clientId = e.client != null ? e.client.id : (Guid?)null,
                    clientName = e.client != null 
                        ? (!string.IsNullOrEmpty(e.client.companyName) ? e.client.companyName : e.client.contactName)
                        : string.Empty,
                    locationCity = e.location != null ? e.location.city : string.Empty,
                    total = e.total,
                    sent = e.sent,
                    approved = e.approved,
                    createDatetime = e.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<EstimateListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<EstimateRM?> GetEstimateByIdAsync(Guid id, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .AsNoTracking()
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == id && e.companyId == companyId);
            
            // Load media and albums separately to avoid complex query
            var media = await _ctx.EstimateMedia
                .AsNoTracking()
                .Where(m => m.estimateId == id && m.companyId == companyId)
                .OrderBy(m => m.orderIndex)
                .ToListAsync();
            
            var albums = await _ctx.EstimateAlbums
                .AsNoTracking()
                .Where(a => a.estimateId == id && a.companyId == companyId)
                .OrderBy(a => a.orderIndex)
                .ToListAsync();

            if (estimate == null)
                return null;

            var result = new EstimateRM
            {
                id = estimate.id,
                companyId = estimate.companyId,
                name = estimate.name,
                description = estimate.description,
                total = estimate.total,
                sent = estimate.sent,
                approved = estimate.approved,
                sentDateTime = estimate.sentDateTime,
                createDatetime = estimate.createDatetime,
                updateDatetime = estimate.updateDatetime,
                termsAndConditions = estimate.termsAndConditions,
                proposedStartDate = estimate.proposedStartDate,
                expirationDate = estimate.expirationDate,
                publicShareId = estimate.publicShareId,
                publicShareUrl = !string.IsNullOrEmpty(estimate.publicShareId)
                    ? $"/Estimates/view/{estimate.publicShareId}"
                    : null,
                viewCount = estimate.viewCount,
                lastViewedAt = estimate.lastViewedAt,
                version = estimate.version,
                parentEstimateId = estimate.parentEstimateId,
                isLatestVersion = estimate.isLatestVersion
            };

            // Map client
            if (estimate.client != null)
            {
                result.client = new ClientListItem
                {
                    id = estimate.client.id,
                    companyId = estimate.client.companyId,
                    contactName = estimate.client.contactName,
                    phone = estimate.client.phone,
                    email = estimate.client.email,
                    companyName = estimate.client.companyName,
                    createDatetime = estimate.client.createDatetime
                };
            }

            // Map location
            if (estimate.location != null)
            {
                result.location = _mapper.Map<Location, LocationRM>(estimate.location);
            }

            // Map items and compute mediaIds for each item
            if (estimate.items != null && estimate.items.Any())
            {
                result.items = estimate.items.Select(i => {
                    var itemRM = _mapper.Map<EstimateItem, EstimateItemRM>(i);
                    // Find media linked to this item
                    if (media != null)
                    {
                        var linkedMediaIds = media.Where(m => m.estimateItemId == i.id).Select(m => m.id).ToList();
                        if (linkedMediaIds.Any())
                            itemRM.mediaIds = linkedMediaIds;
                    }
                    return itemRM;
                }).ToList();
            }

            // Map media - generate presigned URLs for private bucket access
            if (media != null && media.Any())
            {
                var mediaTasks = media.Select(async m =>
                {
                    // Find the item title for this media
                    string? itemTitle = null;
                    if (m.estimateItemId.HasValue && estimate.items != null)
                    {
                        itemTitle = estimate.items.FirstOrDefault(i => i.id == m.estimateItemId)?.title;
                    }

                    var mediaRM = new EstimateMediaRM
                    {
                        id = m.id,
                        estimateId = m.estimateId,
                        companyId = m.companyId,
                        albumId = m.albumId,
                        estimateItemId = m.estimateItemId,
                        estimateItemTitle = itemTitle,
                        webUrl = m.webUrl,
                        thumbUrl = m.thumbUrl,
                        caption = m.caption,
                        type = m.type,
                        orderIndex = m.orderIndex,
                        createDatetime = m.createDatetime,
                        updateDatetime = m.updateDatetime
                    };

                    // Generate presigned URLs if S3 service is available
                    if (_s3PresignedService != null && !string.IsNullOrEmpty(m.webUrl))
                    {
                        // Extract s3Key from the stored URL
                        var s3Key = ExtractS3KeyFromUrl(m.webUrl);
                        if (!string.IsNullOrEmpty(s3Key))
                        {
                            try
                            {
                                var (webPresigned, thumbPresigned) = await _s3PresignedService.GeneratePresignedDownloadUrls(s3Key);
                                mediaRM.webUrl = webPresigned;
                                mediaRM.thumbUrl = thumbPresigned;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[EstimateHandler] Error generating presigned URL for {s3Key}: {ex.Message}");
                            }
                        }
                    }

                    return mediaRM;
                });

                result.media = (await Task.WhenAll(mediaTasks)).ToList();
            }

            // Map albums
            if (albums != null && albums.Any())
            {
                result.albums = albums.Select(a => new EstimateAlbumRM
                {
                    id = a.id,
                    estimateId = a.estimateId,
                    companyId = a.companyId,
                    name = a.name,
                    description = a.description,
                    orderIndex = a.orderIndex,
                    mediaCount = media?.Count(m => m.albumId == a.id) ?? 0,
                    media = media?.Where(m => m.albumId == a.id).Select(m => new EstimateMediaRM
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
                    }).ToList() ?? new List<EstimateMediaRM>(),
                    createDatetime = a.createDatetime,
                    updateDatetime = a.updateDatetime
                }).ToList();
            }

            return result;
        }

        public async Task<EstimateRM> CreateEstimateAsync(CreateEstimateVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            var estimate = new Estimates
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = vm.name,
                description = vm.description,
                total = 0,
                sent = false,
                approved = false,
                sentDateTime = DateTime.MinValue,
                proposedStartDate = vm.proposedStartDate,
                expirationDate = vm.expirationDate,
                createDatetime = now,
                updateDatetime = now,
                termsAndConditions = vm.termsAndConditions
            };

            // Link client if provided
            if (vm.clientId.HasValue)
            {
                var client = await _ctx.Client.FindAsync(vm.clientId.Value);
                if (client != null && client.companyId == companyId)
                {
                    estimate.client = client;
                }
            }

            // Link location if provided
            if (vm.locationId.HasValue)
            {
                var location = await _ctx.Location.FindAsync(vm.locationId.Value);
                if (location != null && location.companyId == companyId)
                {
                    estimate.location = location;
                }
            }

            _ctx.Estimates.Add(estimate);
            await _ctx.SaveChangesAsync();  // Salvar para ter o estimate.id

            // Link items if provided - add directly to DbSet to avoid tracking issues
            if (vm.items != null && vm.items.Any())
            {
                var estimateItems = vm.items.Select(itemVM => new EstimateItem
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    estimateId = estimate.id,  // FK para Estimate
                    productId = itemVM.productId,
                    warehouseId = itemVM.warehouseId,
                    inventoryId = itemVM.inventoryId,
                    quantity = itemVM.quantity,
                    price = itemVM.price,
                    labor = itemVM.labor,
                    title = itemVM.title,
                    description = itemVM.description ?? string.Empty,
                    barcode = itemVM.barcode ?? string.Empty,
                    source = itemVM.source ?? string.Empty,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();

                // Add directly to DbSet instead of collection to avoid concurrency issues
                await _ctx.EstimateItem.AddRangeAsync(estimateItems);
                estimate.total = estimateItems.Sum(i => (i.price + i.labor) * i.quantity);
                await _ctx.SaveChangesAsync();

                // Update media-to-item associations
                for (int idx = 0; idx < vm.items.Count; idx++)
                {
                    var itemVM = vm.items[idx];
                    if (itemVM.mediaIds != null && itemVM.mediaIds.Any())
                    {
                        var newItemId = estimateItems[idx].id;
                        var mediaToLink = await _ctx.EstimateMedia
                            .Where(m => itemVM.mediaIds.Contains(m.id) && m.estimateId == estimate.id)
                            .ToListAsync();
                        foreach (var media in mediaToLink)
                        {
                            media.estimateItemId = newItemId;
                        }
                    }
                }
                await _ctx.SaveChangesAsync();
            }

            // Reload with relationships
            return await GetEstimateByIdAsync(estimate.id, companyId) 
                ?? throw new InvalidOperationException("Failed to retrieve created estimate");
        }

        public async Task<EstimateRM?> UpdateEstimateAsync(UpdateEstimateVM vm, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == vm.id && e.companyId == companyId);

            if (estimate == null)
                return null;

            if (!string.IsNullOrEmpty(vm.name))
                estimate.name = vm.name;
            if (vm.description != null)
                estimate.description = vm.description;
            if (vm.termsAndConditions != null)
                estimate.termsAndConditions = vm.termsAndConditions;
            if (vm.sent.HasValue)
            {
                estimate.sent = vm.sent.Value;
                if (vm.sent.Value && estimate.sentDateTime == DateTime.MinValue)
                {
                    estimate.sentDateTime = DateTime.UtcNow;
                }
            }
            if (vm.approved.HasValue)
                estimate.approved = vm.approved.Value;
            if (vm.proposedStartDate.HasValue)
                estimate.proposedStartDate = vm.proposedStartDate.Value;
            if (vm.expirationDate.HasValue)
                estimate.expirationDate = vm.expirationDate.Value;

            // Update client relationship
            if (vm.clientId.HasValue)
            {
                var client = await _ctx.Client.FindAsync(vm.clientId.Value);
                if (client != null && client.companyId == companyId)
                {
                    estimate.client = client;
                }
            }

            // Update location relationship
            if (vm.locationId.HasValue)
            {
                var location = await _ctx.Location.FindAsync(vm.locationId.Value);
                if (location != null && location.companyId == companyId)
                {
                    estimate.location = location;
                }
            }

            // Update items if provided
            if (vm.items != null)
            {
                var now = DateTime.UtcNow;

                // Clear existing media-item associations before deleting items
                var existingMediaWithItems = await _ctx.EstimateMedia
                    .Where(m => m.estimateId == estimate.id && m.estimateItemId != null)
                    .ToListAsync();
                foreach (var media in existingMediaWithItems)
                {
                    media.estimateItemId = null;
                }
                
                // Remove existing items directly from DbSet
                if (estimate.items != null && estimate.items.Any())
                {
                    _ctx.EstimateItem.RemoveRange(estimate.items);
                }
                
                // Save removal first to avoid conflicts
                estimate.updateDatetime = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
                
                // Create and add new items directly to DbSet (not to collection)
                var estimateItems = vm.items.Select(itemVM => new EstimateItem
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    estimateId = estimate.id,
                    productId = itemVM.productId,
                    warehouseId = itemVM.warehouseId,
                    inventoryId = itemVM.inventoryId,
                    quantity = itemVM.quantity,
                    price = itemVM.price,
                    labor = itemVM.labor,
                    title = itemVM.title,
                    description = itemVM.description ?? string.Empty,
                    barcode = itemVM.barcode ?? string.Empty,
                    source = itemVM.source ?? string.Empty,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();

                // Add directly to DbSet to ensure proper INSERT
                await _ctx.EstimateItem.AddRangeAsync(estimateItems);
                
                // Update total
                estimate.total = estimateItems.Sum(i => (i.price + i.labor) * i.quantity);
                await _ctx.SaveChangesAsync();

                // Re-establish media-to-item associations with new item IDs
                for (int idx = 0; idx < vm.items.Count; idx++)
                {
                    var itemVM = vm.items[idx];
                    if (itemVM.mediaIds != null && itemVM.mediaIds.Any())
                    {
                        var newItemId = estimateItems[idx].id;
                        var mediaToLink = await _ctx.EstimateMedia
                            .Where(m => itemVM.mediaIds.Contains(m.id) && m.estimateId == estimate.id)
                            .ToListAsync();
                        foreach (var media in mediaToLink)
                        {
                            media.estimateItemId = newItemId;
                        }
                    }
                }
                await _ctx.SaveChangesAsync();
                
                return await GetEstimateByIdAsync(estimate.id, companyId);
            }

            estimate.updateDatetime = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            return await GetEstimateByIdAsync(estimate.id, companyId);
        }

        public async Task<bool> DeleteEstimateAsync(Guid id, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == id && e.companyId == companyId);

            if (estimate == null)
                return false;

            // Delete related items first to avoid FK constraint errors
            if (estimate.items != null && estimate.items.Any())
            {
                _ctx.EstimateItem.RemoveRange(estimate.items);
            }

            // Delete related media
            var media = await _ctx.EstimateMedia
                .Where(m => m.estimateId == id && m.companyId == companyId)
                .ToListAsync();
            if (media.Any())
            {
                _ctx.EstimateMedia.RemoveRange(media);
            }

            // Delete related albums
            var albums = await _ctx.EstimateAlbums
                .Where(a => a.estimateId == id && a.companyId == companyId)
                .ToListAsync();
            if (albums.Any())
            {
                _ctx.EstimateAlbums.RemoveRange(albums);
            }

            _ctx.Estimates.Remove(estimate);
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Check if inventory items in the estimate overlap with other active jobs.
        /// Returns overallocation details for user decision before converting.
        /// </summary>
        public async Task<object> CheckOverallocationAsync(Guid estimateId, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == estimateId && e.companyId == companyId);

            if (estimate == null)
                throw new InvalidOperationException("Estimate not found");

            var conflicts = new List<object>();
            var hasConflicts = false;

            if (estimate.items != null && estimate.items.Any())
            {
                // Get all inventory-backed items (those with inventoryId or productId)
                var inventoryItems = estimate.items
                    .Where(i => i.inventoryId.HasValue || i.productId.HasValue)
                    .ToList();

                foreach (var item in inventoryItems)
                {
                    // Find other active jobs that use the same inventory/product
                    var conflictingJobs = await _ctx.JobInventory
                        .Include(ji => ji.job)
                        .Where(ji => ji.companyId == companyId
                            && ji.job != null
                            && ji.job.status != "cancelled"
                            && ji.job.status != "completed"
                            && (
                                (item.inventoryId.HasValue && ji.inventoryId == item.inventoryId) ||
                                (item.productId.HasValue && ji.productId == item.productId)
                            ))
                        .Select(ji => new
                        {
                            jobId = ji.jobId,
                            jobTitle = ji.job!.title,
                            jobNumber = ji.job.jobNumber,
                            jobStatus = ji.job.status,
                            allocatedQuantity = ji.idealQuantity
                        })
                        .ToListAsync();

                    // Check available inventory
                    decimal availableQty = 0;
                    if (item.productId.HasValue)
                    {
                        availableQty = await _ctx.Inventory
                            .Where(i => i.companyId == companyId && i.productId == item.productId)
                            .SumAsync(i => i.quantity);
                    }

                    // Only flag as conflict when there are REAL overlapping active jobs
                    // Items merely present in the estimate do NOT count as conflicts
                    if (conflictingJobs.Any())
                    {
                        var totalAllocated = conflictingJobs.Sum(j => j.allocatedQuantity);
                        var isOverallocated = (totalAllocated + item.quantity) > availableQty;

                        hasConflicts = true;
                        conflicts.Add(new
                        {
                            itemTitle = item.title,
                            requestedQuantity = item.quantity,
                            availableQuantity = availableQty,
                            totalAlreadyAllocated = totalAllocated,
                            isOverallocated,
                            conflictingJobs
                        });
                    }
                }
            }

            return new
            {
                estimateId,
                hasConflicts,
                conflicts
            };
        }

        /// <summary>
        /// Convert an estimate to a job, copying all relevant data including inventory items.
        /// EstimateItem quantities become ideal quantities in JobInventory.
        /// Available inventory is moved from warehouses to job.Inventory as actual quantities.
        /// The difference indicates what additional inventory needs to be purchased.
        /// Optionally syncs to Google Calendar if user has it linked (NMJ-40, NMJ-41).
        /// </summary>
        public async Task<JobRM> ConvertEstimateToJobAsync(ConvertEstimateToJobVM vm, Guid companyId, Guid? userId = null)
        {
            // Get the estimate with all its relationships
            var estimate = await _ctx.Estimates
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == vm.estimateId && e.companyId == companyId);

            if (estimate == null)
                throw new InvalidOperationException("Estimate not found");

            var now = DateTime.UtcNow;

            // Calculate financials from estimate items
            var materialsCost = estimate.items?.Sum(i => i.price * i.quantity) ?? 0;
            var laborCost = estimate.items?.Sum(i => i.labor * i.quantity) ?? 0;
            var estimatedTotal = materialsCost + laborCost;

            // Create new job from estimate
            var job = new Job
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                estimateId = estimate.id,
                title = !string.IsNullOrEmpty(vm.jobTitle) ? vm.jobTitle : estimate.name,
                description = !string.IsNullOrEmpty(vm.jobDescription) ? vm.jobDescription : estimate.description,
                status = vm.status,
                scheduledStartDate = estimate.proposedStartDate,
                scheduledEndDate = estimate.expirationDate,
                estimatedTotal = estimatedTotal,
                materialsCost = materialsCost,
                laborCost = laborCost,
                client = estimate.client,
                location = estimate.location,
                createDatetime = now,
                updateDatetime = now
            };

            var jobInventoryList = new List<JobInventory>();
            var actualInventoryList = new List<Inventory>();

            // Process each estimate item
            if (estimate.items != null && estimate.items.Any())
            {
                // Get default warehouse for the company (fallback for items without warehouse)
                var defaultWarehouse = await _ctx.Warehouse
                    .Where(w => w.companyId == companyId)
                    .FirstOrDefaultAsync();
                var defaultWarehouseId = defaultWarehouse?.id ?? Guid.Empty;

                foreach (var ei in estimate.items)
                {
                    // Use item's warehouse or fallback to default
                    var itemWarehouseId = ei.warehouseId ?? defaultWarehouseId;
                    
                    // Find available inventory in the specified warehouse
                    // Note: Inventory-Job relationship is now tracked via JobInventory table
                    var availableInventory = await _ctx.Inventory
                        .Where(i => i.companyId == companyId 
                            && i.productId == ei.productId 
                            && i.warehouseId == itemWarehouseId)
                        .OrderBy(i => i.createDatetime) // FIFO
                        .ToListAsync();

                    var totalAvailable = availableInventory.Sum(i => i.quantity);
                    var neededQuantity = ei.quantity;
                    var actualQuantity = Math.Min(totalAvailable, neededQuantity);

                    // Create JobInventory entry with ideal vs actual quantities
                    // Only add if we have a valid warehouse
                    if (itemWarehouseId != Guid.Empty)
                    {
                        jobInventoryList.Add(new JobInventory
                        {
                            id = Guid.NewGuid(),
                            companyId = companyId,
                            jobId = job.id,
                            productId = ei.productId ?? Guid.Empty,
                            warehouseId = itemWarehouseId,
                            idealQuantity = ei.quantity, // From estimate
                            actualQuantity = actualQuantity, // What we can actually allocate
                            price = ei.price,
                            labor = ei.labor,
                            title = ei.title,
                            description = ei.description,
                            barcode = ei.barcode,
                            source = ei.source,
                            createDatetime = now,
                            updateDatetime = now
                        });
                    }

                    // Move available inventory to the job
                    if (actualQuantity > 0)
                    {
                        var remainingToAllocate = actualQuantity;

                        foreach (var inventoryItem in availableInventory)
                        {
                            if (remainingToAllocate <= 0) break;

                            if (inventoryItem.quantity <= remainingToAllocate)
                            {
                                // Track entire inventory item allocation via JobInventory
                                // Note: Physical inventory stays in warehouse, JobInventory tracks allocation
                                remainingToAllocate -= inventoryItem.quantity;
                                actualInventoryList.Add(inventoryItem);
                            }
                            else
                            {
                                // Track partial allocation via JobInventory
                                // Note: Physical inventory stays in warehouse, JobInventory tracks allocation
                                actualInventoryList.Add(inventoryItem);
                                remainingToAllocate = 0;
                            }
                        }
                    }
                }

                job.jobInventory = jobInventoryList;
                // Removed: job.inventory = actualInventoryList; - use JobInventory instead
            }

            _ctx.Job.Add(job);

            // Mark estimate as approved if it wasn't already
            if (!estimate.approved)
            {
                estimate.approved = true;
                estimate.updateDatetime = now;
                _ctx.Estimates.Update(estimate);
            }

            await _ctx.SaveChangesAsync();

            // Google Calendar Integration (NMJ-40, NMJ-41)
            // Create calendar event for the new job if user has calendar linked
            if (userId.HasValue && _googleCalendarService != null)
            {
                try
                {
                    // Check if user has calendar linked
                    var (isLinked, _) = await _googleCalendarService.GetConnectionStatusAsync(userId.Value);
                    if (isLinked)
                    {
                        var eventId = await _googleCalendarService.CreateCalendarEventAsync(userId.Value, job);
                        if (!string.IsNullOrEmpty(eventId))
                        {
                            // Save the Google Calendar event ID to the job
                            job.googleCalendarEventId = eventId;
                            await _ctx.SaveChangesAsync();
                            Console.WriteLine($"[EstimateHandler] Created Google Calendar event {eventId} for job {job.id}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log but don't fail the job creation - calendar sync is optional
                    Console.WriteLine($"[EstimateHandler] Failed to create calendar event for job {job.id}: {ex.Message}");
                }
            }

            // Reload with relationships for response
            var createdJob = await _ctx.Job
                .Include(j => j.client)
                .Include(j => j.location)
                .Include(j => j.jobInventory)
                    .ThenInclude(ji => ji.warehouse)
                .FirstOrDefaultAsync(j => j.id == job.id);

            // CRITICAL FIX: Without this check, users lose their data when a race condition or 
            // deleted record causes null. This prevents app crashes during estimate-to-job conversion.
            if (createdJob == null)
                throw new InvalidOperationException("Failed to retrieve created job after save. Data may have been modified by another process.");

            var result = new JobRM
            {
                id = createdJob!.id,
                companyId = createdJob.companyId,
                estimateId = createdJob.estimateId,
                title = createdJob.title,
                description = createdJob.description,
                status = createdJob.status,
                scheduledStartDate = createdJob.scheduledStartDate,
                scheduledEndDate = createdJob.scheduledEndDate,
                estimatedTotal = createdJob.estimatedTotal,
                materialsCost = createdJob.materialsCost,
                laborCost = createdJob.laborCost,
                createDatetime = createdJob.createDatetime,
                updateDatetime = createdJob.updateDatetime
            };

            // Map client
            if (createdJob.client != null)
            {
                result.client = new ClientListItem
                {
                    id = createdJob.client.id,
                    companyId = createdJob.client.companyId,
                    contactName = createdJob.client.contactName,
                    phone = createdJob.client.phone,
                    email = createdJob.client.email,
                    companyName = createdJob.client.companyName,
                    createDatetime = createdJob.client.createDatetime
                };
            }

            // Map location
            if (createdJob.location != null)
            {
                result.location = _mapper.Map<Location, LocationRM>(createdJob.location);
            }

            // Map job inventory (includes difference calculation via AutoMapper)
            if (createdJob.jobInventory != null && createdJob.jobInventory.Any())
            {
                result.jobInventory = createdJob.jobInventory.Select(ji =>
                {
                    var itemRM = _mapper.Map<JobInventory, JobInventoryRM>(ji);
                    if (ji.warehouse != null)
                    {
                        itemRM.warehouse = new WarehouseBasicInfo
                        {
                            id = ji.warehouse.id,
                            name = ji.warehouse.name,
                            location = ji.warehouse.location
                        };
                    }
                    return itemRM;
                }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Extract S3 key from a stored S3 URL
        /// Example: https://nmj-dev-gallery.s3.us-east-2.amazonaws.com/Development/estimate/id/file.jpg
        /// Returns: Development/estimate/id/file.jpg
        /// </summary>
        private static string? ExtractS3KeyFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            // Try to parse as URL and extract path
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                // Remove leading slash from path
                var path = uri.AbsolutePath.TrimStart('/');
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            // If not a valid URL, might already be an s3Key
            if (!url.StartsWith("http"))
            {
                return url;
            }

            return null;
        }

        /// <summary>
        /// Generate a public share link for an estimate
        /// </summary>
        public async Task<EstimateRM?> GenerateShareLinkAsync(Guid id, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .FirstOrDefaultAsync(e => e.id == id && e.companyId == companyId);

            if (estimate == null)
                return null;

            // Generate a unique share ID if it doesn't exist
            if (string.IsNullOrEmpty(estimate.publicShareId))
            {
                estimate.publicShareId = GenerateUniqueShareId();
                estimate.updateDatetime = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
            }

            return await GetEstimateByIdAsync(id, companyId);
        }

        /// <summary>
        /// Get estimate by public share ID (for public access)
        /// </summary>
        public async Task<EstimateRM?> GetEstimateByShareIdAsync(string shareId)
        {
            if (string.IsNullOrEmpty(shareId))
                return null;

            var estimate = await _ctx.Estimates
                .AsNoTracking()
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.publicShareId == shareId);

            if (estimate == null)
                return null;

            // Check if estimate has expired (optional: you can remove this check if you want expired estimates to still be viewable)
            if (estimate.expirationDate.HasValue && estimate.expirationDate.Value < DateTime.UtcNow)
            {
                // Still return the estimate but it will show as expired on the client side
            }

            var result = new EstimateRM
            {
                id = estimate.id,
                companyId = estimate.companyId,
                name = estimate.name,
                description = estimate.description,
                total = estimate.total,
                sent = estimate.sent,
                approved = estimate.approved,
                sentDateTime = estimate.sentDateTime,
                createDatetime = estimate.createDatetime,
                updateDatetime = estimate.updateDatetime,
                termsAndConditions = estimate.termsAndConditions,
                proposedStartDate = estimate.proposedStartDate,
                expirationDate = estimate.expirationDate,
                publicShareId = estimate.publicShareId,
                viewCount = estimate.viewCount,
                lastViewedAt = estimate.lastViewedAt
            };

            // Map client (limited info for public view)
            if (estimate.client != null)
            {
                result.client = new ClientListItem
                {
                    id = estimate.client.id,
                    companyId = estimate.client.companyId,
                    contactName = estimate.client.contactName,
                    // Don't expose phone/email in public view
                    phone = null,
                    email = null,
                    companyName = estimate.client.companyName,
                    createDatetime = estimate.client.createDatetime
                };
            }

            // Map location (limited info for public view - just city/state)
            if (estimate.location != null)
            {
                result.location = new LocationRM
                {
                    id = estimate.location.id,
                    city = estimate.location.city,
                    state = estimate.location.state,
                    // Don't expose full address in public view
                    addressLine1 = string.Empty,
                    addressLine2 = string.Empty,
                    zipcode = string.Empty,
                    latitude = 0,
                    longitude = 0
                };
            }

            // Map items
            if (estimate.items != null && estimate.items.Any())
            {
                result.items = estimate.items.Select(i => _mapper.Map<EstimateItem, EstimateItemRM>(i)).ToList();
            }

            return result;
        }

        /// <summary>
        /// Approve estimate via public share link (client-facing, no auth required)
        /// Only changes status, does NOT convert to job.
        /// </summary>
        public async Task<bool> ApproveEstimateByShareIdAsync(string shareId)
        {
            if (string.IsNullOrEmpty(shareId))
                return false;

            var estimate = await _ctx.Estimates
                .FirstOrDefaultAsync(e => e.publicShareId == shareId);

            if (estimate == null)
                return false;

            if (estimate.approved)
                return true; // Already approved

            estimate.approved = true;
            estimate.updateDatetime = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Generate a unique share ID (URL-safe, short)
        /// </summary>
        private static string GenerateUniqueShareId()
        {
            // Generate a URL-safe unique ID (similar to YouTube video IDs)
            var guid = Guid.NewGuid();
            var bytes = guid.ToByteArray();
            var base64 = Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "")
                .Substring(0, 11); // Take first 11 characters for a short ID
            return base64;
        }

        /// <summary>
        /// Generate HTML view for public estimate (matches PDF design)
        /// </summary>
        public async Task<string?> GetEstimateHtmlByShareIdAsync(string shareId)
        {
            if (string.IsNullOrEmpty(shareId))
                return null;

            var estimate = await _ctx.Estimates
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.publicShareId == shareId);

            if (estimate == null)
                return null;

            // Track view count
            estimate.viewCount++;
            estimate.lastViewedAt = DateTime.UtcNow;
            await _ctx.SaveChangesAsync();

            // Load media with item associations
            var media = await _ctx.EstimateMedia
                .AsNoTracking()
                .Where(m => m.estimateId == estimate.id)
                .OrderBy(m => m.orderIndex)
                .ToListAsync();

            // Get company name
            var company = await _ctx.Company
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.id == estimate.companyId);
            var companyName = company?.name ?? "SilkRoad";

            // Build line items HTML with associated media thumbnails
            var itemsHtml = "";
            if (estimate.items != null && estimate.items.Any())
            {
                var index = 0;
                foreach (var item in estimate.items)
                {
                    var lineTotal = item.price * item.quantity;
                    var rowClass = index % 2 == 0 ? "row-even" : "row-odd";

                    // Find media associated with this line item
                    var itemMedia = media.Where(m => m.estimateItemId == item.id).ToList();
                    var mediaThumbs = "";
                    if (itemMedia.Any())
                    {
                        var thumbsHtml = string.Join("", itemMedia.Select(m => {
                            var url = m.webUrl;
                            // Generate presigned URL if needed
                            if (_s3PresignedService != null && !string.IsNullOrEmpty(m.webUrl))
                            {
                                var s3Key = ExtractS3KeyFromUrl(m.webUrl);
                                if (!string.IsNullOrEmpty(s3Key))
                                {
                                    try
                                    {
                                        var (webPresigned, _) = _s3PresignedService.GeneratePresignedDownloadUrls(s3Key).GetAwaiter().GetResult();
                                        url = webPresigned;
                                    }
                                    catch { /* fallback to original URL */ }
                                }
                            }
                            return $@"<img src=""{url}"" class=""item-thumb"" alt=""{item.title}"" />";
                        }));
                        mediaThumbs = $@"<div class=""item-media"">{thumbsHtml}</div>";
                    }

                    itemsHtml += $@"
                        <tr class=""{rowClass}"">
                            <td class=""cell cell-num"">{index + 1}</td>
                            <td class=""cell cell-desc"">
                                {item.title ?? item.description}
                                {mediaThumbs}
                            </td>
                            <td class=""cell cell-center"">{item.quantity}</td>
                            <td class=""cell cell-right"">{Templates.EstimateHtmlTemplate.FormatCurrency(item.price)}</td>
                            <td class=""cell cell-right cell-total"">{Templates.EstimateHtmlTemplate.FormatCurrency(lineTotal)}</td>
                        </tr>";
                    index++;
                }
            }

            var estimateNumber = $"EST-{estimate.id.ToString().Substring(0, 6).ToUpper()}";

            return Templates.EstimateHtmlTemplate.Generate(
                companyName: companyName,
                estimateNumber: estimateNumber,
                createDate: estimate.createDatetime,
                expirationDate: estimate.expirationDate,
                total: estimate.total,
                clientName: estimate.client?.contactName,
                clientEmail: estimate.client?.email,
                clientPhone: estimate.client?.phone,
                locationAddress: estimate.location?.addressLine1,
                locationCityState: estimate.location != null 
                    ? $"{estimate.location.city}, {estimate.location.state} {estimate.location.zipcode ?? ""}" 
                    : null,
                description: estimate.description,
                lineItemsHtml: itemsHtml,
                sent: estimate.sent,
                approved: estimate.approved,
                shareId: estimate.publicShareId ?? "",
                proposedStartDate: estimate.proposedStartDate
            );
        }

        /// <summary>
        /// Create a new version of an estimate. The original stays as baseline,
        /// and the new version becomes the latest. Client must re-approve.
        /// </summary>
        public async Task<EstimateRM> CreateNewVersionAsync(Guid estimateId, Guid companyId)
        {
            var original = await _ctx.Estimates
                .Include(e => e.client)
                .Include(e => e.location)
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == estimateId && e.companyId == companyId);

            if (original == null)
                throw new InvalidOperationException("Estimate not found");

            // Determine the root parent (for version chain)
            var rootId = original.parentEstimateId ?? original.id;

            // Find current max version in the chain
            var maxVersion = await _ctx.Estimates
                .Where(e => e.companyId == companyId && (e.id == rootId || e.parentEstimateId == rootId))
                .MaxAsync(e => e.version);

            // Mark current latest as not latest
            var currentLatest = await _ctx.Estimates
                .Where(e => e.companyId == companyId && (e.id == rootId || e.parentEstimateId == rootId) && e.isLatestVersion)
                .ToListAsync();
            foreach (var est in currentLatest)
            {
                est.isLatestVersion = false;
            }

            var now = DateTime.UtcNow;

            // Create new version as a copy
            var newVersion = new Estimates
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = original.name,
                description = original.description,
                total = original.total,
                sent = false,
                approved = false,
                sentDateTime = default,
                proposedStartDate = original.proposedStartDate,
                expirationDate = original.expirationDate,
                termsAndConditions = original.termsAndConditions,
                createDatetime = now,
                updateDatetime = now,
                version = maxVersion + 1,
                parentEstimateId = rootId,
                isLatestVersion = true,
            };

            // Copy client/location references
            if (original.client != null)
            {
                newVersion.client = original.client;
            }
            if (original.location != null)
            {
                newVersion.location = original.location;
            }

            _ctx.Estimates.Add(newVersion);

            // Build a mapping from old item id to new item id (for media associations)
            var itemIdMap = new Dictionary<Guid, Guid>();

            // Copy line items
            if (original.items != null && original.items.Any())
            {
                foreach (var item in original.items)
                {
                    var newItemId = Guid.NewGuid();
                    itemIdMap[item.id] = newItemId;
                    var newItem = new EstimateItem
                    {
                        id = newItemId,
                        companyId = companyId,
                        estimateId = newVersion.id,
                        productId = item.productId,
                        warehouseId = item.warehouseId,
                        inventoryId = item.inventoryId,
                        title = item.title,
                        description = item.description,
                        quantity = item.quantity,
                        price = item.price,
                        labor = item.labor,
                        createDatetime = now,
                        updateDatetime = now,
                    };
                    _ctx.EstimateItem.Add(newItem);
                }
            }

            // Copy albums (build mapping for media album associations)
            var albumIdMap = new Dictionary<Guid, Guid>();
            var originalAlbums = await _ctx.EstimateAlbums
                .Where(a => a.estimateId == estimateId && a.companyId == companyId)
                .ToListAsync();
            foreach (var album in originalAlbums)
            {
                var newAlbumId = Guid.NewGuid();
                albumIdMap[album.id] = newAlbumId;
                _ctx.EstimateAlbums.Add(new EstimateAlbum
                {
                    id = newAlbumId,
                    estimateId = newVersion.id,
                    companyId = companyId,
                    name = album.name,
                    description = album.description,
                    orderIndex = album.orderIndex,
                    createDatetime = now,
                    updateDatetime = now,
                });
            }

            // Copy media (reuse same S3 URLs, map album and item references)
            var originalMedia = await _ctx.EstimateMedia
                .Where(m => m.estimateId == estimateId && m.companyId == companyId)
                .ToListAsync();
            foreach (var media in originalMedia)
            {
                _ctx.EstimateMedia.Add(new EstimateMedia
                {
                    id = Guid.NewGuid(),
                    estimateId = newVersion.id,
                    companyId = companyId,
                    albumId = media.albumId.HasValue && albumIdMap.ContainsKey(media.albumId.Value)
                        ? albumIdMap[media.albumId.Value] : null,
                    estimateItemId = media.estimateItemId.HasValue && itemIdMap.ContainsKey(media.estimateItemId.Value)
                        ? itemIdMap[media.estimateItemId.Value] : null,
                    webUrl = media.webUrl,
                    thumbUrl = media.thumbUrl,
                    caption = media.caption,
                    type = media.type,
                    orderIndex = media.orderIndex,
                    createDatetime = now,
                    updateDatetime = now,
                });
            }

            await _ctx.SaveChangesAsync();

            return await GetEstimateByIdAsync(newVersion.id, companyId);
        }

        /// <summary>
        /// Get version history for an estimate (all versions in the chain)
        /// </summary>
        public async Task<List<EstimateListItem>> GetVersionHistoryAsync(Guid estimateId, Guid companyId)
        {
            var estimate = await _ctx.Estimates
                .FirstOrDefaultAsync(e => e.id == estimateId && e.companyId == companyId);

            if (estimate == null)
                throw new InvalidOperationException("Estimate not found");

            var rootId = estimate.parentEstimateId ?? estimate.id;

            var versions = await _ctx.Estimates
                .Where(e => e.companyId == companyId && (e.id == rootId || e.parentEstimateId == rootId))
                .OrderByDescending(e => e.version)
                .Select(e => new EstimateListItem
                {
                    id = e.id,
                    companyId = e.companyId,
                    name = e.name,
                    clientId = e.client != null ? e.client.id : (Guid?)null,
                    clientName = e.client != null ? e.client.contactName : "",
                    locationCity = e.location != null ? e.location.city : "",
                    total = e.total,
                    sent = e.sent,
                    approved = e.approved,
                    createDatetime = e.createDatetime,
                    version = e.version,
                    isLatestVersion = e.isLatestVersion,
                })
                .ToListAsync();

            return versions;
        }
    }
}
